using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FeedbackSystem : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI energyText;
    [SerializeField] private TextMeshProUGUI abilityText;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private GameObject energyGainPrefab;
    [SerializeField] private Transform feedbackCanvas;

    [Header("Audio")]
    [SerializeField] private AudioClip collectSound;
    [SerializeField] private AudioClip abilitySound;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public void ShowEnergyGain(Vector3 worldPosition, string energyType, int amount)
    {
        if (energyGainPrefab != null && feedbackCanvas != null && Camera.main != null)
        {
            Vector3 screenPosition = Camera.main.WorldToScreenPoint(worldPosition);
            GameObject feedback = Instantiate(energyGainPrefab, feedbackCanvas);
            feedback.transform.position = screenPosition;

            TextMeshProUGUI text = feedback.GetComponent<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = $"+{amount} {energyType}";
            }

            Destroy(feedback, 1f);
        }

        PlayClip(collectSound);
        ShowStatus($"Собрано: +{amount} {energyType}");
    }

    public void UpdateEnergyDisplay(Dictionary<EnergySystem.EnergyType, int> energies)
    {
        if (energyText == null)
        {
            return;
        }

        List<string> lines = new List<string>();
        foreach (KeyValuePair<EnergySystem.EnergyType, int> pair in energies)
        {
            lines.Add($"{pair.Key}: {pair.Value}");
        }

        energyText.text = string.Join("\n", lines);
    }

    public void ShowAbilityUsed(string abilityName, float cooldown, float multiplier, int comboChain)
    {
        if (abilityText != null)
        {
            abilityText.text =
                $"{abilityName}\n" +
                $"Перезарядка: {cooldown:F1} c\n" +
                $"Множитель: x{multiplier:F2}\n" +
                $"Цепочка: {comboChain}";
            StopAllCoroutines();
            StartCoroutine(FlashAbilityText());
        }

        PlayClip(abilitySound);
    }

    public void ShowStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    private IEnumerator FlashAbilityText()
    {
        if (abilityText == null)
        {
            yield break;
        }

        Color initialColor = abilityText.color;
        abilityText.color = Color.yellow;
        yield return new WaitForSeconds(0.4f);
        abilityText.color = initialColor;
    }

    private void PlayClip(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}
