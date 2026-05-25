using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AchievementSystem", menuName = "Game/Achievement System")]
public class AchievementSystem : ScriptableObject
{
    [Serializable]
    public class Achievement
    {
        public string id;
        public string title;
        [TextArea] public string description;
        public Sprite icon;

        [Header("Requirements")]
        public AchievementType type;
        public float targetValue = 1f;
        public int rewardXP;
        public int rewardCurrency;

        [Header("State")]
        public float currentValue;
        public bool isUnlocked;
        public bool isHidden;
        public string unlockTimeIso;

        public DateTime UnlockTime
        {
            get
            {
                if (string.IsNullOrEmpty(unlockTimeIso))
                {
                    return DateTime.MinValue;
                }

                DateTime.TryParse(unlockTimeIso, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime parsed);
                return parsed;
            }
        }
    }

    public enum AchievementType
    {
        TotalEnergyCollected,
        TotalDistanceDriven,
        MaxSpeedReached,
        AbilityUsedCount,
        MaxCombo,
        PlayTimeSeconds,
        AllEnergyTypesDiscovered,
        DailyLoginStreak
    }

    [Serializable]
    public class AchievementStats
    {
        public int totalAchievements;
        public int unlockedAchievements;
        public float completionPercentage;
        public int totalRewardXP;
        public int totalRewardCurrency;
    }

    [Serializable]
    private class AchievementSaveEntry
    {
        public string id;
        public float currentValue;
        public bool isUnlocked;
        public string unlockTimeIso;
    }

    [Serializable]
    private class AchievementSaveData
    {
        public List<AchievementSaveEntry> achievements = new List<AchievementSaveEntry>();
        public bool discoveredRed;
        public bool discoveredBlue;
        public bool discoveredGreen;
    }

    [Header("Achievements")]
    public List<Achievement> achievements = new List<Achievement>();

    [Header("Settings")]
    public bool autoSave = true;
    public string saveKey = "AchievementsData";

    public event Action<Achievement> OnAchievementUnlocked;
    public event Action<Achievement> OnAchievementProgressChanged;

    private readonly HashSet<EnergySystem.EnergyType> discoveredEnergyTypes = new HashSet<EnergySystem.EnergyType>();
    private bool isInitialized;

    public void Initialize()
    {
        if (isInitialized)
        {
            return;
        }

        EnsureDefaultAchievements();
        LoadAchievements();

        GameEvents.OnEnergyCollected += HandleEnergyCollected;
        GameEvents.OnAbilityUsed += HandleAbilityUsed;
        GameEvents.OnMaxComboUpdated += HandleMaxComboUpdated;
        GameEvents.OnPlayTimeUpdated += HandlePlayTimeUpdated;
        GameEvents.OnVehicleDistanceUpdated += HandleVehicleDistanceUpdated;
        GameEvents.OnTopSpeedUpdated += HandleTopSpeedUpdated;
        GameEvents.OnDailyLoginStreakUpdated += HandleDailyLoginStreakUpdated;

        RefreshEnergyDiscoveryAchievement();
        isInitialized = true;
    }

    public void Shutdown()
    {
        if (!isInitialized)
        {
            return;
        }

        GameEvents.OnEnergyCollected -= HandleEnergyCollected;
        GameEvents.OnAbilityUsed -= HandleAbilityUsed;
        GameEvents.OnMaxComboUpdated -= HandleMaxComboUpdated;
        GameEvents.OnPlayTimeUpdated -= HandlePlayTimeUpdated;
        GameEvents.OnVehicleDistanceUpdated -= HandleVehicleDistanceUpdated;
        GameEvents.OnTopSpeedUpdated -= HandleTopSpeedUpdated;
        GameEvents.OnDailyLoginStreakUpdated -= HandleDailyLoginStreakUpdated;
        isInitialized = false;
    }

    public void UpdateAchievement(AchievementType type, float increment)
    {
        Achievement achievement = achievements.Find(item => item.type == type && !item.isUnlocked);
        if (achievement == null)
        {
            return;
        }

        achievement.currentValue += increment;
        OnAchievementProgressChanged?.Invoke(achievement);

        if (achievement.currentValue >= achievement.targetValue)
        {
            UnlockAchievement(achievement);
        }

        if (autoSave)
        {
            SaveAchievements();
        }
    }

