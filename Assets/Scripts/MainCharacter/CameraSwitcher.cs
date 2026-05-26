using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class CameraSwitcher : MonoBehaviour
{
    public List<Camera> cameras; // Список всех камер для переключения
    private int currentCameraIndex = 0;

    private InputSystem_Actions inputActions;
    private bool switchCameraRequested;

    void Awake()
    {
        inputActions = new InputSystem_Actions();
        inputActions.Player.SwitchCamera.performed += ctx => switchCameraRequested = true;
    }

    void OnEnable()
    {
        inputActions.Enable();
    }

    void OnDisable()
    {
        inputActions.Disable();
    }

    void Start()
    {
        // Убедимся, что все камеры, кроме первой, выключены
        for (int i = 0; i < cameras.Count; i++)
        {
            cameras[i].gameObject.SetActive(i == currentCameraIndex);
        }
    }

    void Update()
    {
        if (PlatformInputManager.Instance != null && PlatformInputManager.Instance.ConsumeSwitchCameraPressed())
        {
            switchCameraRequested = true;
        }

        if (switchCameraRequested)
        {
            switchCameraRequested = false;
            SwitchNextCamera();
        }
    }

    void SwitchNextCamera()
    {
        // Деактивируем текущую камеру
        cameras[currentCameraIndex].gameObject.SetActive(false);

        // Переходим к следующей камере
        currentCameraIndex = (currentCameraIndex + 1) % cameras.Count;

        // Активируем новую камеру
        cameras[currentCameraIndex].gameObject.SetActive(true);

        Debug.Log("Переключена на камеру: " + cameras[currentCameraIndex].name);
    }
}
