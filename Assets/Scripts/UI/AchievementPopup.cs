using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AchievementPopup : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private float lifetime = 2.5f;

    public void Show(AchievementSystem.Achievement achievement)
    {
        if (achievement == null)
        {
            return;
        }

        if (iconImage != null && achievement.icon != null)
        {
            iconImage.sprite = achievement.icon;
        }

        if (titleText != null)
        {
            titleText.text = achievement.title;
        }

        if (descriptionText != null)
        {
            descriptionText.text = achievement.description;
        }

        StartCoroutine(DestroyAfterDelay());
    }

    private IEnumerator DestroyAfterDelay()
    {
        yield return new WaitForSeconds(lifetime);
        Destroy(gameObject);
    }
}
