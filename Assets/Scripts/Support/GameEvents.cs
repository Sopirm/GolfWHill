using System;

public static class GameEvents
{
    public static event Action<EnergySystem.EnergyType, int> OnEnergyCollected;
    public static event Action<string> OnAbilityUsed;
    public static event Action<int> OnMaxComboUpdated;
    public static event Action<float> OnPlayTimeUpdated;
    public static event Action<float> OnVehicleDistanceUpdated;
    public static event Action<float> OnTopSpeedUpdated;
    public static event Action<int> OnDailyLoginStreakUpdated;

    public static void EnergyCollected(EnergySystem.EnergyType type, int amount)
    {
        OnEnergyCollected?.Invoke(type, amount);
    }

    public static void AbilityUsed(string abilityName)
    {
        OnAbilityUsed?.Invoke(abilityName);
    }

    public static void UpdateMaxCombo(int combo)
    {
        OnMaxComboUpdated?.Invoke(combo);
    }

    public static void UpdatePlayTime(float deltaSeconds)
    {
        OnPlayTimeUpdated?.Invoke(deltaSeconds);
    }

    public static void UpdateVehicleDistance(float deltaDistance)
    {
        OnVehicleDistanceUpdated?.Invoke(deltaDistance);
    }

    public static void UpdateTopSpeed(float speed)
    {
        OnTopSpeedUpdated?.Invoke(speed);
    }

    public static void UpdateDailyLoginStreak(int streakDays)
    {
        OnDailyLoginStreakUpdated?.Invoke(streakDays);
    }
}
