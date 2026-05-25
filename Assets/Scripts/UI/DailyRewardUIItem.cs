using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DailyRewardUIItem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI dayText;
    [SerializeField] private TextMeshProUGUI rewardText;
    [SerializeField] private Image rewardIcon;
    [SerializeField] private GameObject specialBadge;
    [SerializeField] private GameObject claimedMark;
    [SerializeField] private GameObject availableHighlight;
    [SerializeField] private Button claimButton;

    private DailyRewardManager rewardManager;
    private DailyRewardSystem.DailyReward rewardData;
    private Action refreshCallback;

    public void Initialize(DailyRewardManager manager, DailyRewardSystem.DailyReward reward, Action onRefresh)
    {
        rewardManager = manager;
        rewardData = reward;
        refreshCallback = onRefresh;

        if (claimButton != null)
        {
            claimButton.onClick.RemoveAllListeners();
            claimButton.onClick.AddListener(ClaimReward);
        }

        Refresh();
    }

    public void Refresh()
    {
        if (rewardData == null)
        {
            return;
        }

        if (dayText != null) dayText.text = $"День {rewardData.day}";
        if (rewardText != null) rewardText.text = $"{GetRewardName(rewardData.rewardType)} x{rewardData.rewardAmount}";
        if (rewardIcon != null && rewardData.rewardIcon != null) rewardIcon.sprite = rewardData.rewardIcon;
        if (specialBadge != null) specialBadge.SetActive(rewardData.isSpecial);
        if (claimedMark != null) claimedMark.SetActive(rewardData.isClaimed);
        if (availableHighlight != null) availableHighlight.SetActive(rewardData.isAvailable && !rewardData.isClaimed);
        if (claimButton != null) claimButton.interactable = rewardManager != null && rewardManager.RewardSystem.CanClaimReward(rewardData.day);
    }

    private void ClaimReward()
    {
        if (rewardManager == null || rewardData == null)
        {
            return;
        }

        rewardManager.ClaimReward(rewardData.day);
        refreshCallback?.Invoke();
    }

    private string GetRewardName(DailyRewardSystem.RewardType rewardType)
    {
        switch (rewardType)
        {
            case DailyRewardSystem.RewardType.Currency:
                return "Монеты";
            case DailyRewardSystem.RewardType.XP:
                return "XP";
            case DailyRewardSystem.RewardType.RedEnergy:
                return "Красная энергия";
            case DailyRewardSystem.RewardType.BlueEnergy:
                return "Синяя энергия";
            case DailyRewardSystem.RewardType.GreenEnergy:
                return "Зелёная энергия";
            default:
                return rewardType.ToString();
        }
    }
}
