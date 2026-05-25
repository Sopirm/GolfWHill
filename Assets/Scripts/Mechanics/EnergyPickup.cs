using UnityEngine;

public class EnergyPickup : MonoBehaviour
{
    [SerializeField] private EnergySystem.EnergyType energyType = EnergySystem.EnergyType.Red;
    [SerializeField] private int amount = 10;
    [SerializeField] private bool destroyOnCollect = true;

    public void Configure(EnergySystem.EnergyType newType, int newAmount)
    {
        energyType = newType;
        amount = newAmount;
    }

    private void OnTriggerEnter(Collider other)
    {
        EnergySystem energySystem = other.GetComponentInParent<EnergySystem>();
        if (energySystem == null)
        {
            energySystem = FindObjectOfType<EnergySystem>();
        }

        if (energySystem == null)
        {
            return;
        }

        if (energySystem.CollectEnergy(energyType, amount, transform.position) && destroyOnCollect)
        {
            Destroy(gameObject);
        }
    }
}
