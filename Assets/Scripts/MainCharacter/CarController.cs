using UnityEngine;
using UnityEngine.InputSystem;

public class CarController : MonoBehaviour
{
    public float moveSpeed = 10f;
    public float turnSpeed = 100f;

    private InputSystem_Actions inputActions;
    private Vector2 moveInput;

    void Awake()
    {
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
        // Перемещение вперед/назад
        transform.Translate(Vector3.forward * moveInput.y * moveSpeed * Time.deltaTime);

        // Повороты влево/вправо только при движении вперед/назад
        if (Mathf.Abs(moveInput.y) > 0.1f) // Проверяем, движется ли автомобиль
        {
            transform.Rotate(Vector3.up * moveInput.x * turnSpeed * Time.deltaTime);
        }
    }
}