using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public class BalanceTester : MonoBehaviour
{
    [System.Serializable]
    public class BalanceTestResult
    {
        public int level;
        public string abilityName;
        public float timeToAfford;
        public float power;
        public float cooldown;
        public float efficiency;
        public float totalCost;
    }

    [SerializeField] private BalanceConfig balanceConfig;
    [SerializeField] private int testLevels = 10;
    [SerializeField] private float collectionRate = 5f;
    [SerializeField] private bool runOnStart = true;

    private readonly List<BalanceTestResult> testResults = new List<BalanceTestResult>();
    private readonly string[] abilityNames =
    {
        "Fireball",
        "IceShield",
        "HealingPulse",
        "EnergyBlast",
        "NatureResonance",
        "TrinityStorm"
    };

    private void Start()
    {
        if (runOnStart)
        {
            RunAllTests();
        }
    }

    [ContextMenu("Run Balance Tests")]
    public void RunAllTests()
    {
        if (balanceConfig == null)
        {
            Debug.LogWarning("BalanceTester: BalanceConfig is not assigned.");
            return;
        }

        RunBalanceTests();
        AnalyzeResults();
        ExportToCsv();
    }

    private void RunBalanceTests()
    {
        testResults.Clear();

        for (int level = 1; level <= testLevels; level++)
        {
            for (int abilityIndex = 0; abilityIndex < balanceConfig.abilityBaseCosts.Length; abilityIndex++)
            {
                int cost = balanceConfig.CalculateAbilityCost(abilityIndex, level);
                float cooldown = balanceConfig.GetCooldown(abilityIndex, level);
                float power = balanceConfig.GetAbilityPower(abilityIndex, level);
                float efficiency = cooldown > 0f ? power / cooldown : 0f;
                float timeToAfford = collectionRate > 0f ? cost / collectionRate : 0f;

                testResults.Add(new BalanceTestResult
                {
                    level = level,
                    abilityName = GetAbilityName(abilityIndex),
                    timeToAfford = timeToAfford,
                    power = power,
                    cooldown = cooldown,
                    efficiency = efficiency,
                    totalCost = cost
                });
            }
        }
    }

    private void AnalyzeResults()
    {
        if (testResults.Count <= 1)
        {
            return;
        }

        float averageTimeIncrease = 0f;
        float averageEfficiencyIncrease = 0f;
        int growthChecks = 0;

        for (int i = 1; i < testResults.Count; i++)
        {
            BalanceTestResult previous = testResults[i - 1];
            BalanceTestResult current = testResults[i];

            if (previous.abilityName != current.abilityName)
            {
                continue;
            }

            float timeGrowth = previous.timeToAfford <= 0f ? 1f : current.timeToAfford / previous.timeToAfford;
            float efficiencyGrowth = previous.efficiency <= 0f ? 1f : current.efficiency / previous.efficiency;

            averageTimeIncrease += timeGrowth;
            averageEfficiencyIncrease += efficiencyGrowth;
            growthChecks++;

            Debug.Log(
                $"{current.abilityName} L{current.level}: " +
                $"time={current.timeToAfford:F1}s ({(timeGrowth - 1f) * 100f:F0}%), " +
                $"efficiency={current.efficiency:F1} ({(efficiencyGrowth - 1f) * 100f:F0}%)");
        }

        if (growthChecks == 0)
        {
            return;
        }

        averageTimeIncrease /= growthChecks;
        averageEfficiencyIncrease /= growthChecks;

        Debug.Log($"Average time growth: {(averageTimeIncrease - 1f) * 100f:F1}% per level");
        Debug.Log($"Average efficiency growth: {(averageEfficiencyIncrease - 1f) * 100f:F1}% per level");

        if (averageTimeIncrease > 1.35f)
        {
            Debug.LogWarning("Balance warning: time to afford grows too fast.");
        }

        if (averageEfficiencyIncrease < 1.08f)
        {
            Debug.LogWarning("Balance warning: power grows too slowly.");
        }
    }

    private void ExportToCsv()
    {
        StringBuilder csv = new StringBuilder();
        csv.AppendLine("Level,Ability,TimeToAfford,Power,Cooldown,Efficiency,TotalCost");

        foreach (BalanceTestResult result in testResults)
        {
            csv.AppendLine(
                $"{result.level},{result.abilityName},{result.timeToAfford:F2},{result.power:F2},{result.cooldown:F2},{result.efficiency:F2},{result.totalCost:F0}");
        }

        string filePath = Path.Combine(Application.dataPath, "Docs", "BalanceTestResults.csv");
        File.WriteAllText(filePath, csv.ToString());
        Debug.Log($"BalanceTester: results saved to {filePath}");
    }

    private string GetAbilityName(int abilityIndex)
    {
        if (abilityIndex >= 0 && abilityIndex < abilityNames.Length)
        {
            return abilityNames[abilityIndex];
        }

        return $"Ability{abilityIndex}";
    }
}
