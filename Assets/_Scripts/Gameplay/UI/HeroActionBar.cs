using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using _Scripts.Gameplay.Actions;
using _Scripts.Gameplay.GameplayObjects;
using _Scripts.Gameplay.GameplayObjects.RuntimeDataContainers;
using _Scripts.Gameplay.Input;
using JetBrains.Annotations;
using Unity.Multiplayer.Samples.BossRoom.Client;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;

namespace _Scripts.Gameplay.UI
{
    /// <summary>
    /// Provides logic for a Hero Action Bar with attack, skill buttons and a button to open emotes panel
    /// This bar tracks button clicks on hero action buttons for later use by ClientInputSender
    /// </summary>
    public class HeroActionBar : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("List of buttons for abilities")]
        List<UIHUDButton> m_AbilityButtons;

        // [SerializeField]
        // [Tooltip("The button that opens/closes the Emote bar")]
        // UIHUDButton m_EmoteBarButton;
        
        // [SerializeField]
        // [Tooltip("The Emote bar that will be enabled or disabled when clicking the Emote bar button")]
        // GameObject m_EmotePanel;

        /// <summary>
        /// Our input-sender. Initialized in RegisterInputSender()
        /// </summary>
        ClientInputSender m_InputSender;

        /// <summary>
        /// Cached reference to local player's net state.
        /// We find the Sprites to use by checking the Skill1, Skill2, and Skill3 members of our chosen CharacterClass
        /// </summary>
        NetworkCharacterState m_NetState;

        /// <summary>
        /// If we have another player selected, this is a reference to their stats; if anything else is selected, this is null
        /// </summary>
        NetworkCharacterState m_SelectedPlayerNetState;

        /// <summary>
        /// If m_SelectedPlayerNetState is non-null, this indicates whether we think they're alive. (Updated every frame)
        /// </summary>
        bool m_WasSelectedPlayerAliveDuringLastUpdate;

        private Coroutine _currentCoroutine;

        class ActionButtonCooldown
        {
            public float MaxCooldown;
            public float CooldownCtr;
        }

        /// <summary>
        /// Cached UI information about one of the buttons on the action bar.
        /// Takes care of registering/unregistering click-event messages,
        /// and routing the events into HeroActionBar.
        /// </summary>
        class AbilityButtonInfo
        {
            public string ActionName;
            public readonly UIHUDButton Button;
            public readonly UITooltipDetector Tooltip;
            public ActionButtonCooldown ActionButtonCooldown = new ();

            readonly HeroActionBar m_Owner;

            public AbilityButtonInfo(UIHUDButton button, string actionName, HeroActionBar owner)
            {
                Button = button;
                Tooltip = button.GetComponent<UITooltipDetector>();
                ActionName = actionName;
                m_Owner = owner;
            }
            
            public void UpdateCooldown()
            {
                if (ActionButtonCooldown.CooldownCtr == 0) { return; }
                
                ActionButtonCooldown.CooldownCtr = Mathf.Max(0, ActionButtonCooldown.CooldownCtr - Time.fixedDeltaTime);
                Button.image.fillAmount = (ActionButtonCooldown.MaxCooldown - ActionButtonCooldown.CooldownCtr) / ActionButtonCooldown.MaxCooldown;
            }
            
            public void StartCooldown()
            {
                ActionButtonCooldown.CooldownCtr = ActionButtonCooldown.MaxCooldown;
            }
        }

        /// <summary>
        /// Dictionary of info about all the buttons on the action bar.
        /// </summary>
        private List<AbilityButtonInfo> _abilityButtonInfos;

        private void FixedUpdate()
        {
            foreach (var buttonInfo in _abilityButtonInfos)
            {
                buttonInfo.UpdateCooldown();
            }
        }

        /// <summary>
        /// Cache the input sender from a <see cref="ClientPlayerAvatar"/> and self-initialize.
        /// </summary>
        /// <param name="clientPlayerAvatar"></param>
        void RegisterInputSender(ClientPlayerAvatar clientPlayerAvatar)
        {
            if (!clientPlayerAvatar.TryGetComponent(out ClientInputSender inputSender))
            {
                Debug.LogError("ClientInputSender not found on ClientPlayerAvatar!", clientPlayerAvatar);
            }

            if (m_InputSender != null)
            {
                Debug.LogWarning($"Multiple ClientInputSenders in scene? Discarding sender belonging to {m_InputSender.gameObject.name} and adding it for {inputSender.gameObject.name} ");
            }

            m_InputSender = inputSender;
            
            m_NetState = m_InputSender.GetComponent<NetworkCharacterState>();
            m_NetState.DoActionStateEventClient += OnRecvAction;
            
            UpdateAllActionButtons();
        }

        private void OnRecvAction(ActionRequestData actionRequestData, ActionPlayer.ActionState state)
        {
            // only happens when action is activated
            if (state != ActionPlayer.ActionState.Active) { return; }
            
            // only happens if action is linked to a button
            AbilityButtonInfo buttonInfo;
            if (! TryGetButtonOfAbility(actionRequestData.ActionName, out buttonInfo)) { return; }

            // display cooldown of the action
            buttonInfo.StartCooldown();
        }

        private bool TryGetButtonOfAbility(string actionType, out AbilityButtonInfo abilityButtonInfo)
        {
            foreach (var buttonInfo in _abilityButtonInfos)
            {
                if (buttonInfo.ActionName == actionType)
                {
                    abilityButtonInfo = buttonInfo;
                    return true;
                }
            }

            abilityButtonInfo = default;
            return false;
        }

        void DeregisterInputSender()
        {
            m_InputSender = null;
            m_NetState.DoActionStateEventClient -= OnRecvAction;
            m_NetState = null;
        }

        void Awake()
        {
            _abilityButtonInfos = new List<AbilityButtonInfo>();
            foreach (var button in m_AbilityButtons)
            {
                _abilityButtonInfos.Add(new AbilityButtonInfo(button, "", this));
            }

            ClientPlayerAvatar.LocalClientSpawned += RegisterInputSender;
            ClientPlayerAvatar.LocalClientDespawned += DeregisterInputSender;
        }

        void OnDestroy()
        {
            ClientPlayerAvatar.LocalClientSpawned -= RegisterInputSender;
            ClientPlayerAvatar.LocalClientDespawned -= DeregisterInputSender;
        }

        void Update()
        {
            // If we have another player selected, see if their aliveness state has changed,
            // and if so, update the interactiveness of the basic-action button

            // if (UnityEngine.Input.GetKeyUp(KeyCode.Alpha4))
            // {
            //     m_ButtonInfo[ActionButtonType.EmoteBar].Button.OnPointerUpEvent.Invoke();
            // }

            if (!m_SelectedPlayerNetState) { return; }

            bool isAliveNow = m_SelectedPlayerNetState.NetworkLifeState.LifeState.Value == LifeState.Alive;
            if (isAliveNow != m_WasSelectedPlayerAliveDuringLastUpdate)
            {
                // this will update the icons so that the basic-action button's interactiveness is correct
                UpdateAllActionButtons();
            }
        }

        void OnSelectionChanged(ulong oldSelectionNetworkId, ulong newSelectionNetworkId)
        {
            UpdateAllActionButtons();
        }
        

        /// <summary>
        /// Updates all the action buttons and caches info about the currently-selected entity (when appropriate):
        /// stores info in m_SelectedPlayerNetState and m_WasSelectedPlayerAliveDuringLastUpdate
        /// </summary>
        void UpdateAllActionButtons()
        {
            for (var i = 0; i < _abilityButtonInfos.Count; i++) 
            {
                // if not enough abilities in character : set new AbilityButtonInfo
                if (m_NetState.CharacterClass.Abilities.Length <= i)
                {
                    _abilityButtonInfos[i] = new AbilityButtonInfo(m_AbilityButtons[i], "", this);
                    _abilityButtonInfos[i].Button.gameObject.SetActive(false);
                    continue;
                }
                
                UpdateActionButton(_abilityButtonInfos[i], m_NetState.CharacterClass.Abilities[i].ActionName);
            }
        }

        void UpdateActionButton(AbilityButtonInfo buttonInfo, string actionName, bool isClickable = true)
        {
            // first find the info we need (sprite and description)
            Sprite sprite = null;
            string description = "";
            float maxCooldown = 0f;
            float executionTime = 0f;

            if (actionName != "")
            {
                var desc = GameDataSource.Instance.ActionDataByName[actionName];
                sprite = desc.Icon;
                description = desc.Description;
                maxCooldown = desc.ReuseTimeSeconds;
                executionTime = desc.ExecTimeSeconds;
            }

            // set up UI elements appropriately
            if (sprite == null)
            {
                buttonInfo.Button.gameObject.SetActive(false);
            }
            else
            {
                buttonInfo.ActionName = actionName;
                buttonInfo.Button.gameObject.SetActive(true);
                buttonInfo.Button.interactable = isClickable;
                buttonInfo.Button.image.sprite = sprite;
                buttonInfo.ActionButtonCooldown.MaxCooldown = maxCooldown;
                buttonInfo.Tooltip.SetText(description);
            }
        }
    }
}