    public void SetAchievementProgress(AchievementType type, float value)
    {
        Achievement achievement = achievements.Find(item => item.type == type && !item.isUnlocked);
        if (achievement == null)
        {
            return;
        }

        achievement.currentValue = Mathf.Max(achievement.currentValue, value);
        OnAchievementProgressChanged?.Invoke(achievement);

        if (achievement.currentValue >= achievement.targetValue)
        {
            UnlockAchievement(achievement);
        }

        if (autoSave)
        {
            SaveAchievements();
        }
    }

    public List<Achievement> GetAchievements()
    {
        return achievements;
    }

    public AchievementStats GetStats()
    {
        AchievementStats stats = new AchievementStats();
        stats.totalAchievements = achievements.Count;

        foreach (Achievement achievement in achievements)
        {
            if (!achievement.isUnlocked)
            {
                continue;
            }

            stats.unlockedAchievements++;
            stats.totalRewardXP += achievement.rewardXP;
            stats.totalRewardCurrency += achievement.rewardCurrency;
        }

        if (stats.totalAchievements > 0)
        {
            stats.completionPercentage = (float)stats.unlockedAchievements / stats.totalAchievements * 100f;
        }

        return stats;
    }

    [ContextMenu("Create Default Achievements")]
    public void CreateDefaultAchievements()
    {
        achievements = new List<Achievement>
        {
            new Achievement { id = "energy_25", title = "Первые ватты", description = "Соберите 25 единиц энергии.", type = AchievementType.TotalEnergyCollected, targetValue = 25, rewardXP = 25, rewardCurrency = 10 },
            new Achievement { id = "energy_all", title = "Полный спектр", description = "Откройте все три типа энергии.", type = AchievementType.AllEnergyTypesDiscovered, targetValue = 3, rewardXP = 50, rewardCurrency = 20 },
            new Achievement { id = "drive_100", title = "Первые метры", description = "Проедьте 100 метров на машине.", type = AchievementType.TotalDistanceDriven, targetValue = 100f, rewardXP = 40, rewardCurrency = 15 },
            new Achievement { id = "speed_20", title = "Разгон", description = "Разгонитесь до скорости 20.", type = AchievementType.MaxSpeedReached, targetValue = 20f, rewardXP = 35, rewardCurrency = 15 },
            new Achievement { id = "ability_5", title = "Практик способностей", description = "Используйте способности 5 раз.", type = AchievementType.AbilityUsedCount, targetValue = 5f, rewardXP = 40, rewardCurrency = 20 },
            new Achievement { id = "combo_3", title = "Связка", description = "Соберите комбо из 3 способностей.", type = AchievementType.MaxCombo, targetValue = 3f, rewardXP = 70, rewardCurrency = 30 },
            new Achievement { id = "play_300", title = "Не отпускает", description = "Проведите в игре 5 минут.", type = AchievementType.PlayTimeSeconds, targetValue = 300f, rewardXP = 60, rewardCurrency = 25 },
            new Achievement { id = "streak_3", title = "Постоянный водитель", description = "Получите ежедневную награду 3 дня подряд.", type = AchievementType.DailyLoginStreak, targetValue = 3f, rewardXP = 100, rewardCurrency = 50 }
        };
    }

    [ContextMenu("Reset Achievement Progress")]
    public void ResetProgress()
    {
        discoveredEnergyTypes.Clear();

        foreach (Achievement achievement in achievements)
        {
            achievement.currentValue = 0f;
            achievement.isUnlocked = false;
            achievement.unlockTimeIso = string.Empty;
        }

        SaveAchievements();
        NotifyAllAchievementsChanged();
    }

    public void SaveAchievements()
    {
        AchievementSaveData saveData = new AchievementSaveData();

        foreach (Achievement achievement in achievements)
        {
            saveData.achievements.Add(new AchievementSaveEntry
            {
                id = achievement.id,
                currentValue = achievement.currentValue,
                isUnlocked = achievement.isUnlocked,
                unlockTimeIso = achievement.unlockTimeIso
            });
        }

        saveData.discoveredRed = discoveredEnergyTypes.Contains(EnergySystem.EnergyType.Red);
        saveData.discoveredBlue = discoveredEnergyTypes.Contains(EnergySystem.EnergyType.Blue);
        saveData.discoveredGreen = discoveredEnergyTypes.Contains(EnergySystem.EnergyType.Green);

        PlayerPrefs.SetString(saveKey, JsonUtility.ToJson(saveData));
        PlayerPrefs.Save();
    }

