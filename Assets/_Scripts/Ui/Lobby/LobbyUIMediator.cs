using System;
using System.Threading.Tasks;
using _Scripts.Gameplay.ConnectionManagement;
using _Scripts.Infrastructure;
using _Scripts.UnityServices.Auth;
using _Scripts.UnityServices.Lobbies;
using TMPro;
using Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure;
using Unity.Services.Core;
using Unity.Services.Lobbies.Models;
using UnityEngine;

namespace _Scripts.Ui.MainMenu
{
    public class LobbyUIMediator : MonoBehaviour
    {
        [SerializeField] CanvasGroup m_CanvasGroup;
        [SerializeField] TextMeshProUGUI m_PlayerNameLabel;
        [SerializeField] GameObject m_LoadingSpinner;

        AuthenticationServiceFacade m_AuthenticationServiceFacade;
        LobbyServiceFacade m_LobbyServiceFacade;
        LocalLobbyUser m_LocalUser;
        LocalLobby m_LocalLobby;
        GameNetPortal m_GameNetPortal;
        ClientGameNetPortal m_ClientNetPortal;
        IDisposable m_Subscriptions;

        const string k_DefaultLobbyName = "unnamed-lobby";

        [Inject]
        void InjectDependenciesAndInitialize(
            AuthenticationServiceFacade authenticationServiceFacade,
            LobbyServiceFacade lobbyServiceFacade,
            LocalLobbyUser localUser,
            LocalLobby localLobby,
            // NameGenerationData nameGenerationData,
            GameNetPortal gameNetPortal,
            ISubscriber<ConnectStatus> connectStatusSub,
            ClientGameNetPortal clientGameNetPortal
        )
        {
            m_AuthenticationServiceFacade = authenticationServiceFacade;
            // m_NameGenerationData = nameGenerationData;
            m_LocalUser = localUser;
            m_LobbyServiceFacade = lobbyServiceFacade;
            m_LocalLobby = localLobby;
            m_GameNetPortal = gameNetPortal;
            m_ClientNetPortal = clientGameNetPortal;

            // RegenerateName();

            m_Subscriptions = connectStatusSub.Subscribe(OnConnectStatus);
        }

        void OnConnectStatus(ConnectStatus status)
        {
            if (status == ConnectStatus.GenericDisconnect)
            {
                UnblockUIAfterLoadingIsComplete();
            }
        }

        void OnDestroy()
        {
            m_Subscriptions?.Dispose();
        }

        //Lobby and Relay calls done from UI

        public async void CreateLobbyRequest(string lobbyName, bool isPrivate, int maxPlayers)
        {
            // before sending request to lobby service, populate an empty lobby name, if necessary
            if (string.IsNullOrEmpty(lobbyName))
            {
                lobbyName = k_DefaultLobbyName;
            }

            BlockUIWhileLoadingIsInProgress();

            bool playerIsAuthorized = await m_AuthenticationServiceFacade.EnsurePlayerIsAuthorized();

            if (!playerIsAuthorized)
            {
                UnblockUIAfterLoadingIsComplete();
                return;
            }

            var lobbyCreationAttempt = await m_LobbyServiceFacade.TryCreateLobbyAsync(lobbyName, maxPlayers, isPrivate);

            if (lobbyCreationAttempt.Success)
            {
                m_LocalUser.IsHost = true;
                m_LobbyServiceFacade.SetRemoteLobby(lobbyCreationAttempt.Lobby);

                m_GameNetPortal.PlayerName = m_LocalUser.DisplayName;

                Debug.Log($"Created lobby with ID: {m_LocalLobby.LobbyID} and code {m_LocalLobby.LobbyCode}, Internal Relay Join Code{m_LocalLobby.RelayJoinCode}");
                m_GameNetPortal.StartUnityRelayHost();
            }
            else
            {
                UnblockUIAfterLoadingIsComplete();
            }
        }

