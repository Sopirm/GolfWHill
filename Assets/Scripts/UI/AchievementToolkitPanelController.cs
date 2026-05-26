using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class AchievementToolkitPanelController : MonoBehaviour
{
    [SerializeField] private AchievementSystem achievementSystem;
    [SerializeField] private PlayerMetaProgress playerMetaProgress;
    [SerializeField] private bool startOpened;

    private UIDocument uiDocument;
    private AdaptiveUIDocument adaptiveUIDocument;
    private VisualElement overlay;
    private ScrollView windowScroll;
    private Label platformValue;
    private Label controlHintValue;
    private Label levelValue;
    private Label xpValue;
    private Label currencyValue;
    private Label totalValue;
    private Label unlockedValue;
    private Label completionValue;
    private VisualElement achievementList;
    private Button closeButton;
    private bool isBuilt;

    private void Awake()
    {
        uiDocument = GetComponent<UIDocument>();
        adaptiveUIDocument = GetComponent<AdaptiveUIDocument>();

        if (playerMetaProgress == null)
        {
            playerMetaProgress = FindObjectOfType<PlayerMetaProgress>();
        }
    }

    private void OnEnable()
    {
        BuildUI();
        BindData();
        SetOpen(startOpened);
    }

    private void OnDisable()
    {
        UnbindData();
    }

    public void TogglePanel()
    {
        SetOpen(overlay == null || overlay.style.display == DisplayStyle.None);
    }

    public void OpenPanel()
    {
        SetOpen(true);
    }

    public void ClosePanel()
    {
        SetOpen(false);
    }

    private void BuildUI()
    {
        if (isBuilt || uiDocument == null)
        {
            return;
        }

        VisualElement root = uiDocument.rootVisualElement;
        overlay = root.Q<VisualElement>("achievement-overlay");
        windowScroll = root.Q<ScrollView>("achievement-window-scroll");
        platformValue = root.Q<Label>("platform-value");
        controlHintValue = root.Q<Label>("control-hint-value");
        levelValue = root.Q<Label>("level-value");
        xpValue = root.Q<Label>("xp-value");
        currencyValue = root.Q<Label>("currency-value");
        totalValue = root.Q<Label>("total-value");
        unlockedValue = root.Q<Label>("unlocked-value");
        completionValue = root.Q<Label>("completion-value");
        achievementList = root.Q<VisualElement>("achievement-list");
        closeButton = root.Q<Button>("close-button");

        if (closeButton != null)
        {
            closeButton.clicked += ClosePanel;
        }

        isBuilt = true;
    }

    private void BindData()
    {
        if (achievementSystem != null)
        {
            achievementSystem.Initialize();
            achievementSystem.OnAchievementUnlocked += HandleAchievementChanged;
            achievementSystem.OnAchievementProgressChanged += HandleAchievementChanged;
        }

        if (playerMetaProgress != null)
        {
            playerMetaProgress.OnXPChanged += HandleXPChanged;
            playerMetaProgress.OnCurrencyChanged += HandleCurrencyChanged;
            playerMetaProgress.OnLevelChanged += HandleLevelChanged;
        }

        RefreshAll();
    }

    private void UnbindData()
    {
        if (achievementSystem != null)
        {
            achievementSystem.OnAchievementUnlocked -= HandleAchievementChanged;
            achievementSystem.OnAchievementProgressChanged -= HandleAchievementChanged;
        }

        if (playerMetaProgress != null)
        {
            playerMetaProgress.OnXPChanged -= HandleXPChanged;
            playerMetaProgress.OnCurrencyChanged -= HandleCurrencyChanged;
            playerMetaProgress.OnLevelChanged -= HandleLevelChanged;
        }
    }

    private void RefreshAll()
    {
        RefreshMetaProgress();
        RefreshStats();
        RefreshMenuInfo();
        RebuildAchievementList();
    }

    private void RefreshMenuInfo()
    {
        if (platformValue != null)
        {
            platformValue.text = adaptiveUIDocument != null ? adaptiveUIDocument.GetPlatformDisplayName() : "PC";
        }

        if (controlHintValue != null)
        {
            controlHintValue.text = GetControlHintText();
        }
    }

    private void RefreshMetaProgress()
    {
        if (playerMetaProgress == null)
        {
            return;
        }

        if (levelValue != null)
        {
            levelValue.text = playerMetaProgress.Level.ToString();
        }

        if (xpValue != null)
        {
            xpValue.text = $"{playerMetaProgress.CurrentXP}/{playerMetaProgress.GetXPForNextLevel()}";
        }

        if (currencyValue != null)
        {
            currencyValue.text = playerMetaProgress.Currency.ToString();
        }
    }

    private void RefreshStats()
    {
        if (achievementSystem == null)
        {
            return;
        }

        AchievementSystem.AchievementStats stats = achievementSystem.GetStats();

        if (totalValue != null)
        {
            totalValue.text = stats.totalAchievements.ToString();
        }

        if (unlockedValue != null)
        {
            unlockedValue.text = stats.unlockedAchievements.ToString();
        }

        if (completionValue != null)
        {
            completionValue.text = $"{stats.completionPercentage:F0}%";
        }
    }

    private void RebuildAchievementList()
    {
        if (achievementSystem == null || achievementList == null)
        {
            return;
        }

        achievementList.Clear();

        List<AchievementSystem.Achievement> achievements = achievementSystem.GetAchievements();
        foreach (AchievementSystem.Achievement achievement in achievements)
        {
            achievementList.Add(CreateAchievementCard(achievement));
        }
    }

    private VisualElement CreateAchievementCard(AchievementSystem.Achievement achievement)
    {
        VisualElement card = new VisualElement();
        card.AddToClassList("achievement-card");
        card.style.display = DisplayStyle.Flex;
        if (achievement.isUnlocked)
        {
            card.AddToClassList("achievement-card-unlocked");
        }

        Label title = new Label(achievement.isHidden && !achievement.isUnlocked ? "???" : achievement.title);
        title.AddToClassList("achievement-title");
        card.Add(title);

        string descriptionText = achievement.isHidden && !achievement.isUnlocked
            ? "Скрытое достижение"
            : achievement.description;
        Label description = new Label(descriptionText);
        description.AddToClassList("achievement-description");
        card.Add(description);

        float progress = achievement.targetValue > 0f
            ? Mathf.Clamp01(achievement.currentValue / achievement.targetValue)
            : 0f;
        string progressText = achievement.isUnlocked
            ? "Завершено"
            : $"{achievement.currentValue:F0}/{achievement.targetValue:F0}";
        Label progressLabel = new Label(progressText);
        progressLabel.AddToClassList("achievement-progress");
        card.Add(progressLabel);

        VisualElement progressTrack = new VisualElement();
        progressTrack.AddToClassList("progress-track");
        VisualElement progressFill = new VisualElement();
        progressFill.AddToClassList("progress-fill");
        progressFill.style.width = Length.Percent(progress * 100f);
        progressTrack.Add(progressFill);
        card.Add(progressTrack);

        Label reward = new Label($"+{achievement.rewardXP} XP  |  +{achievement.rewardCurrency} coins");
        reward.text = $"+{achievement.rewardXP} XP  |  +{achievement.rewardCurrency} монет";
        reward.AddToClassList("achievement-reward");
        card.Add(reward);

        return card;
    }

    private void SetOpen(bool opened)
    {
        if (overlay == null)
        {
            return;
        }

        overlay.style.display = opened ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void HandleAchievementChanged(AchievementSystem.Achievement achievement)
    {
        RefreshStats();
        RebuildAchievementList();
    }

    private void HandleXPChanged(int currentXP, int nextLevelXP, int level)
    {
        RefreshMetaProgress();
    }

    private void HandleCurrencyChanged(int currency)
    {
        RefreshMetaProgress();
    }

    private void HandleLevelChanged(int level)
    {
        RefreshMetaProgress();
    }

    private string GetControlHintText()
    {
        if (adaptiveUIDocument != null)
        {
            string platform = adaptiveUIDocument.GetPlatformDisplayName();
            if (platform.Contains("Android"))
            {
                return "Мобильный режим: используйте джойстик и кнопки Cam / Use / Door / Brake.";
            }

            if (platform.Contains("WebGL"))
            {
                return "WebGL режим: WASD для движения, C для камеры, правая кнопка мыши для взаимодействия.";
            }
        }

        return "PC режим: WASD для движения, C для камеры, G для двери, ПКМ для взаимодействия.";
    }
}
