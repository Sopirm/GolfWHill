using UnityEngine;

public class TriggerZoneHandler : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        Debug.Log("В зону вошёл объект: " + other.name);
    }

    void OnTriggerStay(Collider other)
    {
        Debug.Log("Объект находится в зоне: " + other.name);
    }

    void OnTriggerExit(Collider other)
    {
        Debug.Log("Объект покинул зону: " + other.name);
    }
}