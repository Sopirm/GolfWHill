using System;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class NetworkLauncher : MonoBehaviour
{
    private const string LocalAddress = "127.0.0.1";
    private const ushort LocalPort = 7777;

    [SerializeField] private TMP_InputField joinCodeInput;
    [SerializeField] private TMP_Text statusText;

    async void Start()
    {
        await InitializeServicesAsync();
    }

    public void StartHostLocal()
    {
        try
        {
            PrepareTransportForLocalConnection();
            bool result = NetworkManager.Singleton.StartHost();
            statusText.text = result ? "Локальный Host запущен." : "Не удалось запустить Host.";
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            statusText.text = $"Ошибка запуска Host: {ex.Message}";
        }
    }

    public void StartClientLocal()
    {
        try
        {
            PrepareTransportForLocalConnection();
            bool result = NetworkManager.Singleton.StartClient();
            statusText.text = result ? "Локальный Client запущен." : "Не удалось запустить Client.";
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            statusText.text = $"Ошибка запуска Client: {ex.Message}";
        }
    }

    public async void StartHostRelay()
    {
        await InitializeServicesAsync();

        ResetCustomDriverConstructor();
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(3);
        string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

        transport.SetRelayServerData(new RelayServerData(allocation, "dtls"));

        bool result = NetworkManager.Singleton.StartHost();
        statusText.text = result
            ? $"Relay Host запущен. Join Code: {joinCode}"
            : "Не удалось запустить Relay Host.";
    }

    public async void StartClientRelay()
    {
        await InitializeServicesAsync();

        string joinCode = joinCodeInput.text.Trim().ToUpper();

        if (string.IsNullOrEmpty(joinCode))
        {
            statusText.text = "Введите join code.";
            return;
        }

        ResetCustomDriverConstructor();
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

        transport.SetRelayServerData(new RelayServerData(allocation, "dtls"));

        bool result = NetworkManager.Singleton.StartClient();
        statusText.text = result
            ? "Client подключён через Relay."
            : "Не удалось подключить Client через Relay.";
    }

    private async Task InitializeServicesAsync()
    {
        if (UnityServices.State == ServicesInitializationState.Uninitialized)
            await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    private void PrepareTransportForLocalConnection()
    {
        ResetCustomDriverConstructor();

        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport == null)
        {
            throw new InvalidOperationException("UnityTransport не найден на объекте NetworkManager.");
        }

        transport.UseWebSockets = false;
        transport.UseEncryption = false;
        transport.SetConnectionData(true, LocalAddress, LocalPort, LocalAddress);
    }

    private void ResetCustomDriverConstructor()
    {
#if UNITY_EDITOR
        // Local editor tools can override the transport driver and leave UTP in an incompatible state.
        UnityTransport.s_DriverConstructor = null;
#endif
    }
}
