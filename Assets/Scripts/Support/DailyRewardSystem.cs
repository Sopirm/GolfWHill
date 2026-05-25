using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DailyRewardSystem", menuName = "Game/Daily Reward System")]
public class DailyRewardSystem : ScriptableObject
{
    [Serializable]
    public class DailyReward
    {
        public int day;
        public RewardType rewardType;
        public int rewardAmount;
        public Sprite rewardIcon;
        public bool isSpecial;

        [Header("State")]
        public bool isClaimed;
        public bool isAvailable;
    }

    public enum RewardType
    {
        Currency,
        XP,
        RedEnergy,
        BlueEnergy,
        GreenEnergy
    }

    [Serializable]
    public class StreakInfo
    {
        public int currentStreak;
        public int maxStreak;
        public string nextResetTime;
        public int streakBonus;
        public float bonusMultiplier;
        public int daysUntilSpecialReward;
    }

    [Serializable]
    private class DailyRewardSaveData
    {
        public int currentStreak;
        public string lastClaimDateIso;
        public List<bool> claimedRewards = new List<bool>();
    }

    [Header("Rewards")]
    public List<DailyReward> weeklyRewards = new List<DailyReward>();

    [Header("Settings")]
    public int resetHour = 4;
    public int streakBonus = 10;
    public int maxStreakDays = 28;
    public string saveKey = "DailyRewardsData";

    public event Action<DailyReward, int> OnRewardClaimed;
    public event Action<int> OnStreakUpdated;

    private int currentStreak;
    private string lastClaimDateIso;
    private bool isInitialized;

    public void Initialize()
    {
        if (isInitialized)
        {
            return;
        }

        EnsureDefaultRewards();
        LoadData();
        CheckStreak();
        UpdateAvailableRewards();
        isInitialized = true;
    }

    public List<DailyReward> GetRewards()
    {
        return weeklyRewards;
    }

    public bool CanClaimReward(int day)
    {
        if (day < 1 || day > weeklyRewards.Count)
        {
            return false;
        }

        DailyReward reward = weeklyRewards[day - 1];
        return reward.isAvailable && !reward.isClaimed && HasNewClaimWindow();
    }

    public bool ClaimReward(int day, PlayerMetaProgress progress, EnergySystem energySystem)
    {
        if (!CanClaimReward(day))
        {
            Debug.LogWarning($"Daily reward for day {day} is not available.");
            return false;
        }

        DailyReward reward = weeklyRewards[day - 1];
        int finalAmount = CalculateRewardWithBonus(reward.rewardAmount);

        switch (reward.rewardType)
        {
            case RewardType.Currency:
                progress?.AddCurrency(finalAmount);
                break;
            case RewardType.XP:
                progress?.AddXP(finalAmount);
                break;
            case RewardType.RedEnergy:
                energySystem?.CollectEnergy(EnergySystem.EnergyType.Red, finalAmount);
                break;
            case RewardType.BlueEnergy:
                energySystem?.CollectEnergy(EnergySystem.EnergyType.Blue, finalAmount);
                break;
            case RewardType.GreenEnergy:
                energySystem?.CollectEnergy(EnergySystem.EnergyType.Green, finalAmount);
                break;
        }

        reward.isClaimed = true;
        reward.isAvailable = false;
        currentStreak = Mathf.Clamp(currentStreak + 1, 1, maxStreakDays);
        lastClaimDateIso = DateTime.Now.ToString("o");

        if (currentStreak >= maxStreakDays)
        {
            currentStreak = 1;
            ResetWeeklyCycle();
        }

        SaveData();
        UpdateAvailableRewards();
        OnRewardClaimed?.Invoke(reward, finalAmount);
        OnStreakUpdated?.Invoke(currentStreak);
        GameEvents.UpdateDailyLoginStreak(currentStreak);
        Debug.Log($"Daily reward claimed: day={day}, streak={currentStreak}, amount={finalAmount}");
        return true;
    }

    public StreakInfo GetStreakInfo()
    {
        return new StreakInfo
        {
            currentStreak = currentStreak,
            maxStreak = maxStreakDays,
            nextResetTime = GetFormattedTimeUntilReset(),
            streakBonus = streakBonus,
            bonusMultiplier = 1f + (currentStreak * streakBonus / 100f),
            daysUntilSpecialReward = weeklyRewards.Count - (currentStreak % Mathf.Max(1, weeklyRewards.Count))
        };
    }

    public TimeSpan GetTimeUntilReset()
    {
        return GetNextResetTime() - DateTime.Now;
    }

    public string GetFormattedTimeUntilReset()
    {
        TimeSpan timeLeft = GetTimeUntilReset();
        if (timeLeft.TotalSeconds < 0)
        {
            timeLeft = TimeSpan.Zero;
        }

        return $"{timeLeft.Hours:D2}:{timeLeft.Minutes:D2}:{timeLeft.Seconds:D2}";
    }