        public async void QueryLobbiesRequest(bool blockUI)
        {
            if (Unity.Services.Core.UnityServices.State != ServicesInitializationState.Initialized)
            {
                return;
            }

            if (blockUI)
            {
                BlockUIWhileLoadingIsInProgress();
            }

            bool playerIsAuthorized = await m_AuthenticationServiceFacade.EnsurePlayerIsAuthorized();

            if (!playerIsAuthorized)
            {
                UnblockUIAfterLoadingIsComplete();
                return;
            }

            await m_LobbyServiceFacade.RetrieveAndPublishLobbyListAsync();
            UnblockUIAfterLoadingIsComplete();
        }

        public async void JoinLobbyWithCodeRequest(string lobbyCode)
        {
            BlockUIWhileLoadingIsInProgress();

            bool playerIsAuthorized = await m_AuthenticationServiceFacade.EnsurePlayerIsAuthorized();

            if (!playerIsAuthorized)
            {
                UnblockUIAfterLoadingIsComplete();
                return;
            }

            var result = await m_LobbyServiceFacade.TryJoinLobbyAsync(null, lobbyCode);

            if (result.Success)
            {
                OnJoinedLobby(result.Lobby);
            }
            else
            {
                UnblockUIAfterLoadingIsComplete();
            }
        }

        public async void JoinLobbyRequest(LocalLobby lobby)
        {
            BlockUIWhileLoadingIsInProgress();

            bool playerIsAuthorized = await m_AuthenticationServiceFacade.EnsurePlayerIsAuthorized();

            if (!playerIsAuthorized)
            {
                UnblockUIAfterLoadingIsComplete();
                return;
            }

            var result = await m_LobbyServiceFacade.TryJoinLobbyAsync(lobby.LobbyID, lobby.LobbyCode);

            if (result.Success)
            {
                OnJoinedLobby(result.Lobby);
            }
            else
            {
                UnblockUIAfterLoadingIsComplete();
            }
        }

        public async Task<bool> QuickJoinRequest()
        {
            BlockUIWhileLoadingIsInProgress();

            bool playerIsAuthorized = await m_AuthenticationServiceFacade.EnsurePlayerIsAuthorized();

            if (! playerIsAuthorized)
            {
                UnblockUIAfterLoadingIsComplete();
                return false;
            }

            var result = await m_LobbyServiceFacade.TryQuickJoinLobbyAsync();

            if (result.Success)
            {
                OnJoinedLobby(result.Lobby);
                return true;
            }
            
            UnblockUIAfterLoadingIsComplete();
            return false;
            
        }

        async void OnJoinedLobby(Lobby remoteLobby)
        {
            m_LobbyServiceFacade.SetRemoteLobby(remoteLobby);
            m_GameNetPortal.PlayerName = m_LocalUser.DisplayName;

            Debug.Log($"Joined lobby with code: {m_LocalLobby.LobbyCode}, Internal Relay Join Code{m_LocalLobby.RelayJoinCode}");
            await m_ClientNetPortal.StartClientUnityRelayModeAsync(OnRelayJoinFailed);

            void OnRelayJoinFailed(string message)
            {
                PopupManager.ShowPopupPanel("Relay join failed", message);
                Debug.Log($"Relay join failed: {message}");
                //leave the lobby if relay failed for some reason
                m_LobbyServiceFacade.EndTracking();
                UnblockUIAfterLoadingIsComplete();
            }
        }

     
        // public void RegenerateName()
        // {
        //     m_LocalUser.DisplayName = m_NameGenerationData.GenerateName();
        //     m_PlayerNameLabel.text = m_LocalUser.DisplayName;
        // }

        void BlockUIWhileLoadingIsInProgress()
        {
            m_CanvasGroup.interactable = false;
            m_LoadingSpinner.SetActive(true);
        }

        void UnblockUIAfterLoadingIsComplete()
        {
            //this callback can happen after we've already switched to a different scene
            //in that case the canvas group would be null
            if (m_CanvasGroup != null)
            {
                m_CanvasGroup.interactable = true;
                m_LoadingSpinner.SetActive(false);
            }
        }
    }
}
