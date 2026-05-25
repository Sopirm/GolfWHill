using UnityEngine;

public class DailyRewardManager : MonoBehaviour
{
    [SerializeField] private DailyRewardSystem dailyRewardSystem;
    [SerializeField] private PlayerMetaProgress playerMetaProgress;
    [SerializeField] private EnergySystem energySystem;
    [SerializeField] private FeedbackSystem feedbackSystem;

    public DailyRewardSystem RewardSystem => dailyRewardSystem;

    private void Awake()
    {
        if (playerMetaProgress == null)
        {
            playerMetaProgress = FindObjectOfType<PlayerMetaProgress>();
        }

        if (energySystem == null)
        {
            energySystem = FindObjectOfType<EnergySystem>();
        }

        dailyRewardSystem?.Initialize();

        if (dailyRewardSystem != null)
        {
            dailyRewardSystem.OnRewardClaimed += HandleRewardClaimed;
        }
    }

    private void OnDestroy()
    {
        if (dailyRewardSystem != null)
        {
            dailyRewardSystem.OnRewardClaimed -= HandleRewardClaimed;
        }
    }

    public bool ClaimReward(int day)
    {
        if (dailyRewardSystem == null)
        {
            return false;
        }

        return dailyRewardSystem.ClaimReward(day, playerMetaProgress, energySystem);
    }

    private void HandleRewardClaimed(DailyRewardSystem.DailyReward reward, int finalAmount)
    {
        feedbackSystem?.ShowStatus($"Получена ежедневная награда: {reward.rewardType} x{finalAmount}");
    }
}
