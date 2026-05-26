using UnityEngine;

public class PlatformRuntimeSettings : MonoBehaviour
{
    [SerializeField] private int targetFrameRatePC = 120;
    [SerializeField] private int targetFrameRateMobile = 60;
    [SerializeField] private int targetFrameRateWebGL = 60;
    [SerializeField] private int mobileQualityLevel = 1;
    [SerializeField] private int standaloneQualityLevel = 2;
    [SerializeField] private int webGLQualityLevel = 1;

    private void Awake()
    {
        ApplyPlatformRuntimeSettings();
    }

    [ContextMenu("Apply Platform Runtime Settings")]
    public void ApplyPlatformRuntimeSettings()
    {
#if UNITY_ANDROID || UNITY_IOS
        Application.targetFrameRate = targetFrameRateMobile;
        QualitySettings.SetQualityLevel(mobileQualityLevel, true);
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
#elif UNITY_WEBGL
        Application.targetFrameRate = targetFrameRateWebGL;
        QualitySettings.SetQualityLevel(webGLQualityLevel, true);
#else
        Application.targetFrameRate = targetFrameRatePC;
        QualitySettings.SetQualityLevel(standaloneQualityLevel, true);
#endif
    }
}
