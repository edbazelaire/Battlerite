using System.Text.RegularExpressions;
using _Scripts.Gameplay.ConnectionManagement;
using _Scripts.Infrastructure;
using TMPro;
using Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure;
using UnityEngine;

namespace _Scripts.Ui.MainMenu
{
    public class IPUIMediator : MonoBehaviour
    {
        public const string DEFAULT_IP = "127.0.0.1";
        public const int DEFAULT_PORT = 9998;

        [SerializeField] TextMeshProUGUI m_PlayerNameLabel;

        [SerializeField] GameObject m_SignInSpinner;

        [SerializeField]
        IPConnectionWindow m_IPConnectionWindow;

        private GameNetPortal _gameNetPortal;
        private ClientGameNetPortal _clientNetPortal;
        private IPublisher<ConnectStatus> _connectStatusPublisher;
        
        [Inject]
        void InjectDependenciesAndInitialize(
            GameNetPortal gameNetPortal,
            ClientGameNetPortal clientGameNetPortal,
            IPublisher<ConnectStatus> connectStatusPublisher
        )
        {
            _gameNetPortal = gameNetPortal;
            _clientNetPortal = clientGameNetPortal;
            _connectStatusPublisher = connectStatusPublisher;
        }

        public void HostIPRequest(string ip, string port)
        {
            int.TryParse(port, out var portNum);
            if (portNum <= 0)
            {
                portNum = DEFAULT_PORT;
            }

            ip = string.IsNullOrEmpty(ip) ? DEFAULT_IP : ip;

            _gameNetPortal.PlayerName = m_PlayerNameLabel.text;

            if (_gameNetPortal.StartHost(ip, portNum))
            {
                m_SignInSpinner.SetActive(true);
            }
            else
            {
                _connectStatusPublisher.Publish(ConnectStatus.StartHostFailed);
            }
        }

        public void JoinWithIP(string ip, string port)
        {
            int.TryParse(port, out var portNum);
            if (portNum <= 0)
            {
                portNum = DEFAULT_PORT;
            }

            ip = string.IsNullOrEmpty(ip) ? DEFAULT_IP : ip;

            _gameNetPortal.PlayerName = m_PlayerNameLabel.text;

            m_SignInSpinner.SetActive(true);

            _clientNetPortal.StartClient(ip, portNum);

            m_IPConnectionWindow.ShowConnectingWindow();
        }

        public void JoiningWindowCancelled()
        {
            DisableSignInSpinner();
            RequestShutdown();
        }

        public void DisableSignInSpinner()
        {
            m_SignInSpinner.SetActive(false);
        }

        void RequestShutdown()
        {
            if (_gameNetPortal && _gameNetPortal.NetManager)
            {
                _gameNetPortal.NetManager.Shutdown();
            }
        }


        /// <summary>
        /// Sanitize user port InputField box allowing only alphanumerics and '.'
        /// </summary>
        /// <param name="dirtyString"> string to sanitize. </param>
        /// <returns> Sanitized text string. </returns>
        public static string Sanitize(string dirtyString)
        {
            return Regex.Replace(dirtyString, "[^A-Za-z0-9.]", "");
        }
        

        public void OnCreateClick()
        {
            HostIPRequest(DEFAULT_IP, DEFAULT_PORT.ToString());
        }
        
        public void OnJoinButtonPressed()
        {
            JoinWithIP(Sanitize(DEFAULT_IP), Sanitize(DEFAULT_PORT.ToString()));
        }
        
        // To be called from the Cancel (X) UI button
        public void CancelConnectingWindow()
        {
            RequestShutdown();
            m_IPConnectionWindow.CancelConnectionWindow();
        }
    }
}