    public void LoadAchievements()
    {
        if (!PlayerPrefs.HasKey(saveKey))
        {
            return;
        }

        AchievementSaveData saveData = JsonUtility.FromJson<AchievementSaveData>(PlayerPrefs.GetString(saveKey));
        if (saveData == null)
        {
            return;
        }

        discoveredEnergyTypes.Clear();
        if (saveData.discoveredRed) discoveredEnergyTypes.Add(EnergySystem.EnergyType.Red);
        if (saveData.discoveredBlue) discoveredEnergyTypes.Add(EnergySystem.EnergyType.Blue);
        if (saveData.discoveredGreen) discoveredEnergyTypes.Add(EnergySystem.EnergyType.Green);

        foreach (Achievement achievement in achievements)
        {
            AchievementSaveEntry entry = saveData.achievements.Find(item => item.id == achievement.id);
            if (entry == null)
            {
                continue;
            }

            achievement.currentValue = entry.currentValue;
            achievement.isUnlocked = entry.isUnlocked;
            achievement.unlockTimeIso = entry.unlockTimeIso;
        }
    }

    private void HandleEnergyCollected(EnergySystem.EnergyType type, int amount)
    {
        UpdateAchievement(AchievementType.TotalEnergyCollected, amount);

        if (discoveredEnergyTypes.Add(type))
        {
            RefreshEnergyDiscoveryAchievement();
        }
    }

    private void HandleAbilityUsed(string abilityName)
    {
        UpdateAchievement(AchievementType.AbilityUsedCount, 1f);
    }

    private void HandleMaxComboUpdated(int combo)
    {
        SetAchievementProgress(AchievementType.MaxCombo, combo);
    }

    private void HandlePlayTimeUpdated(float deltaSeconds)
    {
        UpdateAchievement(AchievementType.PlayTimeSeconds, deltaSeconds);
    }

    private void HandleVehicleDistanceUpdated(float deltaDistance)
    {
        UpdateAchievement(AchievementType.TotalDistanceDriven, deltaDistance);
    }

    private void HandleTopSpeedUpdated(float speed)
    {
        SetAchievementProgress(AchievementType.MaxSpeedReached, speed);
    }

    private void HandleDailyLoginStreakUpdated(int streak)
    {
        SetAchievementProgress(AchievementType.DailyLoginStreak, streak);
    }

    private void RefreshEnergyDiscoveryAchievement()
    {
        SetAchievementProgress(AchievementType.AllEnergyTypesDiscovered, discoveredEnergyTypes.Count);
    }

    private void UnlockAchievement(Achievement achievement)
    {
        if (achievement.isUnlocked)
        {
            return;
        }

        achievement.isUnlocked = true;
        achievement.currentValue = Mathf.Max(achievement.currentValue, achievement.targetValue);
        achievement.unlockTimeIso = DateTime.Now.ToString("o");

        if (PlayerMetaProgress.Instance != null)
        {
            if (achievement.rewardXP > 0)
            {
                PlayerMetaProgress.Instance.AddXP(achievement.rewardXP);
            }

            if (achievement.rewardCurrency > 0)
            {
                PlayerMetaProgress.Instance.AddCurrency(achievement.rewardCurrency);
            }
        }

        OnAchievementUnlocked?.Invoke(achievement);
        OnAchievementProgressChanged?.Invoke(achievement);
        Debug.Log($"Achievement unlocked: {achievement.title}");
    }

    private void EnsureDefaultAchievements()
    {
        if (achievements == null || achievements.Count == 0)
        {
            CreateDefaultAchievements();
        }
    }

    public void NotifyAllAchievementsChanged()
    {
        foreach (Achievement achievement in achievements)
        {
            OnAchievementProgressChanged?.Invoke(achievement);
        }
    }
}
