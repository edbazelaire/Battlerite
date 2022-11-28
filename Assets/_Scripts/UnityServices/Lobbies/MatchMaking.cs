using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
// using ParrelSync;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class MatchMaking : MonoBehaviour
{
    private Lobby _connectedLobby;
    private QueryResponse _lobbies;
    private UnityTransport _transport;
    private const string JoinCodeKey = "j";
    private string _playerId;

    private const int MAX_PLAYERS = 2;

    async void Awake() {
        await Authenticate();
        SetUpEvents();
        _transport = FindObjectOfType<UnityTransport>();
        
        DontDestroyOnLoad(gameObject);
    }

    public async Task<bool> CreateOrJoinLobby()
    {
        _connectedLobby = await QuickJoinLobby() ?? await CreateLobby();
        return _connectedLobby != null;
    }

    private async Task Authenticate()
    {
        var options = new InitializationOptions();

// #if UNITY_EDITOR
//         // diff clients during ParaSync
//         options.SetProfile(ClonesManager.IsClone() ? ClonesManager.GetArgument() : "Primary");
// #endif

        await UnityServices.InitializeAsync(options);
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        _playerId = AuthenticationService.Instance.PlayerId;
    }

    private async Task<Lobby> QuickJoinLobby()
    {
        try {
            var options = new QuickJoinLobbyOptions();
            
            // join a lobby in progress
            var lobby = await Lobbies.Instance.QuickJoinLobbyAsync(options);

            // get relay allocation details if lobby found
            var a = await RelayService.Instance.JoinAllocationAsync(lobby.Data[JoinCodeKey].Value);

            // set details to the transform
            SetTransformAsClient(a);

            // join the game room as client
            NetworkManager.Singleton.StartClient();
            return lobby;
        }
        catch (Exception e)
        {
            Debug.Log("No lobbies available via quick join : " + e);
            return null;
        }
    }

    private async Task<Lobby> CreateLobby()
    {
        try
        {
            // create relay allocation with join code to set in lobby
            var a = await RelayService.Instance.CreateAllocationAsync(MAX_PLAYERS);
            var joinCode = await RelayService.Instance.GetJoinCodeAsync(a.AllocationId);
            
            // create a lobby, adding the relay join code to the lobby data
            var options = new CreateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                    { { JoinCodeKey, new DataObject(DataObject.VisibilityOptions.Public, joinCode) } }
            };

            var lobby = await Lobbies.Instance.CreateLobbyAsync("TestLobby", MAX_PLAYERS, options);
            
            // send a heartbeat every 15 seconds to keep room alive
            StartCoroutine(HeartbeatLobbyCoroutine(lobby.Id, 15));
            
            // set the game room to use te relay allocation
            _transport.SetHostRelayData(a.RelayServer.IpV4, (ushort)a.RelayServer.Port, a.AllocationIdBytes, a.Key, a.ConnectionData);
            
            // Start the room
            NetworkManager.Singleton.StartHost();
            return lobby;
        }
        catch (Exception) {
            Debug.LogFormat("Failed creating a lobby");
            return null;
        }
    }

    private void SetTransformAsClient(JoinAllocation a)
    {
        _transport.SetClientRelayData(a.RelayServer.IpV4, (ushort)a.RelayServer.Port, a.AllocationIdBytes, a.Key, a.ConnectionData, a.HostConnectionData);
    }

    private static IEnumerator HeartbeatLobbyCoroutine(string lobbyId, float waitTimeSeconds)
    {
        var delay = new WaitForSecondsRealtime(waitTimeSeconds);
        while (true)
        {
            Lobbies.Instance.SendHeartbeatPingAsync(lobbyId);
            yield return delay;
        }
    }

    private void OnDestroy()
    {
        try
        {
            StopAllCoroutines();

            // todo : add check to see if "is host"
            if (_connectedLobby != null)
            {
                if (_connectedLobby.HostId == _playerId) Lobbies.Instance.DeleteLobbyAsync(_connectedLobby.Id);
                else Lobbies.Instance.RemovePlayerAsync(_connectedLobby.Id, _playerId);
            }
        }
        catch (Exception e)
        {
            Debug.Log($"Error shutting down lobby : {e}");
        }
    }

    void SetUpEvents() {
        AuthenticationService.Instance.SignedIn += () => {
            Debug.Log($"PlayerID : {AuthenticationService.Instance.PlayerId}");
            Debug.Log($"Access Token : {AuthenticationService.Instance.AccessToken}");
        };
        
        AuthenticationService.Instance.SignInFailed += (err) => {
            Debug.Log(err);
        };
        
        AuthenticationService.Instance.SignedOut += () => {
            Debug.Log("Player signed out");
        };
    }
    
    // =================================================================================================================
    #region Debug

    public async void GetAllLobies() {
        try {
            QueryLobbiesOptions options = new QueryLobbiesOptions();
            options.Count = 25;

            // Filter for open lobbies only
            options.Filters = new List<QueryFilter>()
            {
                new QueryFilter(
                    field: QueryFilter.FieldOptions.AvailableSlots,
                    op: QueryFilter.OpOptions.GT,
                    value: "0")
            };

            // Order by newest lobbies first
            options.Order = new List<QueryOrder>()
            {
                new QueryOrder(
                    asc: false,
                    field: QueryOrder.FieldOptions.Created)
            };

            QueryResponse lobbies = await Lobbies.Instance.QueryLobbiesAsync(options);
            
            Debug.Log($"NUM LOBBIES : {lobbies.Results.Count}");
            foreach (var lobby in lobbies.Results) {
                PrintLobby(lobby);
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    void PrintLobby(Lobby lobby) {
        print("========================================================================================");
        Debug.Log($"AvailableSlots  : {lobby.AvailableSlots}");
        Debug.Log($"Created         : {lobby.Created}");
        Debug.Log($"Data            : {lobby.Data}");
        Debug.Log($"EnvironmentId   : {lobby.EnvironmentId}");
        Debug.Log($"HostId          : {lobby.HostId}");
        Debug.Log($"Id              : {lobby.Id}");
        Debug.Log($"IsLocked        : {lobby.IsLocked}");
        Debug.Log($"IsPrivate       : {lobby.IsPrivate}");
        Debug.Log($"LobbyCode       : {lobby.LobbyCode}");
        Debug.Log($"MaxPlayers      : {lobby.MaxPlayers}");
        Debug.Log($"Name            : {lobby.Name}");
        Debug.Log($"Players         : {lobby.Players.Count}");
    }

    #endregion
}
