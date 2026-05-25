using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerMetaProgressUI : MonoBehaviour
{
    [SerializeField] private PlayerMetaProgress playerMetaProgress;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI xpText;
    [SerializeField] private TextMeshProUGUI currencyText;
    [SerializeField] private Image xpBar;

    private void Start()
    {
        if (playerMetaProgress == null)
        {
            playerMetaProgress = FindObjectOfType<PlayerMetaProgress>();
        }

        if (playerMetaProgress == null)
        {
            return;
        }

        playerMetaProgress.OnXPChanged += HandleXPChanged;
        playerMetaProgress.OnCurrencyChanged += HandleCurrencyChanged;
        playerMetaProgress.OnLevelChanged += HandleLevelChanged;

        RefreshAll();
    }

    private void OnDestroy()
    {
        if (playerMetaProgress == null)
        {
            return;
        }

        playerMetaProgress.OnXPChanged -= HandleXPChanged;
        playerMetaProgress.OnCurrencyChanged -= HandleCurrencyChanged;
        playerMetaProgress.OnLevelChanged -= HandleLevelChanged;
    }

    private void RefreshAll()
    {
        HandleLevelChanged(playerMetaProgress.Level);
        HandleCurrencyChanged(playerMetaProgress.Currency);
        HandleXPChanged(playerMetaProgress.CurrentXP, playerMetaProgress.GetXPForNextLevel(), playerMetaProgress.Level);
    }

    private void HandleXPChanged(int currentXP, int nextLevelXP, int level)
    {
        if (xpText != null)
        {
            xpText.text = $"XP: {currentXP}/{nextLevelXP}";
        }

        if (xpBar != null)
        {
            xpBar.fillAmount = nextLevelXP > 0 ? (float)currentXP / nextLevelXP : 0f;
        }
    }

    private void HandleCurrencyChanged(int currency)
    {
        if (currencyText != null)
        {
            currencyText.text = $"Монеты: {currency}";
        }
    }

    private void HandleLevelChanged(int level)
    {
        if (levelText != null)
        {
            levelText.text = $"Уровень: {level}";
        }
    }
}
