using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class AbilitySystem : MonoBehaviour
{
    [Serializable]
    public class Ability
    {
        public string abilityName;
        [TextArea] public string description;
        public int balanceIndex;
        public EnergySystem.EnergyType[] requiredEnergies;
        public int[] requiredAmounts;
        public bool combinedAbility;
        [Min(1)] public int level = 1;
        public bool unlocked;

        [NonSerialized] public float currentCooldown;

        public bool IsReady => currentCooldown <= 0f;

        public void UpdateCooldown(float deltaTime)
        {
            currentCooldown = Mathf.Max(0f, currentCooldown - deltaTime);
        }
    }

    [SerializeField] private BalanceConfig balanceConfig;
    [SerializeField] private EnergySystem energySystem;
    [SerializeField] private FeedbackSystem feedbackSystem;
    [SerializeField] private List<Ability> abilities = new List<Ability>();

    private float comboTimer;
    private int comboChainLength;
    private readonly HashSet<EnergySystem.EnergyType> discoveredEnergyTypes = new HashSet<EnergySystem.EnergyType>();

    public IReadOnlyList<Ability> Abilities => abilities;
    public int CurrentComboChainLength => comboChainLength;

    private void Awake()
    {
        if (abilities.Count == 0)
        {
            CreateDefaultAbilities();
        }

        SyncAbilityCostsFromConfig();
        RefreshAbilityUnlocks();
    }

    private void OnEnable()
    {
        if (energySystem != null)
        {
            energySystem.EnergyCollected += HandleEnergyCollected;
        }
    }

    private void OnDisable()
    {
        if (energySystem != null)
        {
            energySystem.EnergyCollected -= HandleEnergyCollected;
        }
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;

        foreach (Ability ability in abilities)
        {
            ability.UpdateCooldown(deltaTime);
        }

        HandleComboTimer(deltaTime);
        HandleDebugInput();
    }

    [ContextMenu("Create Default Abilities")]
    public void CreateDefaultAbilities()
    {
        abilities = new List<Ability>
        {
            new Ability
            {
                abilityName = "Огненный шар",
                description = "Тратит красную энергию для атаки.",
                balanceIndex = 0,
                requiredEnergies = new[] { EnergySystem.EnergyType.Red },
                requiredAmounts = new[] { 10 }
            },
            new Ability
            {
                abilityName = "Ледяной щит",
                description = "Тратит синюю энергию для защиты.",
                balanceIndex = 1,
                requiredEnergies = new[] { EnergySystem.EnergyType.Blue },
                requiredAmounts = new[] { 20 }
            },
            new Ability
            {
                abilityName = "Импульс восстановления",
                description = "Тратит зелёную энергию на восстановление.",
                balanceIndex = 2,
                requiredEnergies = new[] { EnergySystem.EnergyType.Green },
                requiredAmounts = new[] { 18 }
            },
            new Ability
            {
                abilityName = "Энергетический взрыв",
                description = "Комбинирует красную и зелёную энергию.",
                balanceIndex = 3,
                combinedAbility = true,
                requiredEnergies = new[] { EnergySystem.EnergyType.Red, EnergySystem.EnergyType.Green },
                requiredAmounts = new[] { 15, 10 }
            },
            new Ability
            {
                abilityName = "Природный резонанс",
                description = "Комбинирует синюю и зелёную энергию для усиления.",
                balanceIndex = 4,
                combinedAbility = true,
                requiredEnergies = new[] { EnergySystem.EnergyType.Blue, EnergySystem.EnergyType.Green },
                requiredAmounts = new[] { 16, 14 }
            },
            new Ability
            {
                abilityName = "Шторм триединства",
                description = "Использует все три типа энергии и получает максимум от комбо.",
                balanceIndex = 5,
                combinedAbility = true,
                requiredEnergies = new[] { EnergySystem.EnergyType.Red, EnergySystem.EnergyType.Blue, EnergySystem.EnergyType.Green },
                requiredAmounts = new[] { 14, 14, 14 }
            }
        };

        SyncAbilityCostsFromConfig();
        RefreshAbilityUnlocks();
    }

    public bool CanActivate(Ability ability)
    {
        if (ability == null || energySystem == null || !ability.IsReady)
        {
            return false;
        }

        if (!ability.unlocked)
        {
            return false;
        }

        if (ability.requiredEnergies == null || ability.requiredAmounts == null)
        {
            return false;
        }

        if (ability.requiredEnergies.Length != ability.requiredAmounts.Length)
        {
            return false;
        }

        for (int i = 0; i < ability.requiredEnergies.Length; i++)
        {
            if (!energySystem.HasEnoughEnergy(ability.requiredEnergies[i], ability.requiredAmounts[i]))
            {
                return false;
            }
        }

        return true;
    }

    public bool TryActivateAbility(int index)
    {
        if (index < 0 || index >= abilities.Count)
        {
            return false;
        }

        Ability ability = abilities[index];
        if (!ability.unlocked)
        {
            feedbackSystem?.ShowStatus($"Способность {ability.abilityName} ещё не открыта.");
            return false;
        }

        if (!CanActivate(ability))
        {
            feedbackSystem?.ShowStatus($"Нельзя использовать: {ability.abilityName}");
            return false;
        }

        for (int i = 0; i < ability.requiredEnergies.Length; i++)
        {
            energySystem.ConsumeEnergy(ability.requiredEnergies[i], ability.requiredAmounts[i]);
        }

        float comboMultiplier = ApplyComboProgression();
        float resonanceMultiplier = CalculateResonanceMultiplier();
        float totalMultiplier = comboMultiplier * resonanceMultiplier;
        float power = balanceConfig != null ? balanceConfig.GetAbilityPower(ability.balanceIndex, ability.level) : 0f;
        float cooldown = balanceConfig != null ? balanceConfig.GetCooldown(ability.balanceIndex, ability.level) : 1f;

        ability.currentCooldown = cooldown;

        if (resonanceMultiplier > 1f)
        {
            RefundResonanceEnergy(ability);
        }

        feedbackSystem?.ShowAbilityUsed(ability.abilityName, cooldown, totalMultiplier, comboChainLength);
        GameEvents.AbilityUsed(ability.abilityName);
        GameEvents.UpdateMaxCombo(comboChainLength);
        Debug.Log(
            $"Ability used: {ability.abilityName}, power={power * totalMultiplier:F1}, " +
            $"comboChain={comboChainLength}, comboMultiplier={comboMultiplier:F2}, resonanceMultiplier={resonanceMultiplier:F2}");

        return true;
    }

    private float ApplyComboProgression()
    {
        if (balanceConfig == null)
        {
            comboChainLength = 1;
            comboTimer = 2f;
            return 1f;
        }

        comboChainLength++;
        comboTimer = balanceConfig.GetComboWindow(comboChainLength);
        return balanceConfig.GetComboMultiplier(comboChainLength - 1);
    }

    private float CalculateResonanceMultiplier()
    {
        if (balanceConfig == null || energySystem == null)
        {
            return 1f;
        }

        foreach (EnergySystem.EnergyType type in Enum.GetValues(typeof(EnergySystem.EnergyType)))
        {
            int current = energySystem.GetCurrentAmount(type);
            int max = energySystem.GetMaxCapacity(type);
            if (max <= 0)
            {
                return 1f;
            }

            if ((float)current / max < balanceConfig.resonanceThresholdNormalized)
            {
                return 1f;
            }
        }

        return balanceConfig.resonancePowerMultiplier;
    }

    private void RefundResonanceEnergy(Ability ability)
    {
        if (balanceConfig == null || energySystem == null)
        {
            return;
        }

        for (int i = 0; i < ability.requiredEnergies.Length; i++)
        {
            int refund = Mathf.RoundToInt(ability.requiredAmounts[i] * balanceConfig.resonanceRefundPercent);
            if (refund > 0)
            {
                energySystem.CollectEnergy(ability.requiredEnergies[i], refund);
            }
        }
    }

    private void HandleComboTimer(float deltaTime)
    {
        if (comboChainLength <= 0)
        {
            return;
        }

        comboTimer -= deltaTime;
        if (comboTimer > 0f)
        {
            return;
        }

        comboChainLength = 0;
        comboTimer = 0f;
        feedbackSystem?.ShowStatus("Комбо-цепочка сброшена.");
    }

    private void HandleDebugInput()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.digit1Key.wasPressedThisFrame) TryActivateAbility(0);
        if (Keyboard.current.digit2Key.wasPressedThisFrame) TryActivateAbility(1);
        if (Keyboard.current.digit3Key.wasPressedThisFrame) TryActivateAbility(2);
        if (Keyboard.current.digit4Key.wasPressedThisFrame) TryActivateAbility(3);
        if (Keyboard.current.digit5Key.wasPressedThisFrame) TryActivateAbility(4);
        if (Keyboard.current.digit6Key.wasPressedThisFrame) TryActivateAbility(5);
    }

    private void SyncAbilityCostsFromConfig()
    {
        if (balanceConfig == null)
        {
            return;
        }

        foreach (Ability ability in abilities)
        {
            if (ability == null || ability.requiredAmounts == null || ability.requiredAmounts.Length == 0)
            {
                continue;
            }

            if (!ability.combinedAbility && ability.balanceIndex >= 0 && ability.balanceIndex < balanceConfig.abilityBaseCosts.Length)
            {
                ability.requiredAmounts[0] = balanceConfig.abilityBaseCosts[ability.balanceIndex];
            }
        }
    }

    private void HandleEnergyCollected(EnergySystem.EnergyType type, int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        if (discoveredEnergyTypes.Add(type))
        {
            RefreshAbilityUnlocks();
        }
    }

    private void RefreshAbilityUnlocks()
    {
        foreach (Ability ability in abilities)
        {
            if (ability == null || ability.requiredEnergies == null || ability.requiredEnergies.Length == 0)
            {
                continue;
            }

            bool wasUnlocked = ability.unlocked;
            ability.unlocked = AreAllRequiredTypesDiscovered(ability);

            if (!wasUnlocked && ability.unlocked)
            {
                feedbackSystem?.ShowStatus($"Открыта способность: {ability.abilityName}");
            }
        }
    }

    private bool AreAllRequiredTypesDiscovered(Ability ability)
    {
        for (int i = 0; i < ability.requiredEnergies.Length; i++)
        {
            if (!discoveredEnergyTypes.Contains(ability.requiredEnergies[i]))
            {
                return false;
            }
        }

        return true;
    }
}
