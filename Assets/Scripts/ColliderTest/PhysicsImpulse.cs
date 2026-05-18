using UnityEngine;
using UnityEngine.InputSystem;

public class PhysicsImpulse : MonoBehaviour
{
    public float impulseForce = 5f;

    private Rigidbody rb;
    private InputSystem_Actions inputActions;
    private bool impulseRequested;

    void Awake()
    {
        inputActions = new InputSystem_Actions();

        inputActions.Player.Impulse.performed += ctx => impulseRequested = true;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void OnEnable()
    {
        inputActions.Enable();
    }

    void OnDisable()
    {
        inputActions.Disable();
    }

    void FixedUpdate()
    {
        if (impulseRequested)
        {
            rb.AddForce(Vector3.up * impulseForce, ForceMode.Impulse);
            Debug.Log("К объекту применён импульс через Input System.");
            impulseRequested = false;
        }
    }
}