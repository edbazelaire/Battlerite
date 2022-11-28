using System;
using _Scripts.Infrastructure;
using _Scripts.Ui.MainMenu;
using _Scripts.UnityServices.Auth;
using _Scripts.UnityServices.Lobbies;
using _Scripts.Utils;
using Unity.Multiplayer.Samples.BossRoom;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.Gameplay.GameState
{
    /// <summary>
    /// Game Logic that runs when sitting at the MainMenu. This is likely to be "nothing", as no game has been started. But it is
    /// nonetheless important to have a game state, as the GameStateBehaviour system requires that all scenes have states.
    /// </summary>
    /// <remarks> OnNetworkSpawn() won't ever run, because there is no network connection at the main menu screen.
    /// Fortunately we know you are a client, because all players are clients when sitting at the main menu screen.
    /// </remarks>
    public class ClientMainMenuState : GameStateBehaviour
    {
        public override GameState ActiveState { get { return GameState.MainMenu; } }

        [SerializeField] LobbyUIMediator m_LobbyUIMediator;
        // [SerializeField] IPUIMediator m_IPUIMediator;
        [SerializeField] GameObject m_SignInSpinner;

        AuthenticationServiceFacade m_AuthServiceFacade;
        LocalLobbyUser m_LocalUser;
        LocalLobby m_LocalLobby;
        ProfileManager m_ProfileManager;
        
        protected override void InitializeScope()
        {
            Scope.BindInstanceAsSingle(m_LobbyUIMediator);
            // Scope.BindInstanceAsSingle(m_IPUIMediator);
        }

        [Inject]
        async void InjectDependenciesAndInitialize(AuthenticationServiceFacade authServiceFacade, LocalLobbyUser localUser, LocalLobby localLobby, ProfileManager profileManager)
        {
            m_AuthServiceFacade = authServiceFacade;
            m_LocalUser = localUser;
            m_LocalLobby = localLobby;
            m_ProfileManager = profileManager;

            if (string.IsNullOrEmpty(Application.cloudProjectId))
            {
                OnSignInFailed();
                Debug.Log("SignIn Failed");
                return;
            }

            try
            {
                var unityAuthenticationInitOptions = new InitializationOptions();
                var profile = m_ProfileManager.Profile;
                if (profile.Length > 0)
                {
                    unityAuthenticationInitOptions.SetProfile(profile);
                }

                await m_AuthServiceFacade.InitializeAndSignInAsync(unityAuthenticationInitOptions);
                OnAuthSignIn();
                m_ProfileManager.onProfileChanged += OnProfileChanged;
            }
            catch (Exception)
            {
                OnSignInFailed();
                Debug.Log("SignIn Failed 2");
            }

            void OnAuthSignIn()
            {
                m_SignInSpinner.SetActive(false);
                
                m_LocalUser.ID = AuthenticationService.Instance.PlayerId;
                // The local LobbyUser object will be hooked into UI before the LocalLobby is populated during lobby join, so the LocalLobby must know about it already when that happens.
                m_LocalLobby.AddUser(m_LocalUser);
            }

            void OnSignInFailed()
            {
                if (m_SignInSpinner)
                {
                    m_SignInSpinner.SetActive(false);
                }
            }
        }

        public override void OnDestroy()
        {
            m_ProfileManager.onProfileChanged -= OnProfileChanged;
            base.OnDestroy();
        }

        async void OnProfileChanged()
        {
            m_SignInSpinner.SetActive(true);
            await m_AuthServiceFacade.SwitchProfileAndReSignInAsync(m_ProfileManager.Profile);

            m_SignInSpinner.SetActive(false);

            Debug.Log($"Signed in. Unity Player ID {AuthenticationService.Instance.PlayerId}");

            // Updating LocalUser and LocalLobby
            m_LocalLobby.RemoveUser(m_LocalUser);
            m_LocalUser.ID = AuthenticationService.Instance.PlayerId;
            m_LocalLobby.AddUser(m_LocalUser);
        }
    }
}
