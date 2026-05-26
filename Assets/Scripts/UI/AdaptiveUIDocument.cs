using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class AdaptiveUIDocument : MonoBehaviour
{
    [SerializeField] private bool simulateMobileInEditor;
    [SerializeField] private bool simulateWebGLInEditor;

    private UIDocument uiDocument;
    private VisualElement root;

    private void Awake()
    {
        uiDocument = GetComponent<UIDocument>();
    }

    private void OnEnable()
    {
        ApplyPlatformClasses();
    }

    [ContextMenu("Apply Platform Classes")]
    public void ApplyPlatformClasses()
    {
        if (uiDocument == null)
        {
            uiDocument = GetComponent<UIDocument>();
        }

        if (uiDocument == null)
        {
            return;
        }

        root = uiDocument.rootVisualElement;
        if (root == null)
        {
            return;
        }

        root.RemoveFromClassList("platform-pc");
        root.RemoveFromClassList("platform-mobile");
        root.RemoveFromClassList("platform-webgl");

        if (IsMobile())
        {
            root.AddToClassList("platform-mobile");
        }
        else if (IsWebGL())
        {
            root.AddToClassList("platform-webgl");
        }
        else
        {
            root.AddToClassList("platform-pc");
        }
    }

    public string GetPlatformDisplayName()
    {
        if (IsMobile())
        {
            return "Android / Mobile";
        }

        if (IsWebGL())
        {
            return "WebGL";
        }

        return "PC";
    }

    private bool IsMobile()
    {
#if UNITY_EDITOR
        if (simulateMobileInEditor)
        {
            return true;
        }
#endif
#if UNITY_ANDROID || UNITY_IOS
        return true;
#else
        return Application.isMobilePlatform;
#endif
    }

    private bool IsWebGL()
    {
#if UNITY_EDITOR
        if (simulateWebGLInEditor)
        {
            return true;
        }
#endif
#if UNITY_WEBGL
        return true;
#else
        return false;
#endif
    }
}
