using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DailyRewardUI : MonoBehaviour
{
    [SerializeField] private DailyRewardManager dailyRewardManager;
    [SerializeField] private GameObject rewardItemPrefab;
    [SerializeField] private Transform rewardListContainer;
    [SerializeField] private TextMeshProUGUI streakText;
    [SerializeField] private TextMeshProUGUI resetTimerText;
    [SerializeField] private TextMeshProUGUI bonusText;

    private readonly List<DailyRewardUIItem> rewardItems = new List<DailyRewardUIItem>();
    private float timerRefreshDelay;

    private void Start()
    {
        if (dailyRewardManager == null || dailyRewardManager.RewardSystem == null)
        {
            return;
        }

        BuildItems();
        dailyRewardManager.RewardSystem.OnRewardClaimed += HandleRewardClaimed;
        dailyRewardManager.RewardSystem.OnStreakUpdated += HandleStreakUpdated;
        RefreshUI();
    }

    private void OnDestroy()
    {
        if (dailyRewardManager == null || dailyRewardManager.RewardSystem == null)
        {
            return;
        }

        dailyRewardManager.RewardSystem.OnRewardClaimed -= HandleRewardClaimed;
        dailyRewardManager.RewardSystem.OnStreakUpdated -= HandleStreakUpdated;
    }

    private void Update()
    {
        timerRefreshDelay += Time.unscaledDeltaTime;
        if (timerRefreshDelay < 1f)
        {
            return;
        }

        timerRefreshDelay = 0f;
        RefreshHeader();
    }

    public void RefreshUI()
    {
        RefreshHeader();

        foreach (DailyRewardUIItem item in rewardItems)
        {
            item.Refresh();
        }
    }

    private void BuildItems()
    {
        if (rewardItemPrefab == null || rewardListContainer == null)
        {
            return;
        }

        foreach (Transform child in rewardListContainer)
        {
            Destroy(child.gameObject);
        }

        rewardItems.Clear();

        foreach (DailyRewardSystem.DailyReward reward in dailyRewardManager.RewardSystem.GetRewards())
        {
            GameObject itemObject = Instantiate(rewardItemPrefab, rewardListContainer);
            DailyRewardUIItem item = itemObject.GetComponent<DailyRewardUIItem>();
            if (item == null)
            {
                continue;
            }

            item.Initialize(dailyRewardManager, reward, RefreshUI);
            rewardItems.Add(item);
        }
    }

    private void RefreshHeader()
    {
        DailyRewardSystem.StreakInfo info = dailyRewardManager.RewardSystem.GetStreakInfo();

        if (streakText != null)
        {
            streakText.text = $"Серия: {info.currentStreak}/{info.maxStreak}";
        }

        if (resetTimerText != null)
        {
            resetTimerText.text = $"Следующий сброс: {info.nextResetTime}";
        }

        if (bonusText != null)
        {
            bonusText.text = $"Бонус серии: x{info.bonusMultiplier:F2}";
        }
    }

    private void HandleRewardClaimed(DailyRewardSystem.DailyReward reward, int amount)
    {
        RefreshUI();
    }

    private void HandleStreakUpdated(int streak)
    {
        RefreshUI();
    }
}