    [ContextMenu("Create Default Daily Rewards")]
    public void CreateDefaultRewards()
    {
        weeklyRewards = new List<DailyReward>
        {
            new DailyReward { day = 1, rewardType = RewardType.Currency, rewardAmount = 25 },
            new DailyReward { day = 2, rewardType = RewardType.RedEnergy, rewardAmount = 15 },
            new DailyReward { day = 3, rewardType = RewardType.XP, rewardAmount = 50 },
            new DailyReward { day = 4, rewardType = RewardType.BlueEnergy, rewardAmount = 15 },
            new DailyReward { day = 5, rewardType = RewardType.Currency, rewardAmount = 50 },
            new DailyReward { day = 6, rewardType = RewardType.GreenEnergy, rewardAmount = 20 },
            new DailyReward { day = 7, rewardType = RewardType.XP, rewardAmount = 100, isSpecial = true }
        };
    }

    [ContextMenu("Reset Daily Rewards Progress")]
    public void ResetProgress()
    {
        currentStreak = 0;
        lastClaimDateIso = string.Empty;

        foreach (DailyReward reward in weeklyRewards)
        {
            reward.isClaimed = false;
            reward.isAvailable = false;
        }

        SaveData();
        UpdateAvailableRewards();
        OnStreakUpdated?.Invoke(currentStreak);
    }

    private void CheckStreak()
    {
        DateTime lastClaimDate = ParseLastClaimDate();
        if (lastClaimDate == DateTime.MinValue)
        {
            currentStreak = 0;
            return;
        }

        TimeSpan delta = DateTime.Now - lastClaimDate;
        if (delta.TotalHours > 48)
        {
            currentStreak = 0;
            ResetWeeklyCycle();
        }
    }

    private void UpdateAvailableRewards()
    {
        if (weeklyRewards.Count == 0)
        {
            return;
        }

        int targetDay = (currentStreak % weeklyRewards.Count) + 1;
        bool canClaimToday = HasNewClaimWindow();

        for (int i = 0; i < weeklyRewards.Count; i++)
        {
            DailyReward reward = weeklyRewards[i];
            reward.isAvailable = canClaimToday && reward.day == targetDay && !reward.isClaimed;
        }
    }

    private void ResetWeeklyCycle()
    {
        foreach (DailyReward reward in weeklyRewards)
        {
            reward.isClaimed = false;
        }
    }

    private bool HasNewClaimWindow()
    {
        DateTime lastClaimDate = ParseLastClaimDate();
        if (lastClaimDate == DateTime.MinValue)
        {
            return true;
        }

        return DateTime.Now >= GetNextResetTime(lastClaimDate);
    }

    private int CalculateRewardWithBonus(int baseAmount)
    {
        float multiplier = 1f + (currentStreak * streakBonus / 100f);
        return Mathf.RoundToInt(baseAmount * multiplier);
    }

    private DateTime GetNextResetTime()
    {
        return GetNextResetTime(DateTime.Now);
    }

    private DateTime GetNextResetTime(DateTime reference)
    {
        DateTime resetTime = reference.Date.AddHours(resetHour);
        if (reference >= resetTime)
        {
            resetTime = resetTime.AddDays(1);
        }

        return resetTime;
    }

    private DateTime ParseLastClaimDate()
    {
        if (string.IsNullOrEmpty(lastClaimDateIso))
        {
            return DateTime.MinValue;
        }

        DateTime.TryParse(lastClaimDateIso, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime parsed);
        return parsed;
    }

    private void SaveData()
    {
        DailyRewardSaveData data = new DailyRewardSaveData
        {
            currentStreak = currentStreak,
            lastClaimDateIso = lastClaimDateIso
        };

        foreach (DailyReward reward in weeklyRewards)
        {
            data.claimedRewards.Add(reward.isClaimed);
        }

        PlayerPrefs.SetString(saveKey, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    private void LoadData()
    {
        if (!PlayerPrefs.HasKey(saveKey))
        {
            return;
        }

        DailyRewardSaveData data = JsonUtility.FromJson<DailyRewardSaveData>(PlayerPrefs.GetString(saveKey));
        if (data == null)
        {
            return;
        }

        currentStreak = Mathf.Max(0, data.currentStreak);
        lastClaimDateIso = data.lastClaimDateIso;

        for (int i = 0; i < weeklyRewards.Count && i < data.claimedRewards.Count; i++)
        {
            weeklyRewards[i].isClaimed = data.claimedRewards[i];
        }
    }

    private void EnsureDefaultRewards()
    {
        if (weeklyRewards == null || weeklyRewards.Count == 0)
        {
            CreateDefaultRewards();
        }
    }
}
