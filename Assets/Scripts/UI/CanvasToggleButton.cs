using TMPro;
using UnityEngine;

public class CanvasToggleButton : MonoBehaviour
{
    [SerializeField] private GameObject targetPanel;
    [SerializeField] private TMP_Text buttonLabel;
    [SerializeField] private string expandedText = "Скрыть";
    [SerializeField] private string collapsedText = "Показать";
    [SerializeField] private bool startExpanded = true;

    private void Start()
    {
        if (targetPanel != null)
        {
            targetPanel.SetActive(startExpanded);
        }

        UpdateLabel();
    }

    public void TogglePanel()
    {
        if (targetPanel == null)
        {
            return;
        }

        targetPanel.SetActive(!targetPanel.activeSelf);
        UpdateLabel();
    }

    private void UpdateLabel()
    {
        if (buttonLabel == null)
        {
            return;
        }

        bool isExpanded = targetPanel != null && targetPanel.activeSelf;
        buttonLabel.text = isExpanded ? expandedText : collapsedText;
    }
}
