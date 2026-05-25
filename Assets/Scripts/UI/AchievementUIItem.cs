using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AchievementUIItem : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private Image progressBar;
    [SerializeField] private GameObject lockedOverlay;
    [SerializeField] private GameObject unlockedBadge;
    [SerializeField] private TextMeshProUGUI rewardText;

    public AchievementSystem.Achievement Achievement { get; private set; }

    public void Initialize(AchievementSystem.Achievement achievement)
    {
        Achievement = achievement;
        UpdateUI();
    }

    public void UpdateUI()
    {
        if (Achievement == null)
        {
            return;
        }

        if (titleText != null) titleText.text = Achievement.title;
        if (descriptionText != null) descriptionText.text = Achievement.description;
        if (rewardText != null)
        {
            rewardText.text = $"+{Achievement.rewardXP} XP";
            if (Achievement.rewardCurrency > 0)
            {
                rewardText.text += $" | +{Achievement.rewardCurrency} монет";
            }
        }

        if (iconImage != null && Achievement.icon != null)
        {
            iconImage.sprite = Achievement.icon;
            iconImage.color = Color.white;
        }

        if (Achievement.isHidden && !Achievement.isUnlocked)
        {
            if (titleText != null) titleText.text = "???";
            if (descriptionText != null) descriptionText.text = "Скрытое достижение";
            if (progressText != null) progressText.text = "???";
            if (iconImage != null) iconImage.color = Color.black;
        }
        else if (Achievement.isUnlocked)
        {
            if (progressText != null) progressText.text = "Завершено!";
            if (progressBar != null) progressBar.fillAmount = 1f;
        }
        else
        {
            float progress = Achievement.currentValue / Mathf.Max(1f, Achievement.targetValue);
            if (progressText != null) progressText.text = $"{Achievement.currentValue:F0}/{Achievement.targetValue:F0}";
            if (progressBar != null) progressBar.fillAmount = progress;
        }

        if (lockedOverlay != null) lockedOverlay.SetActive(!Achievement.isUnlocked);
        if (unlockedBadge != null) unlockedBadge.SetActive(Achievement.isUnlocked);
    }
}
