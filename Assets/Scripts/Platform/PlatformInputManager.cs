using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem.UI;

public class PlatformInputManager : MonoBehaviour
{
    [System.Serializable]
    public class InputSettings
    {
        [Header("PC/WebGL")]
        public float keyboardSensitivity = 1f;

        [Header("Mobile")]
        public float joystickDeadZone = 0.2f;
        public float mobileButtonAlpha = 0.82f;
        public bool createMobileUIRuntime = true;
        public bool enableGyro = false;
        public bool simulateMobileInEditor;

        [Header("WebGL")]
        public bool confineWebGLCursor = true;
    }

    public static PlatformInputManager Instance { get; private set; }

    [SerializeField] private InputSettings settings = new InputSettings();

    private PlatformVirtualJoystick movementJoystick;
    private PlatformTouchButton switchCameraButton;
    private PlatformTouchButton interactButton;
    private PlatformTouchButton toggleDoorButton;
    private PlatformTouchButton brakeButton;
    private Canvas mobileCanvas;
    private bool switchCameraRequested;
    private bool interactRequested;
    private bool toggleDoorRequested;

    public bool IsMobileInputActive
    {
        get
        {
#if UNITY_EDITOR
            if (settings.simulateMobileInEditor)
            {
                return true;
            }
#endif
            return Application.isMobilePlatform;
        }
    }

    public Vector2 GetMoveInput()
    {
        if (!IsMobileInputActive || movementJoystick == null)
        {
            return Vector2.zero;
        }

        Vector2 input = movementJoystick.InputVector;
        if (input.magnitude < settings.joystickDeadZone)
        {
            return Vector2.zero;
        }

        if (brakeButton != null && brakeButton.IsHeld)
        {
            input.y = Mathf.Min(input.y, 0f);
        }

        return input;
    }

    public bool ConsumeSwitchCameraPressed()
    {
        bool value = switchCameraRequested;
        switchCameraRequested = false;
        return value;
    }

    public bool ConsumeInteractPressed()
    {
        bool value = interactRequested;
        interactRequested = false;
        return value;
    }

    public bool ConsumeToggleDoorPressed()
    {
        bool value = toggleDoorRequested;
        toggleDoorRequested = false;
        return value;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitializeForPlatform();
    }

    private void Update()
    {
        if (!IsMobileInputActive)
        {
            return;
        }

        if (switchCameraButton != null && switchCameraButton.ConsumePressedThisFrame())
        {
            switchCameraRequested = true;
        }

        if (interactButton != null && interactButton.ConsumePressedThisFrame())
        {
            interactRequested = true;
        }

        if (toggleDoorButton != null && toggleDoorButton.ConsumePressedThisFrame())
        {
            toggleDoorRequested = true;
        }
    }

    private void InitializeForPlatform()
    {
#if UNITY_STANDALONE || UNITY_EDITOR
        SetupPCControls();
#elif UNITY_ANDROID || UNITY_IOS
        SetupMobileControls();
#elif UNITY_WEBGL
        SetupWebGLControls();
#endif
    }

    private void SetupPCControls()
    {
        if (mobileCanvas != null)
        {
            mobileCanvas.gameObject.SetActive(false);
        }
    }

    private void SetupMobileControls()
    {
        if (settings.createMobileUIRuntime)
        {
            CreateMobileUI();
        }

        if (SystemInfo.supportsGyroscope && settings.enableGyro)
        {
            Input.gyro.enabled = true;
        }
    }

    private void SetupWebGLControls()
    {
        if (settings.confineWebGLCursor)
        {
            Cursor.lockState = CursorLockMode.Confined;
        }

        if (mobileCanvas != null)
        {
            mobileCanvas.gameObject.SetActive(false);
        }
    }

    private void CreateMobileUI()
    {
        if (mobileCanvas != null)
        {
            mobileCanvas.gameObject.SetActive(true);
            return;
        }

        EnsureEventSystemExists();

        GameObject canvasObject = new GameObject("MobileControlsCanvas");
        mobileCanvas = canvasObject.AddComponent<Canvas>();
        mobileCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        mobileCanvas.sortingOrder = 500;
        canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObject.AddComponent<GraphicRaycaster>();
        DontDestroyOnLoad(canvasObject);

        movementJoystick = CreateJoystick(canvasObject.transform);
        switchCameraButton = CreateButton(canvasObject.transform, "CamButton", "Cam", new Vector2(-110f, 250f), new Vector2(110f, 64f));
        interactButton = CreateButton(canvasObject.transform, "InteractButton", "Use", new Vector2(-110f, 170f), new Vector2(110f, 64f));
        toggleDoorButton = CreateButton(canvasObject.transform, "DoorButton", "Door", new Vector2(-110f, 90f), new Vector2(110f, 64f));
        brakeButton = CreateButton(canvasObject.transform, "BrakeButton", "Brake", new Vector2(110f, 90f), new Vector2(120f, 64f));
    }

    private PlatformVirtualJoystick CreateJoystick(Transform parent)
    {
        GameObject root = new GameObject("MoveJoystick", typeof(RectTransform), typeof(Image), typeof(PlatformVirtualJoystick));
        root.transform.SetParent(parent, false);

        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0f, 0f);
        rect.anchoredPosition = new Vector2(40f, 40f);
        rect.sizeDelta = new Vector2(180f, 180f);

        Image bg = root.GetComponent<Image>();
        bg.color = new Color(0.08f, 0.18f, 0.18f, settings.mobileButtonAlpha * 0.55f);

        GameObject handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handle.transform.SetParent(root.transform, false);

        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.anchorMin = new Vector2(0.5f, 0.5f);
        handleRect.anchorMax = new Vector2(0.5f, 0.5f);
        handleRect.pivot = new Vector2(0.5f, 0.5f);
        handleRect.sizeDelta = new Vector2(70f, 70f);

        Image handleImage = handle.GetComponent<Image>();
        handleImage.color = new Color(0.92f, 0.92f, 0.9f, settings.mobileButtonAlpha);

        PlatformVirtualJoystick joystick = root.GetComponent<PlatformVirtualJoystick>();
        joystick.SetHandle(handleRect);

        return joystick;
    }

    private PlatformTouchButton CreateButton(Transform parent, string objectName, string text, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject root = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button), typeof(PlatformTouchButton));
        root.transform.SetParent(parent, false);

        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Image image = root.GetComponent<Image>();
        image.color = new Color(0.08f, 0.18f, 0.18f, settings.mobileButtonAlpha);

        GameObject label = new GameObject("Label", typeof(RectTransform), typeof(Text));
        label.transform.SetParent(root.transform, false);
        RectTransform labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        Text labelText = label.GetComponent<Text>();
        labelText.text = text;
        labelText.alignment = TextAnchor.MiddleCenter;
        labelText.color = Color.white;
        labelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        labelText.resizeTextForBestFit = true;

        return root.GetComponent<PlatformTouchButton>();
    }

    private void EnsureEventSystemExists()
    {
        if (FindObjectOfType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        DontDestroyOnLoad(eventSystemObject);
    }
}
