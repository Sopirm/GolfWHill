using UnityEngine;

[CreateAssetMenu(fileName = "BalanceConfig", menuName = "Game/Balance Config")]
public class BalanceConfig : ScriptableObject
{
    [Header("Collection")]
    [Min(1)] public int baseEnergyPerCollect = 10;
    [Min(10)] public int baseCapacityPerType = 100;

    [Header("Ability Progression")]
    [Min(1f)] public float abilityCostGrowth = 1.2f;
    [Range(0f, 0.5f)] public float cooldownReductionPerLevel = 0.05f;
    [Min(0.25f)] public float minimumCooldown = 0.5f;
    [Min(1)] public int maxAbilityLevel = 10;

    [Header("Base Ability Values")]
    public int[] abilityBaseCosts = new int[] { 10, 20, 18, 25, 30, 36 };
    public float[] abilityCooldowns = new float[] { 2f, 3f, 4f, 5f, 6f, 8f };
    public float[] abilityBasePower = new float[] { 20f, 16f, 14f, 40f, 45f, 70f };

    [Header("Energy Degradation")]
    public bool enableEnergyDegradation = true;
    [Min(0.5f)] public float degradationInterval = 4f;
    [Range(0.01f, 0.5f)] public float degradationPercent = 0.1f;

    [Header("Combo System")]
    [Min(0f)] public float comboBaseBonus = 1f;
    [Range(0f, 1f)] public float comboBonusPerStep = 0.3f;
    [Min(0.5f)] public float comboBaseWindow = 2f;
    [Min(0f)] public float comboWindowPerStep = 0.5f;

    [Header("Custom Mechanic: Resonance")]
    [Range(0.1f, 1f)] public float resonanceThresholdNormalized = 0.6f;
    [Min(1f)] public float resonancePowerMultiplier = 1.25f;
    [Range(0f, 1f)] public float resonanceRefundPercent = 0.2f;

    [Header("Balance Curves")]
    public AnimationCurve difficultyCurve = AnimationCurve.Linear(0f, 1f, 1f, 2f);
    public AnimationCurve rewardCurve = AnimationCurve.Linear(0f, 1f, 1f, 1.6f);
    public AnimationCurve progressionCurve = AnimationCurve.Linear(0f, 1f, 1f, 2f);

    public int CalculateAbilityCost(int abilityIndex, int level)
    {
        if (abilityIndex < 0 || abilityIndex >= abilityBaseCosts.Length)
        {
            return 0;
        }

        int clampedLevel = Mathf.Clamp(level, 1, maxAbilityLevel);
        int baseCost = abilityBaseCosts[abilityIndex];
        return Mathf.RoundToInt(baseCost * Mathf.Pow(abilityCostGrowth, clampedLevel - 1));
    }

    public float GetCooldown(int abilityIndex, int level)
    {
        if (abilityIndex < 0 || abilityIndex >= abilityCooldowns.Length)
        {
            return minimumCooldown;
        }

        int clampedLevel = Mathf.Clamp(level, 1, maxAbilityLevel);
        float baseCooldown = abilityCooldowns[abilityIndex];
        float scaledCooldown = baseCooldown * (1f - ((clampedLevel - 1) * cooldownReductionPerLevel));
        return Mathf.Max(minimumCooldown, scaledCooldown);
    }

    public float GetAbilityPower(int abilityIndex, int level)
    {
        if (abilityIndex < 0 || abilityIndex >= abilityBasePower.Length)
        {
            return 0f;
        }

        int clampedLevel = Mathf.Clamp(level, 1, maxAbilityLevel);
        float multiplier = rewardCurve == null ? 1f : rewardCurve.Evaluate((clampedLevel - 1f) / Mathf.Max(1f, maxAbilityLevel - 1f));
        return abilityBasePower[abilityIndex] * Mathf.Max(1f, multiplier);
    }

    public float GetCapacityMultiplier(int playerLevel)
    {
        if (progressionCurve == null)
        {
            return 1f;
        }

        float normalizedLevel = Mathf.Clamp01(playerLevel / 10f);
        return Mathf.Max(1f, progressionCurve.Evaluate(normalizedLevel));
    }

    public float GetComboWindow(int chainLength)
    {
        return comboBaseWindow + (chainLength * comboWindowPerStep);
    }

    public float GetComboMultiplier(int chainLength)
    {
        return comboBaseBonus * (1f + (chainLength * comboBonusPerStep));
    }
}
