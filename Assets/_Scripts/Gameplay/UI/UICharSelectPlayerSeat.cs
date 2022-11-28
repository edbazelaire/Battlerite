using _Scripts.Gameplay.GameState;
using Unity.Multiplayer.Samples.BossRoom.Client;
using UnityEngine;
using UnityEngine.UI;
using Avatar = _Scripts.Gameplay.Configuration.Avatar;

namespace _Scripts.Gameplay.UI
{
    /// <summary>
    /// Controls one of the eight "seats" on the character-select screen (the boxes along the bottom).
    /// </summary>
    public class UICharSelectPlayerSeat : MonoBehaviour
    {
        public Avatar avatar;
        
        [SerializeField]
        private Button m_Button;
        
        [SerializeField]
        private GameObject portraitOver;
        
        [SerializeField]
        private Image border;

        // just a way to designate which seat we are -- the leftmost seat on the lobby UI is index 0, the next one is index 1, etc.
        private int m_SeatIndex;

        // playerNumber of who is sitting in this seat right now. 0-based; e.g. this is 0 for Player 1, 1 for Player 2, etc. Meaningless when m_State is Inactive (and in that case it is set to -1 for clarity)
        private int m_PlayerNumber;

        // the last SeatState we were assigned
        private CharSelectData.SeatState m_State;

        // once this is true, we're never clickable again!
        private bool m_IsDisabled;

        public void Initialize(int seatIndex)
        {
            m_SeatIndex = seatIndex;
            m_State = CharSelectData.SeatState.Inactive;
            m_PlayerNumber = -1;
            ConfigureStateGraphics();
        }

        public void SetState(CharSelectData.SeatState state, int playerIndex, string playerName)
        {
            if (state == m_State && playerIndex == m_PlayerNumber)
                return; // no actual changes

            m_State = state;
            m_PlayerNumber = playerIndex;
            if (m_State == CharSelectData.SeatState.Inactive)
                m_PlayerNumber = -1;
            ConfigureStateGraphics();
        }

        public bool IsLocked()
        {
            return m_State == CharSelectData.SeatState.LockedIn;
        }

        public void SetDisableInteraction(bool disable)
        {
            m_Button.interactable = !disable;
            m_IsDisabled = disable;

            if (!disable)
            {
                // if we were locked move to unlocked state
                PlayUnlockAnim();
            }
        }

        private void PlayLockAnim() {
            border.color = Color.blue;
            portraitOver.SetActive(true);
        }

        private void PlayUnlockAnim() {
            border.color = Color.white;
            portraitOver.SetActive(false);
        }

        private void ConfigureStateGraphics()
        {
            if (m_State == CharSelectData.SeatState.Inactive)
            {
                m_Button.interactable = m_IsDisabled ? false : true;
                PlayUnlockAnim();
            }
            else // either active or locked-in... these states are visually very similar
            {
                m_Button.interactable = m_IsDisabled ? false : true;

                if (m_State == CharSelectData.SeatState.LockedIn)
                {
                    m_Button.interactable = false;
                    PlayLockAnim();
                }
                else
                {
                    PlayUnlockAnim();
                }
            }
        }

        // Called directly by Button in UI
        public void OnClicked()
        {
            print("client clicked on seat " + m_SeatIndex);
            ClientCharSelectState.Instance.OnPlayerClickedSeat(m_SeatIndex);
        }

    }
}
