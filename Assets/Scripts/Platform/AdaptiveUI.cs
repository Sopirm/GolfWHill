using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class AdaptiveUI : MonoBehaviour
{
    [System.Serializable]
    public class PlatformUISettings
    {
        public int pcFontSize = 16;
        public int mobileFontSize = 22;
        public int webGLFontSize = 18;
        public Vector2 pcButtonSize = new Vector2(160f, 40f);
        public Vector2 mobileButtonSize = new Vector2(220f, 70f);
        public Vector2 webGLButtonSize = new Vector2(180f, 50f);
        public float pcSpacing = 10f;
        public float mobileSpacing = 16f;
        public float webGLSpacing = 12f;
        public bool showMobileControls = true;
        public bool optimizeForTouch = true;
        public float minTouchableSize = 44f;
    }

    [SerializeField] private PlatformUISettings settings = new PlatformUISettings();
    [SerializeField] private List<TextMeshProUGUI> textElements = new List<TextMeshProUGUI>();
    [SerializeField] private List<Button> buttons = new List<Button>();
    [SerializeField] private List<Selectable> selectables = new List<Selectable>();
    [SerializeField] private GameObject pcOnlyElements;
    [SerializeField] private GameObject mobileOnlyElements;
    [SerializeField] private GameObject webGLOnlyElements;

    private CanvasScaler canvasScaler;

    private void Start()
    {
        ApplyPlatformSettings();
    }

    private void OnEnable()
    {
        ApplyPlatformSettings();
    }

    private void OnValidate()
    {
        ApplyPlatformSettings();
    }

    public void ApplyPlatformSettings()
    {
        canvasScaler = GetComponentInParent<CanvasScaler>();

#if UNITY_ANDROID || UNITY_IOS
        ApplyMobileSettings();
#elif UNITY_WEBGL
        ApplyWebGLSettings();
#else
        ApplyPCSettings();
#endif

        if (transform is RectTransform rectTransform)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        }
    }

    private void ApplyPCSettings()
    {
        ApplyFontSize(settings.pcFontSize, false);
        ApplyButtonSizes(settings.pcButtonSize, false);
        ToggleGroups(true, false, false);
        ApplyCanvasScaler(new Vector2(1920f, 1080f), 0.5f);
    }

    private void ApplyMobileSettings()
    {
        ApplyFontSize(settings.mobileFontSize, true);
        ApplyButtonSizes(settings.mobileButtonSize, true);
        ToggleGroups(false, settings.showMobileControls, false);
        ApplyCanvasScaler(new Vector2(1080f, 1920f), 1f);
    }

    private void ApplyWebGLSettings()
    {
        ApplyFontSize(settings.webGLFontSize, false);
        ApplyButtonSizes(settings.webGLButtonSize, false);
        ToggleGroups(false, false, true);
        ApplyCanvasScaler(new Vector2(1920f, 1080f), 0.5f);
    }

    private void ApplyFontSize(int fontSize, bool autoSize)
    {
        foreach (TextMeshProUGUI text in textElements)
        {
            if (text == null)
            {
                continue;
            }

            text.fontSize = fontSize;
            text.enableAutoSizing = autoSize;
            text.fontSizeMin = Mathf.Max(12, fontSize - 6);
            text.fontSizeMax = fontSize;
        }
    }

    private void ApplyButtonSizes(Vector2 buttonSize, bool touchOptimized)
    {
        foreach (Button button in buttons)
        {
            if (button == null)
            {
                continue;
            }

            RectTransform rect = button.GetComponent<RectTransform>();
            if (rect != null)
            {
                float width = touchOptimized ? Mathf.Max(buttonSize.x, settings.minTouchableSize) : buttonSize.x;
                float height = touchOptimized ? Mathf.Max(buttonSize.y, settings.minTouchableSize) : buttonSize.y;
                rect.sizeDelta = new Vector2(width, height);
            }

            Navigation navigation = button.navigation;
            navigation.mode = touchOptimized ? Navigation.Mode.None : Navigation.Mode.Automatic;
            button.navigation = navigation;
        }

        if (!touchOptimized || !settings.optimizeForTouch)
        {
            return;
        }

        foreach (Selectable selectable in selectables)
        {
            if (selectable == null || selectable.transform is not RectTransform rect)
            {
                continue;
            }

            float width = Mathf.Max(rect.sizeDelta.x, settings.minTouchableSize);
            float height = Mathf.Max(rect.sizeDelta.y, settings.minTouchableSize);
            rect.sizeDelta = new Vector2(width, height);
        }
    }

    private void ToggleGroups(bool showPC, bool showMobile, bool showWebGL)
    {
        if (pcOnlyElements != null) pcOnlyElements.SetActive(showPC);
        if (mobileOnlyElements != null) mobileOnlyElements.SetActive(showMobile);
        if (webGLOnlyElements != null) webGLOnlyElements.SetActive(showWebGL);
    }

    private void ApplyCanvasScaler(Vector2 referenceResolution, float match)
    {
        if (canvasScaler == null)
        {
            return;
        }

        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = referenceResolution;
        canvasScaler.matchWidthOrHeight = match;
    }
}
