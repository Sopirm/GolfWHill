using System;
using System.Collections.Generic;
using UnityEngine;

public class EnergySystem : MonoBehaviour
{
    public enum EnergyType
    {
        Red,
        Blue,
        Green
    }

    [Serializable]
    public class EnergyStorage
    {
        public EnergyType type;
        public int currentAmount;
        public int maxCapacity;

        public int Add(int amount)
        {
            int addedAmount = Mathf.Clamp(amount, 0, Mathf.Max(0, maxCapacity - currentAmount));
            currentAmount += addedAmount;
            return addedAmount;
        }

        public bool TryConsume(int amount)
        {
            if (amount <= 0 || currentAmount < amount)
            {
                return false;
            }

            currentAmount -= amount;
            return true;
        }
    }

    [SerializeField] private BalanceConfig balanceConfig;
    [SerializeField] private FeedbackSystem feedbackSystem;
    [SerializeField] private List<EnergyStorage> energyStorages = new List<EnergyStorage>();

    private readonly Dictionary<EnergyType, EnergyStorage> storageByType = new Dictionary<EnergyType, EnergyStorage>();
    private float degradationTimer;

    public event Action<EnergyType, int, int> EnergyChanged;
    public event Action<EnergyType, int> EnergyCollected;

    public IReadOnlyList<EnergyStorage> Storages => energyStorages;

    private void Awake()
    {
        InitializeEnergyTypes();
    }

    private void Update()
    {
        HandleEnergyDegradation(Time.deltaTime);
    }

    public void InitializeEnergyTypes()
    {
        storageByType.Clear();

        if (energyStorages.Count == 0)
        {
            foreach (EnergyType type in Enum.GetValues(typeof(EnergyType)))
            {
                energyStorages.Add(new EnergyStorage
                {
                    type = type,
                    currentAmount = 0,
                    maxCapacity = GetDefaultCapacity()
                });
            }
        }

        for (int i = 0; i < energyStorages.Count; i++)
        {
            EnergyStorage storage = energyStorages[i];
            storage.maxCapacity = Mathf.Max(1, storage.maxCapacity);
            storage.currentAmount = Mathf.Clamp(storage.currentAmount, 0, storage.maxCapacity);
            storageByType[storage.type] = storage;
        }

        NotifyAll();
    }

    public bool CollectEnergy(EnergyType type, int amount, Vector3? worldPosition = null)
    {
        if (!storageByType.TryGetValue(type, out EnergyStorage storage))
        {
            return false;
        }

        int collected = storage.Add(amount);
        if (collected <= 0)
        {
            return false;
        }

        EnergyCollected?.Invoke(type, collected);
        GameEvents.EnergyCollected(type, collected);
        feedbackSystem?.ShowEnergyGain(worldPosition ?? transform.position, type.ToString(), collected);
        NotifyChange(type, storage);
        return true;
    }

    public bool HasEnoughEnergy(EnergyType type, int amount)
    {
        return storageByType.TryGetValue(type, out EnergyStorage storage) && storage.currentAmount >= amount;
    }

    public bool ConsumeEnergy(EnergyType type, int amount)
    {
        if (!storageByType.TryGetValue(type, out EnergyStorage storage))
        {
            return false;
        }

        bool consumed = storage.TryConsume(amount);
        if (consumed)
        {
            NotifyChange(type, storage);
        }

        return consumed;
    }

    public int GetCurrentAmount(EnergyType type)
    {
        return storageByType.TryGetValue(type, out EnergyStorage storage) ? storage.currentAmount : 0;
    }

    public int GetMaxCapacity(EnergyType type)
    {
        return storageByType.TryGetValue(type, out EnergyStorage storage) ? storage.maxCapacity : 0;
    }

    public Dictionary<EnergyType, int> GetEnergySnapshot()
    {
        Dictionary<EnergyType, int> snapshot = new Dictionary<EnergyType, int>();
        foreach (EnergyStorage storage in energyStorages)
        {
            snapshot[storage.type] = storage.currentAmount;
        }

        return snapshot;
    }

    public void AutoBalanceCapacity(int playerLevel)
    {
        float multiplier = balanceConfig != null ? balanceConfig.GetCapacityMultiplier(playerLevel) : 1f;

        foreach (EnergyStorage storage in energyStorages)
        {
            storage.maxCapacity = Mathf.RoundToInt(GetDefaultCapacity() * multiplier);
            storage.currentAmount = Mathf.Clamp(storage.currentAmount, 0, storage.maxCapacity);
            NotifyChange(storage.type, storage);
        }
    }

    private int GetDefaultCapacity()
    {
        if (balanceConfig == null)
        {
            return 100;
        }

        return Mathf.Max(balanceConfig.baseCapacityPerType, balanceConfig.baseEnergyPerCollect * 10);
    }

    private void HandleEnergyDegradation(float deltaTime)
    {
        if (balanceConfig == null || !balanceConfig.enableEnergyDegradation)
        {
            return;
        }

        degradationTimer += deltaTime;
        if (degradationTimer < balanceConfig.degradationInterval)
        {
            return;
        }

        degradationTimer = 0f;

        foreach (EnergyStorage storage in energyStorages)
        {
            if (storage.currentAmount <= 0)
            {
                continue;
            }

            int loss = Mathf.Max(1, Mathf.RoundToInt(storage.currentAmount * balanceConfig.degradationPercent));
            storage.currentAmount = Mathf.Max(0, storage.currentAmount - loss);
            NotifyChange(storage.type, storage);
        }
    }

    private void NotifyAll()
    {
        foreach (EnergyStorage storage in energyStorages)
        {
            NotifyChange(storage.type, storage);
        }
    }

    private void NotifyChange(EnergyType type, EnergyStorage storage)
    {
        EnergyChanged?.Invoke(type, storage.currentAmount, storage.maxCapacity);
        feedbackSystem?.UpdateEnergyDisplay(GetEnergySnapshot());
    }
}
