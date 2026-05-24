using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class LobbyManager : MonoBehaviour
{
    private const string RelayJoinCodeKey = "RelayJoinCode";
    private const string PlayerNameKey = "PlayerName";
    private const float HeartbeatIntervalSeconds = 15f;
    private const float PollIntervalSeconds = 1.5f;

    private static LobbyManager instance;

    private Lobby currentLobby;
    private float heartbeatTimer;
    private float pollTimer;

    public static LobbyManager Instance
    {
        get
        {
            EnsureInstanceExists();
            return instance;
        }
    }

    public Lobby CurrentLobby => currentLobby;
    public bool IsInLobby => currentLobby != null;
    public string CurrentLobbyCode => currentLobby?.LobbyCode ?? string.Empty;

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

        instance = FindFirstObjectByType<LobbyManager>();
        if (instance != null)
        {
            return;
        }

        var lobbyObject = new GameObject(nameof(LobbyManager));
        instance = lobbyObject.AddComponent<LobbyManager>();
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

    private void Update()
    {
        HandleLobbyHeartbeat();
        HandleLobbyPolling();
    }

    public async Task<Lobby> CreateLobbyAsync(string lobbyName, int maxPlayers, bool isPrivate = false, bool createRelay = true)
    {
        bool initialized = await AuthenticationManager.EnsureInitializedAsync();
        if (!initialized)
        {
            Debug.LogError("Cannot create lobby because Authentication is not initialized.");
            return null;
        }

        if (string.IsNullOrWhiteSpace(lobbyName))
        {
            Debug.LogWarning("Lobby name cannot be empty.");
            return null;
        }

        string relayJoinCode = string.Empty;
        if (createRelay)
        {
            relayJoinCode = await RelayManager.Instance.CreateRelayAsync(maxPlayers - 1);
            if (string.IsNullOrWhiteSpace(relayJoinCode))
            {
                Debug.LogError("Lobby creation aborted because Relay host could not be created.");
                return null;
            }
        }

        try
        {
            var options = new CreateLobbyOptions
            {
                IsPrivate = isPrivate,
                Player = BuildPlayer()
            };

            if (!string.IsNullOrWhiteSpace(relayJoinCode))
            {
                options.Data = new Dictionary<string, DataObject>
                {
                    {
                        RelayJoinCodeKey,
                        new DataObject(DataObject.VisibilityOptions.Member, relayJoinCode)
                    }
                };
            }

            currentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);
            ResetTimers();
            Debug.Log($"Lobby created: {currentLobby.Name}, code: {currentLobby.LobbyCode}");
            return currentLobby;
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to create lobby: {e.Message}");

            if (createRelay)
            {
                RelayManager.Instance.Disconnect();
            }

            return null;
        }
    }

    public async Task<List<Lobby>> FindLobbiesAsync(int count = 10)
    {
        bool initialized = await AuthenticationManager.EnsureInitializedAsync();
        if (!initialized)
        {
            Debug.LogError("Cannot query lobbies because Authentication is not initialized.");
            return new List<Lobby>();
        }

        try
        {
            var options = new QueryLobbiesOptions
            {
                Count = count,
                Filters = new List<QueryFilter>
                {
                    new(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
                }
            };

            QueryResponse response = await LobbyService.Instance.QueryLobbiesAsync(options);
            return response.Results;
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to query lobbies: {e.Message}");
            return new List<Lobby>();
        }
    }

    public async Task<Lobby> QuickJoinLobbyAsync()
    {
        bool initialized = await AuthenticationManager.EnsureInitializedAsync();
        if (!initialized)
        {
            Debug.LogError("Cannot quick join because Authentication is not initialized.");
            return null;
        }

        try
        {
            currentLobby = await LobbyService.Instance.QuickJoinLobbyAsync(new QuickJoinLobbyOptions
            {
                Player = BuildPlayer()
            });

            ResetTimers();
            await TryJoinRelayFromLobbyAsync(currentLobby);
            Debug.Log($"Quick joined lobby: {currentLobby.Name}");
            return currentLobby;
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to quick join lobby: {e.Message}");
            return null;
        }
    }

    public async Task<Lobby> JoinLobbyByIdAsync(string lobbyId)
    {
        if (string.IsNullOrWhiteSpace(lobbyId))
        {
            Debug.LogWarning("Lobby ID cannot be empty.");
            return null;
        }

        bool initialized = await AuthenticationManager.EnsureInitializedAsync();
        if (!initialized)
        {
            Debug.LogError("Cannot join lobby because Authentication is not initialized.");
            return null;
        }

        try
        {
            currentLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId, new JoinLobbyByIdOptions
            {
                Player = BuildPlayer()
            });

            ResetTimers();
            await TryJoinRelayFromLobbyAsync(currentLobby);
            Debug.Log($"Joined lobby by id: {currentLobby.Name}");
            return currentLobby;
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to join lobby by id: {e.Message}");
            return null;
        }
    }

    public async Task<Lobby> JoinLobbyByCodeAsync(string lobbyCode)
    {
        if (string.IsNullOrWhiteSpace(lobbyCode))
        {
            Debug.LogWarning("Lobby code cannot be empty.");
            return null;
        }

        bool initialized = await AuthenticationManager.EnsureInitializedAsync();
        if (!initialized)
        {
            Debug.LogError("Cannot join lobby because Authentication is not initialized.");
            return null;
        }

        try
        {
            currentLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode, new JoinLobbyByCodeOptions
            {
                Player = BuildPlayer()
            });

            ResetTimers();
            await TryJoinRelayFromLobbyAsync(currentLobby);
            Debug.Log($"Joined lobby by code: {currentLobby.Name}");
            return currentLobby;
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to join lobby by code: {e.Message}");
            return null;
        }
    }

    public async Task LeaveLobbyAsync(bool disconnectRelay = true)
    {
        if (currentLobby == null)
        {
            return;
        }

        try
        {
            if (IsLobbyHost())
            {
                await LobbyService.Instance.DeleteLobbyAsync(currentLobby.Id);
                Debug.Log("Lobby deleted by host.");
            }
            else
            {
                await LobbyService.Instance.RemovePlayerAsync(currentLobby.Id, AuthenticationManager.Instance.PlayerId);
                Debug.Log("Player left lobby.");
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to leave lobby: {e.Message}");
        }
        finally
        {
            currentLobby = null;
            heartbeatTimer = 0f;
            pollTimer = 0f;

            if (disconnectRelay)
            {
                RelayManager.Instance.Disconnect();
            }
        }
    }

    public string GetRelayJoinCode()
    {
        if (currentLobby == null || currentLobby.Data == null)
        {
            return string.Empty;
        }

        return currentLobby.Data.TryGetValue(RelayJoinCodeKey, out DataObject relayCodeData)
            ? relayCodeData.Value
            : string.Empty;
    }

    private async void HandleLobbyHeartbeat()
    {
        if (currentLobby == null || !IsLobbyHost())
        {
            return;
        }

        heartbeatTimer -= Time.deltaTime;
        if (heartbeatTimer > 0f)
        {
            return;
        }

        heartbeatTimer = HeartbeatIntervalSeconds;

        try
        {
            await LobbyService.Instance.SendHeartbeatPingAsync(currentLobby.Id);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to send lobby heartbeat: {e.Message}");
        }
    }

    private async void HandleLobbyPolling()
    {
        if (currentLobby == null)
        {
            return;
        }

        pollTimer -= Time.deltaTime;
        if (pollTimer > 0f)
        {
            return;
        }

        pollTimer = PollIntervalSeconds;

        try
        {
            currentLobby = await LobbyService.Instance.GetLobbyAsync(currentLobby.Id);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to poll lobby state: {e.Message}");
        }
    }

    private async Task TryJoinRelayFromLobbyAsync(Lobby lobby)
    {
        string relayJoinCode = ExtractRelayJoinCode(lobby);
        if (string.IsNullOrWhiteSpace(relayJoinCode))
        {
            return;
        }

        bool connected = await RelayManager.Instance.JoinRelayAsync(relayJoinCode);
        if (!connected)
        {
            Debug.LogWarning("Joined lobby, but failed to connect to Relay.");
        }
    }

    private static string ExtractRelayJoinCode(Lobby lobby)
    {
        if (lobby?.Data == null)
        {
            return string.Empty;
        }

        return lobby.Data.TryGetValue(RelayJoinCodeKey, out DataObject relayCodeData)
            ? relayCodeData.Value
            : string.Empty;
    }

    private bool IsLobbyHost()
    {
        return currentLobby != null && currentLobby.HostId == AuthenticationManager.Instance.PlayerId;
    }

    private Player BuildPlayer()
    {
        return new Player(
            id: AuthenticationManager.Instance.PlayerId,
            data: new Dictionary<string, PlayerDataObject>
            {
                {
                    PlayerNameKey,
                    new PlayerDataObject(
                        PlayerDataObject.VisibilityOptions.Member,
                        AuthenticationManager.Instance.PlayerName)
                }
            });
    }

    private void ResetTimers()
    {
        heartbeatTimer = HeartbeatIntervalSeconds;
        pollTimer = PollIntervalSeconds;
    }
}
