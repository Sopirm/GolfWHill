using UnityEngine;
using UnityEngine.InputSystem;

public class ObjectSpawner : MonoBehaviour
{
    public GameObject prefabToSpawn;
    public Transform spawnPoint;
    public float spawnForce = 5f;

    private InputSystem_Actions inputActions;
    private bool spawnRequested;

    void Awake()
    {
        inputActions = new InputSystem_Actions();
        inputActions.Player.SpawnObject.performed += ctx => spawnRequested = true;
    }

    void OnEnable()
    {
        inputActions.Enable();
    }

    void OnDisable()
    {
        inputActions.Disable();
    }

    void Update()
    {
        if (spawnRequested)
        {
            spawnRequested = false;

            if (prefabToSpawn == null || spawnPoint == null)
                return;

            GameObject spawnedObject = Instantiate(
                prefabToSpawn,
                spawnPoint.position,
                spawnPoint.rotation
            );

            Rigidbody rb = spawnedObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(spawnPoint.forward * spawnForce, ForceMode.Impulse);
            }
        }
    }
}