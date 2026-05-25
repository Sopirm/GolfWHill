using System;
using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class PlayerMetaProgress : MonoBehaviour
{
    [Serializable]
    private class SaveData
    {
        public int level = 1;
        public int currentXP;
        public int totalXP;
        public int currency;
    }

    public static PlayerMetaProgress Instance { get; private set; }

    [SerializeField] private string saveKey = "PlayerMetaProgressData";
    [SerializeField] private int level = 1;
    [SerializeField] private int currentXP;
    [SerializeField] private int totalXP;
    [SerializeField] private int currency;
    [SerializeField] private int baseXPForLevel = 100;
    [SerializeField] private float growthFactor = 1.5f;

    public event Action<int, int, int> OnXPChanged;
    public event Action<int> OnCurrencyChanged;
    public event Action<int> OnLevelChanged;

    public int Level => level;
    public int CurrentXP => currentXP;
    public int TotalXP => totalXP;
    public int Currency => currency;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Load();
        NotifyState();
    }

    public void AddXP(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        totalXP += amount;
        currentXP += amount;

        while (currentXP >= GetXPForNextLevel())
        {
            currentXP -= GetXPForNextLevel();
            level++;
            OnLevelChanged?.Invoke(level);
        }

        Save();
        OnXPChanged?.Invoke(currentXP, GetXPForNextLevel(), level);
    }

    public void AddCurrency(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        currency += amount;
        Save();
        OnCurrencyChanged?.Invoke(currency);
    }

    public bool SpendCurrency(int amount)
    {
        if (amount <= 0 || currency < amount)
        {
            return false;
        }

        currency -= amount;
        Save();
        OnCurrencyChanged?.Invoke(currency);
        return true;
    }

    public int GetXPForNextLevel()
    {
        return Mathf.RoundToInt(baseXPForLevel * Mathf.Pow(level, growthFactor));
    }

    [ContextMenu("Reset Progress")]
    public void ResetProgress()
    {
        level = 1;
        currentXP = 0;
        totalXP = 0;
        currency = 0;
        Save();
        NotifyState();
    }

    private void OnApplicationQuit()
    {
        Save();
    }

    private void OnDisable()
    {
        Save();
    }

    private void Save()
    {
        PlayerPrefs.SetInt($"{saveKey}_Level", level);
        PlayerPrefs.SetInt($"{saveKey}_CurrentXP", currentXP);
        PlayerPrefs.SetInt($"{saveKey}_TotalXP", totalXP);
        PlayerPrefs.SetInt($"{saveKey}_Currency", currency);
        PlayerPrefs.Save();
    }

    private void Load()
    {
        string levelKey = $"{saveKey}_Level";
        string currentXPKey = $"{saveKey}_CurrentXP";
        string totalXPKey = $"{saveKey}_TotalXP";
        string currencyKey = $"{saveKey}_Currency";

        if (PlayerPrefs.HasKey(levelKey))
        {
            level = Mathf.Max(1, PlayerPrefs.GetInt(levelKey, 1));
            currentXP = Mathf.Max(0, PlayerPrefs.GetInt(currentXPKey, 0));
            totalXP = Mathf.Max(0, PlayerPrefs.GetInt(totalXPKey, 0));
            currency = Mathf.Max(0, PlayerPrefs.GetInt(currencyKey, 0));
            return;
        }

        if (!PlayerPrefs.HasKey(saveKey))
        {
            return;
        }

        SaveData data = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(saveKey));
        if (data == null)
        {
            return;
        }

        level = Mathf.Max(1, data.level);
        currentXP = Mathf.Max(0, data.currentXP);
        totalXP = Mathf.Max(0, data.totalXP);
        currency = Mathf.Max(0, data.currency);
        Save();
    }

    private void NotifyState()
    {
        OnLevelChanged?.Invoke(level);
        OnCurrencyChanged?.Invoke(currency);
        OnXPChanged?.Invoke(currentXP, GetXPForNextLevel(), level);
    }
}
