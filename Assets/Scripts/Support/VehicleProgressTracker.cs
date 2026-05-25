using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class VehicleProgressTracker : MonoBehaviour
{
    [SerializeField] private float minDistanceStep = 0.25f;

    private Rigidbody vehicleRigidbody;
    private Vector3 lastPosition;
    private float accumulatedDistance;
    private float highestSpeed;

    private void Awake()
    {
        vehicleRigidbody = GetComponent<Rigidbody>();
        lastPosition = transform.position;
    }

    private void Update()
    {
        float distanceStep = Vector3.Distance(transform.position, lastPosition);
        if (distanceStep >= minDistanceStep)
        {
            accumulatedDistance += distanceStep;
            GameEvents.UpdateVehicleDistance(distanceStep);
            lastPosition = transform.position;
        }

        float speed = vehicleRigidbody.linearVelocity.magnitude;
        if (speed > highestSpeed)
        {
            highestSpeed = speed;
            GameEvents.UpdateTopSpeed(highestSpeed);
        }
    }
}
