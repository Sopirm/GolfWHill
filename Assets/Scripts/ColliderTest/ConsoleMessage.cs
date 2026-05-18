using UnityEngine;
using UnityEngine.InputSystem;

public class ConsoleMessage : MonoBehaviour
{
    private InputSystem_Actions inputActions;
    private bool logRequested;

    void Awake()
    {
        inputActions = new InputSystem_Actions();

        inputActions.Player.LogMessage.performed += ctx => logRequested = true;
    }

    void Start()
    {
        Debug.Log("Скрипт ConsoleMessage успешно запущен.");
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
        if (logRequested)
        {
            Debug.Log("Команда LogMessage получена через Input System.");
            logRequested = false;
        }
    }
}