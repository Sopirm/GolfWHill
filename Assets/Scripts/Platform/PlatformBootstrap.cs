using UnityEngine;

public class PlatformBootstrap : MonoBehaviour
{
    [SerializeField] private PlatformInputManager platformInputManagerPrefab;
    [SerializeField] private PlatformRuntimeSettings runtimeSettingsPrefab;

    private void Awake()
    {
        EnsurePlatformInputManager();
        EnsureRuntimeSettings();
    }

    private void EnsurePlatformInputManager()
    {
        if (FindObjectOfType<PlatformInputManager>() != null)
        {
            return;
        }

        if (platformInputManagerPrefab != null)
        {
            Instantiate(platformInputManagerPrefab);
            return;
        }

        GameObject inputManagerObject = new GameObject("PlatformInputManager");
        inputManagerObject.AddComponent<PlatformInputManager>();
    }

    private void EnsureRuntimeSettings()
    {
        if (FindObjectOfType<PlatformRuntimeSettings>() != null)
        {
            return;
        }

        if (runtimeSettingsPrefab != null)
        {
            Instantiate(runtimeSettingsPrefab);
            return;
        }

        GameObject runtimeSettingsObject = new GameObject("PlatformRuntimeSettings");
        runtimeSettingsObject.AddComponent<PlatformRuntimeSettings>();
    }
}
