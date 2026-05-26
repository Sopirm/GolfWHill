using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(Rigidbody))]
public class NetworkPlayer : NetworkBehaviour
{
    [Header("Car Setup")]
    [SerializeField] private float motorForce = 1000f;
    [SerializeField] private float steeringAngle = 30f;
    [SerializeField] private float brakeForce = 3000f;
    [SerializeField] private float spawnSpacing = 8f;
    [SerializeField] private List<WheelCollider> frontWheelColliders = new();
    [SerializeField] private List<WheelCollider> rearWheelColliders = new();
    [SerializeField] private List<Transform> frontWheelMeshes = new();
    [SerializeField] private List<Transform> rearWheelMeshes = new();
    [SerializeField] private Transform centerOfMass;
    [SerializeField] private Renderer playerRenderer;

    private readonly NetworkVariable<int> colorIndex =
        new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<Vector2> driveInput =
        new(Vector2.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<Vector3> syncedPosition =
        new(Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<Quaternion> syncedRotation =
        new(Quaternion.identity, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<Vector3> syncedVelocity =
        new(Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<Vector3> syncedAngularVelocity =
        new(Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private InputSystem_Actions inputActions;
    private Camera[] playerCameras;
    private AudioListener[] audioListeners;
    private Rigidbody rb;
    private Vector2 localMoveInput;
    private int currentCameraIndex;
    private bool switchCameraRequested;
    private Vector2 lastPlatformMoveInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        inputActions = new InputSystem_Actions();
        playerCameras = GetComponentsInChildren<Camera>(true);
        audioListeners = GetComponentsInChildren<AudioListener>(true);

        if (playerRenderer == null)
        {
            playerRenderer = GetComponentInChildren<Renderer>();
        }

        AutoAssignCarReferences();

        if (centerOfMass != null)
        {
            rb.centerOfMass = centerOfMass.localPosition;
        }

        inputActions.Player.Move.performed += OnMovePerformed;
        inputActions.Player.Move.canceled += OnMoveCanceled;
        inputActions.Player.SwitchCamera.performed += OnSwitchCameraPerformed;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            transform.position = GetSpawnPosition();
            colorIndex.Value = Random.Range(0, 4);
            PublishServerState();
        }
        else
        {
            ApplySyncedState();
        }

        ApplyColor(colorIndex.Value);
        colorIndex.OnValueChanged += OnColorChanged;

        rb.isKinematic = !IsServer;
        rb.interpolation = IsServer ? RigidbodyInterpolation.Interpolate : RigidbodyInterpolation.None;

        ConfigureLocalPlayerPresentation();
    }

    public override void OnNetworkDespawn()
    {
        colorIndex.OnValueChanged -= OnColorChanged;
    }

    private void OnEnable()
    {
        inputActions?.Enable();
    }

    private void OnDisable()
    {
        inputActions?.Disable();
    }

    private void Update()
    {
        if (IsOwner)
        {
            UpdatePlatformMovementInput();
            HandleCameraSwitchInput();
            ConfigureLocalPlayerPresentation();
        }
    }

    private void FixedUpdate()
    {
        if (IsServer)
        {
            ApplyMotorForce();
            ApplySteering();
            PublishServerState();
        }
        else
        {
            ApplySyncedState();
        }

        UpdateWheelMeshes();
    }

    private void OnMovePerformed(InputAction.CallbackContext context)
    {
        localMoveInput = context.ReadValue<Vector2>();

        if (IsOwner && IsServer)
        {
            driveInput.Value = localMoveInput;
        }
        else if (IsOwner)
        {
            SubmitInputServerRpc(localMoveInput);
        }
    }

    private void OnMoveCanceled(InputAction.CallbackContext context)
    {
        localMoveInput = Vector2.zero;

        if (IsOwner && IsServer)
        {
            driveInput.Value = localMoveInput;
        }
        else if (IsOwner)
        {
            SubmitInputServerRpc(localMoveInput);
        }
    }

    private void OnSwitchCameraPerformed(InputAction.CallbackContext context)
    {
        if (!IsOwner)
        {
            return;
        }

        switchCameraRequested = true;
    }

    [Rpc(SendTo.Server)]
    private void SubmitInputServerRpc(Vector2 input)
    {
        driveInput.Value = input;
    }

    private void ApplyMotorForce()
    {
        float verticalInput = driveInput.Value.y;
        float currentMotorForce = verticalInput * motorForce;

        foreach (var wheelCollider in rearWheelColliders)
        {
            if (wheelCollider != null)
            {
                wheelCollider.motorTorque = currentMotorForce;
            }
        }

        bool shouldBrake = Mathf.Approximately(verticalInput, 0f) && rb.linearVelocity.magnitude > 0.1f;
        float brakeTorque = shouldBrake ? brakeForce : 0f;

        foreach (var wheelCollider in frontWheelColliders)
        {
            if (wheelCollider != null)
            {
                wheelCollider.brakeTorque = brakeTorque;
            }
        }

        foreach (var wheelCollider in rearWheelColliders)
        {
            if (wheelCollider != null)
            {
                wheelCollider.brakeTorque = brakeTorque;
            }
        }
    }

    private void ApplySteering()
    {
        float horizontalInput = driveInput.Value.x;
        float currentSteeringAngle = horizontalInput * steeringAngle;

        foreach (var wheelCollider in frontWheelColliders)
        {
            if (wheelCollider != null)
            {
                wheelCollider.steerAngle = currentSteeringAngle;
            }
        }
    }

    private void UpdateWheelMeshes()
    {
        UpdateWheelMeshSet(frontWheelColliders, frontWheelMeshes);
        UpdateWheelMeshSet(rearWheelColliders, rearWheelMeshes);
    }

    private static void UpdateWheelMeshSet(List<WheelCollider> colliders, List<Transform> meshes)
    {
        int count = Mathf.Min(colliders.Count, meshes.Count);

        for (int i = 0; i < count; i++)
        {
            if (colliders[i] == null || meshes[i] == null)
            {
                continue;
            }

            colliders[i].GetWorldPose(out var position, out var rotation);
            meshes[i].position = position;
            meshes[i].rotation = rotation;
        }
    }

    private void ConfigureLocalPlayerPresentation()
    {
        for (int i = 0; i < playerCameras.Length; i++)
        {
            bool shouldEnable = IsOwner && i == currentCameraIndex;
            playerCameras[i].gameObject.SetActive(shouldEnable);
        }

        for (int i = 0; i < audioListeners.Length; i++)
        {
            audioListeners[i].enabled = IsOwner && i == currentCameraIndex;
        }
    }

    private void HandleCameraSwitchInput()
    {
        if (PlatformInputManager.Instance != null && PlatformInputManager.Instance.ConsumeSwitchCameraPressed())
        {
            switchCameraRequested = true;
        }

        if (!switchCameraRequested || playerCameras == null || playerCameras.Length <= 1)
        {
            return;
        }

        switchCameraRequested = false;
        currentCameraIndex = (currentCameraIndex + 1) % playerCameras.Length;
        ConfigureLocalPlayerPresentation();
        Debug.Log($"Локальный игрок переключил камеру на: {playerCameras[currentCameraIndex].name}");
    }

    private void UpdatePlatformMovementInput()
    {
        if (PlatformInputManager.Instance == null || !PlatformInputManager.Instance.IsMobileInputActive)
        {
            return;
        }

        Vector2 platformMoveInput = PlatformInputManager.Instance.GetMoveInput();
        if (platformMoveInput == lastPlatformMoveInput)
        {
            return;
        }

        lastPlatformMoveInput = platformMoveInput;
        localMoveInput = platformMoveInput;

        if (IsOwner && IsServer)
        {
            driveInput.Value = localMoveInput;
        }
        else if (IsOwner)
        {
            SubmitInputServerRpc(localMoveInput);
        }
    }

    private void PublishServerState()
    {
        syncedPosition.Value = rb.position;
        syncedRotation.Value = rb.rotation;
        syncedVelocity.Value = rb.linearVelocity;
        syncedAngularVelocity.Value = rb.angularVelocity;
    }

    private void ApplySyncedState()
    {
        rb.MovePosition(syncedPosition.Value);
        rb.MoveRotation(syncedRotation.Value);
    }

    private void AutoAssignCarReferences()
    {
        if (frontWheelColliders.Count == 0 || rearWheelColliders.Count == 0)
        {
            var wheelColliders = GetComponentsInChildren<WheelCollider>(true);
            System.Array.Sort(wheelColliders, (left, right) => left.transform.localPosition.z.CompareTo(right.transform.localPosition.z));

            frontWheelColliders.Clear();
            rearWheelColliders.Clear();

            foreach (var wheelCollider in wheelColliders)
            {
                if (wheelCollider.transform.localPosition.z >= 0f)
                {
                    frontWheelColliders.Add(wheelCollider);
                }
                else
                {
                    rearWheelColliders.Add(wheelCollider);
                }
            }
        }

        if (frontWheelMeshes.Count == 0 || rearWheelMeshes.Count == 0)
        {
            var wheelMeshes = new List<Transform>();
            foreach (var renderer in GetComponentsInChildren<MeshRenderer>(true))
            {
                var lowerName = renderer.transform.name.ToLowerInvariant();
                if (lowerName.Contains("wheel"))
                {
                    wheelMeshes.Add(renderer.transform);
                }
            }

            if (wheelMeshes.Count >= 4)
            {
                wheelMeshes.Sort((left, right) => right.localPosition.z.CompareTo(left.localPosition.z));

                frontWheelMeshes.Clear();
                rearWheelMeshes.Clear();

                foreach (var wheelMesh in wheelMeshes)
                {
                    if (wheelMesh.localPosition.z >= 0f)
                    {
                        frontWheelMeshes.Add(wheelMesh);
                    }
                    else
                    {
                        rearWheelMeshes.Add(wheelMesh);
                    }
                }
            }
        }

        if (centerOfMass == null)
        {
            foreach (var child in GetComponentsInChildren<Transform>(true))
            {
                if (child.name.ToLowerInvariant().Contains("mass"))
                {
                    centerOfMass = child;
                    break;
                }
            }
        }
    }

    private Vector3 GetSpawnPosition()
    {
        return transform.position + new Vector3((float)OwnerClientId * spawnSpacing, 0f, 0f);
    }

    private void OnColorChanged(int previousValue, int newValue)
    {
        ApplyColor(newValue);
    }

    private void ApplyColor(int index)
    {
        if (playerRenderer == null)
            return;

        switch (index)
        {
            case 0:
                playerRenderer.material.color = Color.red;
                break;
            case 1:
                playerRenderer.material.color = Color.blue;
                break;
            case 2:
                playerRenderer.material.color = Color.green;
                break;
            case 3:
                playerRenderer.material.color = Color.yellow;
                break;
            default:
                playerRenderer.material.color = Color.white;
                break;
        }
    }
}
