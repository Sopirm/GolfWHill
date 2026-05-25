using UnityEngine;

[RequireComponent(typeof(Camera))]
[DisallowMultipleComponent]
public class MenuFallbackCameraController : MonoBehaviour
{
    [SerializeField] private float checkInterval = 0.25f;

    private Camera fallbackCamera;
    private AudioListener fallbackListener;
    private float nextCheckTime;

    private void Awake()
    {
        fallbackCamera = GetComponent<Camera>();
        fallbackListener = GetComponent<AudioListener>();
        SetFallbackState(true);
    }

    private void Update()
    {
        if (Time.unscaledTime < nextCheckTime)
        {
            return;
        }

        nextCheckTime = Time.unscaledTime + checkInterval;
        bool localGameplayCameraExists = HasActiveNonFallbackCamera();
        SetFallbackState(!localGameplayCameraExists);
    }

    private bool HasActiveNonFallbackCamera()
    {
        Camera[] cameras = Camera.allCameras;
        for (int i = 0; i < cameras.Length; i++)
        {
            Camera cameraItem = cameras[i];
            if (cameraItem == null || cameraItem == fallbackCamera)
            {
                continue;
            }

            if (cameraItem.isActiveAndEnabled && cameraItem.gameObject.activeInHierarchy)
            {
                return true;
            }
        }

        return false;
    }

    private void SetFallbackState(bool enabledState)
    {
        if (fallbackCamera != null)
        {
            fallbackCamera.enabled = enabledState;
        }

        if (fallbackListener != null)
        {
            fallbackListener.enabled = enabledState;
        }
    }
}
