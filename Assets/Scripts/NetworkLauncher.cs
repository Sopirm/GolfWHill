using System;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class NetworkLauncher : MonoBehaviour
{
    private const string LocalAddress = "127.0.0.1";
    private const ushort LocalPort = 7777;
    private const string DefaultLobbyName = "Golf Lobby";
    private const int DefaultLobbyMaxPlayers = 4;

    [SerializeField] private TMP_InputField joinCodeInput;
    [SerializeField] private TMP_InputField lobbyCodeInput;
    [SerializeField] private TMP_InputField lobbyNameInput;
    [SerializeField] private TMP_Text statusText;

    private async void Start()
    {
        bool initialized = await AuthenticationManager.EnsureInitializedAsync();
        if (!initialized)
        {
            statusText.text = "Unity Services не удалось инициализировать.";
        }
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
        string relayJoinCode = await RelayManager.Instance.CreateRelayAsync(3);
        statusText.text = string.IsNullOrEmpty(relayJoinCode)
            ? "Не удалось запустить Relay Host."
            : $"Relay Host запущен. Join Code: {relayJoinCode}";
    }

    public async void StartClientRelay()
    {
        string lobbyCode = ReadInput(lobbyCodeInput).ToUpper();
        if (!string.IsNullOrEmpty(lobbyCode))
        {
            var lobby = await LobbyManager.Instance.JoinLobbyByCodeAsync(lobbyCode);
            statusText.text = lobby == null
                ? "Не удалось войти в lobby."
                : $"Вход в lobby выполнен: {lobby.Name}";
            return;
        }

        string joinCode = ReadInput(joinCodeInput).ToUpper();

        if (string.IsNullOrEmpty(joinCode))
        {
            statusText.text = "Введите lobby code или relay join code.";
            return;
        }

        bool result = await RelayManager.Instance.JoinRelayAsync(joinCode);
        statusText.text = result
            ? "Client подключён через Relay."
            : "Не удалось подключить Client через Relay.";
    }

    public async void CreateLobby()
    {
        string lobbyName = ReadInput(lobbyNameInput);
        if (string.IsNullOrEmpty(lobbyName))
        {
            lobbyName = DefaultLobbyName;
        }

        var lobby = await LobbyManager.Instance.CreateLobbyAsync(lobbyName, DefaultLobbyMaxPlayers, false, true);
        if (lobby == null)
        {
            statusText.text = "Не удалось создать lobby.";
            return;
        }

        string relayJoinCode = LobbyManager.Instance.GetRelayJoinCode();
        statusText.text = string.IsNullOrWhiteSpace(relayJoinCode)
            ? $"Lobby создано. Lobby Code: {lobby.LobbyCode}"
            : $"Lobby создано. Lobby Code: {lobby.LobbyCode}, Relay: {relayJoinCode}";
    }

    public async void QuickJoinLobby()
    {
        var lobby = await LobbyManager.Instance.QuickJoinLobbyAsync();
        statusText.text = lobby == null
            ? "Не удалось быстро войти в lobby."
            : $"Вход в lobby выполнен: {lobby.Name}";
    }

    public async void JoinLobbyByCode()
    {
        string lobbyCode = ReadLobbyCode();
        if (string.IsNullOrEmpty(lobbyCode))
        {
            statusText.text = "Введите lobby code.";
            return;
        }

        var lobby = await LobbyManager.Instance.JoinLobbyByCodeAsync(lobbyCode);
        statusText.text = lobby == null
            ? "Не удалось войти в lobby."
            : $"Вход в lobby выполнен: {lobby.Name}";
    }

    public async void LeaveLobby()
    {
        await LobbyManager.Instance.LeaveLobbyAsync();
        statusText.text = "Вы вышли из lobby.";
    }

    public void CopyLobbyCode()
    {
        string lobbyCode = LobbyManager.Instance.CurrentLobbyCode;
        if (string.IsNullOrWhiteSpace(lobbyCode))
        {
            statusText.text = "Lobby code пока недоступен.";
            return;
        }

        GUIUtility.systemCopyBuffer = lobbyCode;
        statusText.text = $"Lobby code скопирован: {lobbyCode}";
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

    private string ReadLobbyCode()
    {
        string lobbyCode = ReadInput(lobbyCodeInput).ToUpper();
        if (!string.IsNullOrEmpty(lobbyCode))
        {
            return lobbyCode;
        }

        return ReadInput(joinCodeInput).ToUpper();
    }

    private static string ReadInput(TMP_InputField inputField)
    {
        return inputField == null ? string.Empty : inputField.text.Trim();
    }
}
