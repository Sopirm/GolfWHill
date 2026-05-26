using UnityEngine;
using UnityEngine.InputSystem;

public class PickupAnimatorController : MonoBehaviour
{
    private Animator animator;
    private Rigidbody rb; // Для получения скорости автомобиля
    private InputSystem_Actions inputActions;

    private bool toggleDoorRequested;

    void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>(); // Получаем Rigidbody, чтобы использовать его скорость

        if (animator == null)
        {
            Debug.LogError("Animator component not found on PickupAnimatorController object.");
            enabled = false;
            return;
        }
        if (rb == null)
        {
            Debug.LogWarning("Rigidbody component not found on PickupAnimatorController object. Suspension animation will not work.");
        }


        inputActions = new InputSystem_Actions();

        // Подписываемся на событие нажатия ToggleDoor
        inputActions.Player.ToggleDoor.performed += ctx => toggleDoorRequested = true;
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
        if (PlatformInputManager.Instance != null && PlatformInputManager.Instance.ConsumeToggleDoorPressed())
        {
            toggleDoorRequested = true;
        }

        // Управление анимацией двери
        if (toggleDoorRequested)
        {
            animator.SetTrigger("OpenDoor"); // Активируем триггер
            toggleDoorRequested = false;
        }

        // Управление анимацией подвески
        if (rb != null)
        {
            // Проверяем скорость автомобиля
            bool isMovingFast = rb.linearVelocity.magnitude > 1f; // "Скорость", при которой подвеска будет "сжиматься"
            animator.SetBool("IsSuspensionCompressed", isMovingFast);
        }
    }
}
