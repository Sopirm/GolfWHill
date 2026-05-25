using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AchievementUI : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private AchievementSystem achievementSystem;

    [Header("Prefabs")]
    [SerializeField] private GameObject achievementItemPrefab;
    [SerializeField] private GameObject achievementPopupPrefab;

    [Header("Containers")]
    [SerializeField] private Transform achievementListContainer;
    [SerializeField] private Transform popupContainer;

    [Header("Stats")]
    [SerializeField] private TextMeshProUGUI totalText;
    [SerializeField] private TextMeshProUGUI unlockedText;
    [SerializeField] private TextMeshProUGUI percentageText;
    [SerializeField] private Image progressBar;

    [Header("Filters")]
    [SerializeField] private Toggle showAllToggle;
    [SerializeField] private Toggle showUnlockedToggle;
    [SerializeField] private Toggle showLockedToggle;

    [Header("Sorting")]
    [SerializeField] private TMP_Dropdown sortDropdown;

    private readonly List<AchievementUIItem> uiItems = new List<AchievementUIItem>();

    private void Start()
    {
        if (achievementSystem == null)
        {
            return;
        }

        achievementSystem.Initialize();
        achievementSystem.OnAchievementUnlocked += HandleAchievementUnlocked;
        achievementSystem.OnAchievementProgressChanged += HandleAchievementProgressChanged;

        BuildList();
        HookFilters();
        RefreshUI();
    }

    private void OnDestroy()
    {
        if (achievementSystem == null)
        {
            return;
        }

        achievementSystem.OnAchievementUnlocked -= HandleAchievementUnlocked;
        achievementSystem.OnAchievementProgressChanged -= HandleAchievementProgressChanged;
    }

    public void RefreshUI()
    {
        if (achievementSystem == null)
        {
            return;
        }

        AchievementSystem.AchievementStats stats = achievementSystem.GetStats();
        if (totalText != null) totalText.text = $"Всего: {stats.totalAchievements}";
        if (unlockedText != null) unlockedText.text = $"Открыто: {stats.unlockedAchievements}";
        if (percentageText != null) percentageText.text = $"{stats.completionPercentage:F1}%";
        if (progressBar != null) progressBar.fillAmount = stats.completionPercentage / 100f;

        bool showAll = showAllToggle == null || showAllToggle.isOn;
        bool showUnlocked = showUnlockedToggle != null && showUnlockedToggle.isOn;
        bool showLocked = showLockedToggle != null && showLockedToggle.isOn;

        foreach (AchievementUIItem item in uiItems)
        {
            if (item == null)
            {
                continue;
            }

            bool visible = showAll
                || (showUnlocked && item.Achievement.isUnlocked)
                || (showLocked && !item.Achievement.isUnlocked);

            item.gameObject.SetActive(visible);
            item.UpdateUI();
        }
    }

    private void BuildList()
    {
        if (achievementItemPrefab == null || achievementListContainer == null)
        {
            return;
        }

        foreach (Transform child in achievementListContainer)
        {
            Destroy(child.gameObject);
        }

        uiItems.Clear();

        foreach (AchievementSystem.Achievement achievement in achievementSystem.GetAchievements())
        {
            GameObject itemObject = Instantiate(achievementItemPrefab, achievementListContainer);
            AchievementUIItem uiItem = itemObject.GetComponent<AchievementUIItem>();
            if (uiItem == null)
            {
                continue;
            }

            uiItem.Initialize(achievement);
            uiItems.Add(uiItem);
        }
    }

    private void HookFilters()
    {
        if (showAllToggle != null)
        {
            showAllToggle.onValueChanged.AddListener(_ => RefreshUI());
        }

        if (showUnlockedToggle != null)
        {
            showUnlockedToggle.onValueChanged.AddListener(_ => RefreshUI());
        }

        if (showLockedToggle != null)
        {
            showLockedToggle.onValueChanged.AddListener(_ => RefreshUI());
        }

        if (sortDropdown != null)
        {
            sortDropdown.onValueChanged.AddListener(SortAchievements);
        }
    }

    private void SortAchievements(int sortType)
    {
        switch (sortType)
        {
            case 0:
                uiItems.Sort((a, b) => b.Achievement.UnlockTime.CompareTo(a.Achievement.UnlockTime));
                break;
            case 1:
                uiItems.Sort((a, b) =>
                    (b.Achievement.currentValue / Mathf.Max(1f, b.Achievement.targetValue))
                    .CompareTo(a.Achievement.currentValue / Mathf.Max(1f, a.Achievement.targetValue)));
                break;
            case 2:
                uiItems.Sort((a, b) => b.Achievement.rewardXP.CompareTo(a.Achievement.rewardXP));
                break;
            case 3:
                uiItems.Sort((a, b) => string.Compare(a.Achievement.title, b.Achievement.title, System.StringComparison.Ordinal));
                break;
        }

        for (int i = 0; i < uiItems.Count; i++)
        {
            uiItems[i].transform.SetSiblingIndex(i);
        }
    }

    private void HandleAchievementUnlocked(AchievementSystem.Achievement achievement)
    {
        if (achievementPopupPrefab != null && popupContainer != null)
        {
            GameObject popupObject = Instantiate(achievementPopupPrefab, popupContainer);
            AchievementPopup popup = popupObject.GetComponent<AchievementPopup>();
            if (popup != null)
            {
                popup.Show(achievement);
            }
        }

        RefreshUI();
    }

    private void HandleAchievementProgressChanged(AchievementSystem.Achievement achievement)
    {
        RefreshUI();
    }
}
