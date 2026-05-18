using UnityEngine;
using UnityEngine.InputSystem;

public class RaycastInteractor : MonoBehaviour
{
    public Camera playerCamera;
    public float rayDistance = 10f;

    private InputSystem_Actions inputActions;
    private bool interactRequested;

    void Awake()
    {
        inputActions = new InputSystem_Actions();
        inputActions.Player.RaycastInteract.performed += ctx => interactRequested = true;
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
        if (!interactRequested)
            return;

        interactRequested = false;

        if (playerCamera == null)
            return;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        Debug.DrawRay(playerCamera.transform.position, playerCamera.transform.forward * rayDistance, Color.red, 1f); // Листинг 4
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, rayDistance))
        {
            Debug.Log("Луч попал в объект: " + hit.collider.name);
            Debug.Log("Точка попадания: " + hit.point);
            Debug.Log("Расстояние: " + hit.distance);
        }
        else
        {
            Debug.Log("Объект не найден");
        }
    }
}