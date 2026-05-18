using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic; // Добавлено для List

public class CarController : MonoBehaviour
{
    public float motorForce = 1000f; // Сила двигателя
    public float steeringAngle = 30f; // Максимальный угол поворота колес
    public float brakeForce = 3000f; // Сила торможения

    public List<WheelCollider> frontWheelColliders; // Передние WheelCollider
    public List<WheelCollider> rearWheelColliders;  // Задние WheelCollider
    public List<Transform> frontWheelMeshes;    // Меши передних колес
    public List<Transform> rearWheelMeshes;     // Меши задних колес

    private Rigidbody rb;
    private InputSystem_Actions inputActions;
    private Vector2 moveInput;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("Rigidbody not found on CarController object.");
            enabled = false;
            return;
        }

        inputActions = new InputSystem_Actions();

        inputActions.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Move.canceled += ctx => moveInput = Vector2.zero;
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
        ApplyMotorForce();
        ApplySteering();
        UpdateWheelMeshes();
    }

    void ApplyMotorForce()
    {
        float verticalInput = moveInput.y;
        float currentMotorForce = verticalInput * motorForce;

        foreach (var wheelCollider in rearWheelColliders)
        {
            wheelCollider.motorTorque = currentMotorForce;
        }

        // Торможение, если нет газа и скорость достаточно высокая
        if (verticalInput == 0 && rb.linearVelocity.magnitude > 0.1f)
        {
            foreach (var wheelCollider in frontWheelColliders)
            {
                wheelCollider.brakeTorque = brakeForce;
            }
            foreach (var wheelCollider in rearWheelColliders)
            {
                wheelCollider.brakeTorque = brakeForce;
            }
        }
        else
        {
            foreach (var wheelCollider in frontWheelColliders)
            {
                wheelCollider.brakeTorque = 0;
            }
            foreach (var wheelCollider in rearWheelColliders)
            {
                wheelCollider.brakeTorque = 0;
            }
        }
    }

    void ApplySteering()
    {
        float horizontalInput = moveInput.x;
        float currentSteeringAngle = horizontalInput * steeringAngle;

        // Поворот только при движении (как ранее просили)
        if (Mathf.Abs(moveInput.y) > 0.1f || rb.linearVelocity.magnitude > 0.5f)
        {
            foreach (var wheelCollider in frontWheelColliders)
            {
                wheelCollider.steerAngle = currentSteeringAngle;
            }
        }
        else
        {
            foreach (var wheelCollider in frontWheelColliders)
            {
                wheelCollider.steerAngle = 0; // Сброс угла поворота, если машина стоит
            }
        }
    }

    void UpdateWheelMeshes()
    {
        // Передние колеса
        for (int i = 0; i < frontWheelColliders.Count; i++)
        {
            UpdateWheelMesh(frontWheelColliders[i], frontWheelMeshes[i]);
        }
        // Задние колеса
        for (int i = 0; i < rearWheelColliders.Count; i++)
        {
            UpdateWheelMesh(rearWheelColliders[i], rearWheelMeshes[i]);
        }
    }

    void UpdateWheelMesh(WheelCollider wheelCollider, Transform wheelMesh)
    {
        Vector3 pos;
        Quaternion rot;
        wheelCollider.GetWorldPose(out pos, out rot);

        wheelMesh.position = pos;
        wheelMesh.rotation = rot;
    }
}