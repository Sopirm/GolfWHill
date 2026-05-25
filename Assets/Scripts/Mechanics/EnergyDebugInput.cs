using UnityEngine;
using UnityEngine.InputSystem;

public class EnergyDebugInput : MonoBehaviour
{
    [SerializeField] private EnergySystem energySystem;
    [SerializeField] private int collectAmount = 10;

    private void Update()
    {
        if (energySystem == null || Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            energySystem.CollectEnergy(EnergySystem.EnergyType.Red, collectAmount);
        }

        if (Keyboard.current.wKey.wasPressedThisFrame)
        {
            energySystem.CollectEnergy(EnergySystem.EnergyType.Blue, collectAmount);
        }

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            energySystem.CollectEnergy(EnergySystem.EnergyType.Green, collectAmount);
        }
    }
}
