using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

public class AuthenticationManager : MonoBehaviour
{
    private const string DefaultPlayerName = "Anonymous";

    private static AuthenticationManager instance;

    [Header("Authentication Settings")]
    [SerializeField] private bool autoSignIn = true;

    private bool servicesInitialized;
    private bool authEventsSubscribed;
    private Task initializationTask;

    public static AuthenticationManager Instance
    {
        get
        {
            EnsureInstanceExists();
            return instance;
        }
    }

    public bool IsAuthenticated { get; private set; }
    public string PlayerId { get; private set; } = string.Empty;
    public string PlayerName { get; private set; } = DefaultPlayerName;

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

        instance = FindFirstObjectByType<AuthenticationManager>();
        if (instance != null)
        {
            return;
        }

        var authObject = new GameObject(nameof(AuthenticationManager));
        instance = authObject.AddComponent<AuthenticationManager>();
    }

    public static Task<bool> EnsureInitializedAsync()
    {
        return Instance.InitializeAsync();
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

    private async void Start()
    {
        await InitializeAsync();
    }

    private async Task<bool> InitializeAsync()
    {
        if (initializationTask != null)
        {
            await initializationTask;
            return IsAuthenticated || !autoSignIn;
        }

        initializationTask = InitializeInternalAsync();
        await initializationTask;
        return IsAuthenticated || !autoSignIn;
    }

    private async Task InitializeInternalAsync()
    {
        if (!servicesInitialized)
        {
            try
            {
                if (UnityServices.State == ServicesInitializationState.Uninitialized)
                {
                    await UnityServices.InitializeAsync();
                }

                servicesInitialized = true;
                SubscribeToAuthenticationEvents();
                Debug.Log("Unity Services initialized successfully.");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to initialize Unity Services: {e.Message}");
                return;
            }
        }

        if (autoSignIn)
        {
            await SignInAnonymouslyAsync();
        }
        else
        {
            RefreshPlayerState();
        }
    }

    public async Task<bool> SignInAnonymouslyAsync()
    {
        if (!servicesInitialized)
        {
            bool initialized = await InitializeAsync();
            if (!initialized && !servicesInitialized)
            {
                return false;
            }
        }

        try
        {
            if (AuthenticationService.Instance.IsSignedIn)
            {
                RefreshPlayerState();
                Debug.Log("Already signed in.");
                return true;
            }

            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            RefreshPlayerState();

            Debug.Log($"Signed in successfully. Player ID: {PlayerId}");
            return true;
        }
        catch (Exception e)
        {
            IsAuthenticated = false;
            PlayerId = string.Empty;
            PlayerName = DefaultPlayerName;
            Debug.LogError($"Sign in failed: {e.Message}");
            return false;
        }
    }

    public void SignOut()
    {
        if (!servicesInitialized)
        {
            return;
        }

        AuthenticationService.Instance.SignOut();
        IsAuthenticated = false;
        PlayerId = string.Empty;
        PlayerName = DefaultPlayerName;
        Debug.Log("Signed out.");
    }

    public async Task UpdatePlayerNameAsync(string newName)
    {
        if (!IsAuthenticated)
        {
            Debug.LogWarning("Cannot update player name because user is not authenticated.");
            return;
        }

        try
        {
            await AuthenticationService.Instance.UpdatePlayerNameAsync(newName);
            PlayerName = string.IsNullOrWhiteSpace(newName) ? DefaultPlayerName : newName;
            Debug.Log($"Player name updated to: {PlayerName}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to update player name: {e.Message}");
        }
    }

    private void SubscribeToAuthenticationEvents()
    {
        if (authEventsSubscribed)
        {
            return;
        }

        AuthenticationService.Instance.SignedIn += HandleSignedIn;
        AuthenticationService.Instance.SignedOut += HandleSignedOut;
        AuthenticationService.Instance.SignInFailed += HandleSignInFailed;
        authEventsSubscribed = true;
    }

    private void UnsubscribeFromAuthenticationEvents()
    {
        if (!authEventsSubscribed || UnityServices.State == ServicesInitializationState.Uninitialized)
        {
            return;
        }

        AuthenticationService.Instance.SignedIn -= HandleSignedIn;
        AuthenticationService.Instance.SignedOut -= HandleSignedOut;
        AuthenticationService.Instance.SignInFailed -= HandleSignInFailed;
        authEventsSubscribed = false;
    }

    private void HandleSignedIn()
    {
        RefreshPlayerState();
        Debug.Log($"Authentication state changed: Signed In ({PlayerId})");
    }

    private void HandleSignedOut()
    {
        IsAuthenticated = false;
        PlayerId = string.Empty;
        PlayerName = DefaultPlayerName;
        Debug.Log("Authentication state changed: Signed Out");
    }

    private void HandleSignInFailed(RequestFailedException error)
    {
        IsAuthenticated = false;
        Debug.LogError($"Sign in failed: {error.Message}");
    }

    private void RefreshPlayerState()
    {
        IsAuthenticated = AuthenticationService.Instance.IsSignedIn;
        PlayerId = IsAuthenticated ? AuthenticationService.Instance.PlayerId : string.Empty;
        PlayerName = IsAuthenticated && !string.IsNullOrWhiteSpace(AuthenticationService.Instance.PlayerName)
            ? AuthenticationService.Instance.PlayerName
            : DefaultPlayerName;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            UnsubscribeFromAuthenticationEvents();
            instance = null;
        }
    }
}
