using System;
using _Scripts.Infrastructure;
using _Scripts.UnityServices.Lobbies;
using _Scripts.Utils;
using Unity.Collections;
using Unity.Multiplayer.Samples.BossRoom;
using Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Scripts.Gameplay.ConnectionManagement
{
    public enum ConnectStatus
    {
        Undefined,
        Success,                  //client successfully connected. This may also be a successful reconnect.
        ServerFull,               //can't join, server is already at capacity.
        LoggedInAgain,            //logged in on a separate client, causing this one to be kicked out.
        UserRequestedDisconnect,  //Intentional Disconnect triggered by the user.
        GenericDisconnect,        //server disconnected, but no specific reason given.
        Reconnecting,             //client lost connection and is attempting to reconnect.
        IncompatibleBuildType,    //client build type is incompatible with server.
        HostEndedSession,         //host intentionally ended the session.
        StartHostFailed,          // server failed to bind
        StartClientFailed         // failed to connect to server and/or invalid network endpoint
    }

    public struct ReconnectMessage
    {
        public int CurrentAttempt;
        public int MaxAttempt;

        public ReconnectMessage(int currentAttempt, int maxAttempt)
        {
            CurrentAttempt = currentAttempt;
            MaxAttempt = maxAttempt;
        }
    }

    [Serializable]
    public class ConnectionPayload
    {
        public string playerId;
        public int clientScene = -1;
        public string playerName;
        public bool isDebug;
    }
    
    public class GameNetPortal : MonoBehaviour
    {
        [SerializeField] 
        private NetworkManager networkManager;
        public NetworkManager NetManager => networkManager;

        [SerializeField] 
        private AvatarRegistry m_AvatarRegistry;
        public AvatarRegistry AvatarRegistry => m_AvatarRegistry;

        public string PlayerName;
        
        /// <summary>
        /// How many connections we create a Unity relay allocation for
        /// </summary>
        private const int k_MaxUnityRelayConnections = 8;
        
        private ProfileManager _profileManager = new ProfileManager();

        public static GameNetPortal Instance;
        private ClientGameNetPortal _clientPortal;
        private ServerGameNetPortal _serverPortal;
        
        private LocalLobby _localLobby;
        private LobbyServiceFacade _lobbyServiceFacade;
        private ProfileManager m_ProfileManager;
        
        [Inject]
        private void InjectDependencies(LocalLobby localLobby, LobbyServiceFacade lobbyServiceFacade, ProfileManager profileManager)
        {
            _localLobby = localLobby;
            _lobbyServiceFacade = lobbyServiceFacade;
            m_ProfileManager = profileManager;
        }

        private void Awake()
        {
            Debug.Assert(Instance == null);
            Instance = this;
            _clientPortal = GetComponent<ClientGameNetPortal>();
            _serverPortal = GetComponent<ServerGameNetPortal>();
            DontDestroyOnLoad(gameObject);

            NetManager.OnClientConnectedCallback += ClientNetworkReadyWrapper;
        }
        
        private void OnSceneEvent(SceneEvent sceneEvent)
        {
            // only processing single player finishing loading events
            if (sceneEvent.SceneEventType != SceneEventType.LoadComplete) return;

            _serverPortal.OnClientSceneChanged(sceneEvent.ClientId, SceneManager.GetSceneByName(sceneEvent.SceneName).buildIndex);
        }

        private void OnDestroy()
        {
            if (NetManager != null)
            {
                NetManager.OnClientConnectedCallback -= ClientNetworkReadyWrapper;
            }

            Instance = null;
        }
        
        private void ClientNetworkReadyWrapper(ulong clientId)
        {
            if (clientId == NetManager.LocalClientId)
            {
                OnNetworkReady();
                if (NetManager.IsServer)
                {
                    NetManager.SceneManager.OnSceneEvent += OnSceneEvent;
                }
            }
        }
        
        /// <summary>
        /// This method runs when NetworkManager has started up (following a succesful connect on the client, or directly after StartHost is invoked
        /// on the host). It is named to match NetworkBehaviour.OnNetworkSpawn, and serves the same role, even though GameNetPortal itself isn't a NetworkBehaviour.
        /// </summary>
        private void OnNetworkReady()
        {
            if (NetManager.IsHost)
            {
                //special host code. This is what kicks off the flow that happens on a regular client
                //when it has finished connecting successfully. A dedicated server would remove this.
                _clientPortal.OnConnectFinished(ConnectStatus.Success);
            }

            _clientPortal.OnNetworkReady();
            _serverPortal.OnNetworkReady();
        }

        public bool StartHost(string ipaddress, int port)
        {
            var utp = (UnityTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport;
            utp.SetConnectionData(ipaddress, (ushort)port);

            return StartHost();
        }
        
        public async void StartUnityRelayHost()
        {
            Debug.Log("Setting up Unity Relay host");

            try
            {
                var (ipv4Address, port, allocationIdBytes, connectionData, key, joinCode) =
                    await UnityRelayUtilities.AllocateRelayServerAndGetJoinCode(k_MaxUnityRelayConnections);

                _localLobby.RelayJoinCode = joinCode;
                //next line enabled lobby and relay services integration
                await _lobbyServiceFacade.UpdateLobbyDataAsync(_localLobby.GetDataForUnityServices());
                await _lobbyServiceFacade.UpdatePlayerRelayInfoAsync(allocationIdBytes.ToString(), joinCode);

                // we now need to set the RelayCode somewhere :P
                var utp = (UnityTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport;
                utp.SetHostRelayData(ipv4Address, port, allocationIdBytes, key, connectionData, isSecure: true);
            }
            catch (Exception e)
            {
                Debug.LogErrorFormat($"{e.Message}");
                throw;
            }

            StartHost();
        }
        
        bool StartHost()
        {
            return NetManager.StartHost();
        }

        /// <summary>
        /// This will disconnect (on the client) or shutdown the server (on the host).
        /// It's a local signal (not from the network), indicating that the user has requested a disconnect.
        /// </summary>
        public void RequestDisconnect()
        {
            if (NetManager.IsServer)
            {
                NetManager.SceneManager.OnSceneEvent -= OnSceneEvent;
                SessionManager<SessionPlayerData>.Instance.OnServerEnded();
            }
            _clientPortal.OnUserDisconnectRequest();
            _serverPortal.OnUserDisconnectRequest();
        }

        public string GetPlayerId()
        {
            if (Unity.Services.Core.UnityServices.State != ServicesInitializationState.Initialized)
            {
                return ClientPrefs.GetGuid() + _profileManager.Profile;
            }

            return AuthenticationService.Instance.IsSignedIn ? AuthenticationService.Instance.PlayerId : ClientPrefs.GetGuid() + _profileManager.Profile;
        }
    }
}