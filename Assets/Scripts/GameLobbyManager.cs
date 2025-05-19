using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Networking.Transport;
using Unity.Services.Lobbies.Models;
using Unity.Services.Authentication;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine.SceneManagement;
using Exception = System.Exception;
using Random = UnityEngine.Random;

public class GameLobbyManager : MonoBehaviour
{
    public static GameLobbyManager Instance {get; private set; }

    private Lobby _joinedLobby;

    private void Awake()
    {
        Instance = this;
        
        DontDestroyOnLoad(gameObject);

        InitializeUnityAuthentication();
    }

    private void Update()
    {
        HandleHearthbeat();

        HandlePeriodicListLobbies();
    }

    private async void InitializeUnityAuthentication()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            InitializationOptions initializationOptions = new InitializationOptions();

            // Doit etre fait lors de test sur le meme ordinateur
            initializationOptions.SetProfile(Random.Range(0, 100000).ToString());

            await UnityServices.InitializeAsync(initializationOptions);

            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }

    private const float HEART_BEAT_TIMER_MAX = 15f;
    private float _heartbeatTimer = HEART_BEAT_TIMER_MAX;
    
    private void HandleHearthbeat()
    {
        if (IsLobbyHost())
        {
            _heartbeatTimer -= Time.deltaTime;

            if (_heartbeatTimer <= 0f)
            {
                _heartbeatTimer = HEART_BEAT_TIMER_MAX;
                LobbyService.Instance.SendHeartbeatPingAsync(_joinedLobby.Id);
            }
        }
    }

    public event EventHandler<OnLobbyListChangedEventArgs> OnLobbyListChanged;
    public class OnLobbyListChangedEventArgs : EventArgs
    {
        public List<Lobby> lobbyList;
    }

    private const float LIST_LOBBIES_TIMER_MAX = 3f;
    private float _listLobbiesTimer = LIST_LOBBIES_TIMER_MAX;
    
    private void HandlePeriodicListLobbies()
    {
        if (PeriodicListingShouldStop()) { return; }

        _listLobbiesTimer -= Time.deltaTime;

        if (_listLobbiesTimer < 0)
        {
            _listLobbiesTimer = LIST_LOBBIES_TIMER_MAX;
            CarryOutUpdateLobbyListProcedure();
        }
    }
    
    private bool PeriodicListingShouldStop()
    {
        return !AuthenticationService.Instance.IsSignedIn ||
               PlayerHasAlreadyJoinedLobby() ||
               SceneManager.GetActiveScene().name != Loader.Scene.LobbyScene.ToString();
    }

    private bool PlayerHasAlreadyJoinedLobby()
    {
        return _joinedLobby != null;
    }

    // `CarryOutUpdateLobbyListProcedure` is called based on a Timer, thus it shouldn't affect the performance drastically.
    // ReSharper disable Unity.PerformanceAnalysis
    private async void CarryOutUpdateLobbyListProcedure()
    {
        try
        {
            await UpdateLobbyList();
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    private async Task UpdateLobbyList()
    {
        QueryLobbiesOptions queryLobbiesOptions = new QueryLobbiesOptions();
        
        queryLobbiesOptions.Filters = new List<QueryFilter>{
            new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT),
        };
        
        QueryResponse queryResponse = await LobbyService.Instance.QueryLobbiesAsync(queryLobbiesOptions);
        
        OnLobbyListChanged?.Invoke(this, new OnLobbyListChangedEventArgs
        {
            lobbyList = queryResponse.Results,
        });
    }

    public event EventHandler OnCreateLobbyStarted;
    public event EventHandler OnCreateLobbyFailed;
    
    public async void CarryOutCreateLobbyProcedure(string lobbyName, bool isPrivate)
    {
        OnCreateLobbyStarted?.Invoke(this, EventArgs.Empty);

        try
        {
            await CreateLobby(lobbyName, isPrivate);

            await CreateHostRelay();
            
            GameMultiplayerManager.Instance.StartHost();
            
            Loader.LoadNetwork(Loader.Scene.CharacterSelectScene);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
            OnCreateLobbyFailed?.Invoke(this, EventArgs.Empty);
            _joinedLobby = null;
        }
        
    }

    private async Task CreateLobby(string lobbyName, bool isPrivate)
    {
        CreateLobbyOptions lobbyOptions = new CreateLobbyOptions()
        {
            IsPrivate = isPrivate
        };

        _joinedLobby = await LobbyService.Instance.CreateLobbyAsync(
            lobbyName,
            GameMultiplayerManager.MAX_NUMBER_OF_PLAYERS,
            lobbyOptions
        );
    }

    public event EventHandler OnJoinStarted;
    public event EventHandler OnJoinFailed;
    public event EventHandler OnQuickJoinFailed;

    public async void QuickJoin()
    {
        OnJoinStarted?.Invoke(this, EventArgs.Empty);
        try
        {
            _joinedLobby = await LobbyService.Instance.QuickJoinLobbyAsync();
            
            bool hasNoError = await CreateClientRelay();

            if (hasNoError)
            {
                GameMultiplayerManager.Instance.StartClient();
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
            OnQuickJoinFailed?.Invoke(this, EventArgs.Empty);
            _joinedLobby = null;
        }
    }
    public async void JoinLobbyByCode(string lobbyCode)
    {
        OnJoinStarted?.Invoke(this, EventArgs.Empty);
        try
        {
            _joinedLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode);
                
            bool hasNoError = await CreateClientRelay();

            if (hasNoError)
            {
                GameMultiplayerManager.Instance.StartClient();
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
            OnJoinFailed?.Invoke(this, EventArgs.Empty);
            _joinedLobby = null;
        }
    }
    
    public async void JoinLobbyById(string lobbyId)
    {
        OnJoinStarted?.Invoke(this, EventArgs.Empty);
        try
        {
            _joinedLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId);
                
            bool hasNoError = await CreateClientRelay();

            if (hasNoError)
            {
                GameMultiplayerManager.Instance.StartClient();
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
            OnJoinFailed?.Invoke(this, EventArgs.Empty);
            _joinedLobby = null;
        }
    }

    public string GetLobbyName()
    {
        return _joinedLobby.Name;
    }
    
    public string GetLobbyCode()
    {
        return _joinedLobby.LobbyCode;
    }

    public async void LeaveLobby()
    {
        try
        {
            if (_joinedLobby != null)
            {
                await LobbyService.Instance.RemovePlayerAsync(_joinedLobby.Id, AuthenticationService.Instance.PlayerId);
                
                _joinedLobby = null;
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    public async void KickPlayer(string lobbyPlayerId)
    {
        try
        {
            if (IsLobbyHost())
            {
                await LobbyService.Instance.RemovePlayerAsync(_joinedLobby.Id, lobbyPlayerId);
            }
        }
        catch (Exception e)
        {
            Debug.Log(e);
        }
    }

    private async Task CreateHostRelay()
    {
        try
        {

            Allocation allocation = await MultiplayerRelay.AllocateRelay();

            string relayJoinCode = await MultiplayerRelay.GetRelayJoinCode(allocation);

            await SaveRelayJoinCodeInLobby(relayJoinCode);

            bool isSecure = false;
            var endpoint = GetEndpointForAllocation(
                allocation.ServerEndpoints,
                allocation.RelayServer.IpV4,
                allocation.RelayServer.Port,
                out isSecure
            );

            MultiplayerRelay.SetNetworkManagerRelayServer(ref allocation, ref endpoint, isSecure);
        }
        catch (Exception e) when (e is LobbyServiceException or RelayServiceException)
        {
            Debug.Log(e);
        }
    }

    private async Task SaveRelayJoinCodeInLobby(string relayJoinCode)
    {
            _joinedLobby = await LobbyService.Instance.UpdateLobbyAsync(_joinedLobby.Id, new UpdateLobbyOptions()
            {
                Data = new Dictionary<string, DataObject>()
                {
                    {
                        MultiplayerRelay.RELAY_JOIN_CODE_KEY,
                        new DataObject(DataObject.VisibilityOptions.Member, relayJoinCode)
                    }
                }
            });
    }
    
    private async Task<bool> CreateClientRelay()
    {
        try
        {
            Dictionary<string, DataObject> dataObjectDictionary = _joinedLobby.Data;
            
            if (TriedJoinLobbyWhenItWasInitializing(dataObjectDictionary))
            {
                ManageTriedJoinLobbyTooEarlyError();
                return false;
            }
            
            string relayJoinCode = _joinedLobby.Data[MultiplayerRelay.RELAY_JOIN_CODE_KEY].Value;
            
            JoinAllocation joinAllocation = await MultiplayerRelay.JoinRelay(relayJoinCode);
            
            bool isSecure = false;
            var endpoint = GetEndpointForAllocation(
                joinAllocation.ServerEndpoints,
                joinAllocation.RelayServer.IpV4,
                joinAllocation.RelayServer.Port,
                out isSecure
            );
        
            MultiplayerRelay.SetNetworkManagerRelayServer(ref joinAllocation, ref endpoint, isSecure);

            return true;
        }
        catch (RelayServiceException e)
        {
            Debug.Log(e);
            return false;
        }
    }

    private bool TriedJoinLobbyWhenItWasInitializing(Dictionary<string, DataObject> dataObjects)
    {
        return dataObjects == null;
    }
    
    private void ManageTriedJoinLobbyTooEarlyError()
    {
        OnJoinFailed?.Invoke(this, EventArgs.Empty);
        
        LeaveLobby();
    }
    
    private bool IsLobbyHost()
    {
        return _joinedLobby != null && _joinedLobby.HostId == AuthenticationService.Instance.PlayerId;
    }
    
    /// <summary>
    /// Determine the server endpoint for connecting to the Relay server, for either an Allocation or a JoinAllocation.
    /// If DTLS encryption is available, and there's a secure server endpoint available, use that as a secure connection. Otherwise, just connect to the Relay IP unsecured.
    /// source: https://github.com/Unity-Technologies/com.unity.services.samples.game-lobby/blob/main/Assets/Scripts/GameLobby/NGO/SetupInGame.cs
    /// </summary>
     private NetworkEndpoint GetEndpointForAllocation(
        List<RelayServerEndpoint> endpoints,
        string ip,
        int port,
        out bool isSecure)
    {
#if ENABLE_MANAGED_UNITYTLS
        foreach (RelayServerEndpoint endpoint in endpoints)
        {
            if (endpoint.Secure && endpoint.Network == RelayServerEndpoint.NetworkOptions.Udp)
            {
                isSecure = true;
                return NetworkEndpoint.Parse(endpoint.Host, (ushort)port);
            }
        }
#endif
        isSecure = false;
        return NetworkEndpoint.Parse(ip, (ushort)port);
    }

}
