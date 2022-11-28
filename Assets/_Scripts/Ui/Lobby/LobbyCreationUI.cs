using _Scripts.Infrastructure;
using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.Ui.MainMenu
{
    public class LobbyCreationUI : MonoBehaviour
    {
        [SerializeField] InputField m_LobbyNameInputField;
        [SerializeField] GameObject m_LoadingIndicatorObject;
        
        LobbyUIMediator m_LobbyUIMediator;

        private const int k_maxPlayers = 2;

        void Awake()
        {
            EnableUnityRelayUI();
        }

        [Inject]
        void InjectDependencies(LobbyUIMediator lobbyUIMediator)
        {
            m_LobbyUIMediator = lobbyUIMediator;
        }

        void EnableUnityRelayUI()
        {
            m_LoadingIndicatorObject.SetActive(false);
        }

        public async void OnStartClick()
        {
            bool lobbyJoind = await m_LobbyUIMediator.QuickJoinRequest();
            if (! lobbyJoind)
            {
                m_LobbyUIMediator.CreateLobbyRequest("TestLobby", false, k_maxPlayers);
            }
        }

        public void OnCreateClick()
        {
            // var lobbyName = m_LobbyNameInputField.text;
            m_LobbyUIMediator.CreateLobbyRequest("TestLobby", false, k_maxPlayers);
        }
    }
}
