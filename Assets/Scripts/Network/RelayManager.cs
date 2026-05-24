using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Relay;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class RelayManager : MonoBehaviour
{
    private static RelayManager instance;

    private string joinCode = string.Empty;

    public static RelayManager Instance
    {
        get
        {
            EnsureInstanceExists();
            return instance;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInstanceExists();
    }

    private static void EnsureInstanceExists()
    {
        if (instance != null)
        {
            return;
        }

        instance = FindFirstObjectByType<RelayManager>();
        if (instance != null)
        {
            return;
        }

        var relayObject = new GameObject(nameof(RelayManager));
        instance = relayObject.AddComponent<RelayManager>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public async Task<string> CreateRelayAsync(int maxPlayers = 4)
    {
        bool initialized = await AuthenticationManager.EnsureInitializedAsync();
        if (!initialized)
        {
            Debug.LogError("Relay creation failed because Authentication is not initialized.");
            return null;
        }

        try
        {
            Debug.Log("Creating relay allocation...");

            var transport = GetTransport();
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers);

            joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            transport.SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, "dtls"));

            bool hostStarted = NetworkManager.Singleton.StartHost();
            if (!hostStarted)
            {
                Debug.LogError("Failed to start host after relay allocation was created.");
                return null;
            }

            Debug.Log($"Relay host started. Join code: {joinCode}");
            return joinCode;
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"Relay creation failed: {e.Message}");
            return null;
        }
        catch (Exception e)
        {
            Debug.LogError($"Unexpected relay host error: {e.Message}");
            return null;
        }
    }

    public async Task<bool> JoinRelayAsync(string relayJoinCode)
    {
        bool initialized = await AuthenticationManager.EnsureInitializedAsync();
        if (!initialized)
        {
            Debug.LogError("Relay join failed because Authentication is not initialized.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(relayJoinCode))
        {
            Debug.LogWarning("Cannot join relay without a join code.");
            return false;
        }

        try
        {
            Debug.Log($"Joining relay with code: {relayJoinCode}");

            var transport = GetTransport();
            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(relayJoinCode);

            transport.SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, "dtls"));
            bool clientStarted = NetworkManager.Singleton.StartClient();
            if (!clientStarted)
            {
                Debug.LogError("Failed to start client after joining relay allocation.");
                return false;
            }

            joinCode = relayJoinCode.ToUpperInvariant();
            Debug.Log("Relay client started successfully.");
            return true;
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"Relay join failed: {e.Message}");
            return false;
        }
        catch (Exception e)
        {
            Debug.LogError($"Unexpected relay client error: {e.Message}");
            return false;
        }
    }

    public void Disconnect()
    {
        if (NetworkManager.Singleton == null)
        {
            return;
        }

        if (NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
        }

        joinCode = string.Empty;
        Debug.Log("Disconnected from relay session.");
    }

    public string GetJoinCode()
    {
        return joinCode;
    }

    public void CopyJoinCodeToClipboard()
    {
        if (string.IsNullOrWhiteSpace(joinCode))
        {
            Debug.LogWarning("Join code is empty, nothing to copy.");
            return;
        }

        GUIUtility.systemCopyBuffer = joinCode;
        Debug.Log($"Join code copied to clipboard: {joinCode}");
    }

    private static UnityTransport GetTransport()
    {
        var networkManager = NetworkManager.Singleton;
        if (networkManager == null)
        {
            throw new InvalidOperationException("NetworkManager.Singleton is not available.");
        }

        var transport = networkManager.GetComponent<UnityTransport>();
        if (transport == null)
        {
            throw new InvalidOperationException("UnityTransport is missing on NetworkManager.");
        }

#if UNITY_EDITOR
        // Multiplayer Play Mode and other editor tools can leave a custom driver constructor behind.
        UnityTransport.s_DriverConstructor = null;
#endif

        return transport;
    }
}
