using System;
using _Scripts.Gameplay.Actions;
using _Scripts.Gameplay.Configuration;
using _Scripts.Gameplay.GameplayObjects;
using _Scripts.Gameplay.GameplayObjects.Character;
using _Scripts.Gameplay.GameplayObjects.RuntimeDataContainers;
using _Scripts.Utils;
using Unity.Multiplayer.Samples.BossRoom;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;
using Action = System.Action;

namespace _Scripts.Gameplay.Input
{
    /// <summary>
    /// Captures inputs for a character on a client and sends them to the server.
    /// </summary>
    [RequireComponent(typeof(NetworkCharacterState))]
    public class ClientInputSender : NetworkBehaviour
    {
        float m_LastSentMove;

        NetworkCharacterState m_NetworkCharacter;
        
        /// <summary>
        /// This event fires at the time when an action request is sent to the server.
        /// </summary>
        public Action<ActionRequestData> ActionInputEvent;
        
        /// <summary>
        /// Request Cancel of current action
        /// </summary>
        public Action CancelAbilityEvent;

        // direction of the player movement
        private Vector3 _direction;

        /// <summary>
        /// This describes how a skill was requested. Skills requested via mouse click will do raycasts to determine their target; skills requested
        /// in other matters will use the stateful target stored in NetworkCharacterState.
        /// </summary>
        public enum SkillTriggerStyle
        {
            None,        //no skill was triggered.
            Down,    //skill was triggered via a Keyboard press, implying target should be taken from the active target.
            Release, //represents a released key.
            UI,          //skill was triggered from the UI, and similar to Keyboard, target should be inferred from the active target.
            UIRelease,   //represents letting go of the mouse-button on a UI button
        }

        bool IsReleaseStyle(SkillTriggerStyle style)
        {
            return style == SkillTriggerStyle.Release || style == SkillTriggerStyle.UIRelease;
        }

        /// <summary>
        /// This struct essentially relays the call params of RequestAction to FixedUpdate. Recall that we may need to do raycasts
        /// as part of doing the action, and raycasts done outside of FixedUpdate can give inconsistent results (otherwise we would
        /// just expose PerformAction as a public method, and let it be called in whatever scoped it liked.
        /// </summary>
        /// <remarks>
        /// Reference: https://answers.unity.com/questions/1141633/why-does-fixedupdate-work-when-update-doesnt.html
        /// </remarks>
        struct ActionRequest
        {
            public SkillTriggerStyle TriggerStyle;
            public string RequestedAction;
            public ulong TargetId;
        }

        /// <summary>
        /// List of ActionRequests that have been received since the last FixedUpdate ran. This is a static array, to avoid allocs, and
        /// because we don't really want to let this list grow indefinitely.
        /// </summary>
        readonly ActionRequest[] m_ActionRequests = new ActionRequest[5];

        /// <summary>
        /// Number of ActionRequests that have been queued since the last FixedUpdate.
        /// </summary>
        int m_ActionRequestCount;

        bool m_MoveRequest;

        Camera m_MainCamera;
        
        [SerializeField]
        CharacterClassContainer m_CharacterClassContainer;

        /// <summary>
        /// Convenience getter that returns our CharacterData
        /// </summary>
        CharacterClass CharacterData => m_CharacterClassContainer.CharacterClass;
        
        public override void OnNetworkSpawn()
        {
            if (!IsClient || !IsOwner)
            {
                enabled = false;
            }
        }

        void Awake()
        {
            m_NetworkCharacter = GetComponent<NetworkCharacterState>();
            m_MainCamera = Camera.main;
        }
        

        void SendInput(ActionRequestData action)
        {
            ActionInputEvent?.Invoke(action);
            m_NetworkCharacter.RecvDoActionServerRPC(action);
        }

        void FixedUpdate()
        {
            //play all ActionRequests, in FIFO order.
            for (int i = 0; i < m_ActionRequestCount; ++i)
            {
                var data = new ActionRequestData();
                var actionType = m_ActionRequests[i].RequestedAction;
                var triggerStyle = m_ActionRequests[i].TriggerStyle;
                PopulateSkillRequest(actionType, triggerStyle, ref data);
                SendInput(data);
            }

            m_ActionRequestCount = 0;
            
            if (_direction != Vector3.zero)
            {
                // send direction movement to the server
                _direction.y = 0; 
                m_NetworkCharacter.SendCharacterDirectionServerRpc(_direction);

                // reset direction
                _direction = Vector3.zero;
            }
        }

        /// <summary>
        /// Populates the ActionRequestData with additional information. The TargetIds of the action should already be set before calling this.
        /// </summary>
        /// <param name="hitPoint">The point in world space where the click ray hit the target.</param>
        /// <param name="action">The action to perform (will be stamped on the resultData)</param>
        /// <param name="triggerStyle">type of trigger action (key down or key release)</param>
        /// <param name="resultData">The ActionRequestData to be filled out with additional information.</param>
        void PopulateSkillRequest(string actionName, SkillTriggerStyle triggerStyle, ref ActionRequestData resultData)
        {
            resultData.ActionName = actionName;
            resultData.TriggerStyle = triggerStyle;
        }

        /// <summary>
        /// Request an action be performed. This will occur on the next FixedUpdate.
        /// </summary>
        /// <param name="actionName"> The action you'd like to perform. </param>
        /// <param name="triggerStyle"> What input style triggered this action. </param>
        /// <param name="targetId"> NetworkObjectId of target. </param>
        public void RequestAction(string actionName, SkillTriggerStyle triggerStyle)
        {
            // do not populate an action request unless said action is valid
            if (actionName == "")
            {
                return;
            }

            Assert.IsTrue(GameDataSource.Instance.ActionDataByName.ContainsKey(actionName),
                $"Action {actionName} must be part of ActionData dictionary!");

            if (m_ActionRequestCount < m_ActionRequests.Length)
            {
                m_ActionRequests[m_ActionRequestCount].RequestedAction = actionName;
                m_ActionRequests[m_ActionRequestCount].TriggerStyle = triggerStyle;
                m_ActionRequestCount++;
            }
        }

        public void RequestCancelAction()
        {
            CancelAbilityEvent?.Invoke();
            m_NetworkCharacter.CancelAbilityServerRPC();
        }
        
        /// <summary>
        /// Request a movement be performed. This will occur on the next FixedUpdate.
        /// </summary>
        /// <param name="direction"> The action you'd like to perform. </param>
        public void RequestMovement(Vector3 direction) {
            _direction = direction;
        }

        private void RequestManaFlowLevel(int level)
        {
            m_NetworkCharacter.RequestManaFlowLevelServerRpc(level);
        }
        
        public Vector3 GetMousePosition()
        {
            if (Physics.Raycast(m_MainCamera.ScreenPointToRay(UnityEngine.Input.mousePosition), out var hit, Mathf.Infinity))
            {
                return hit.point;
            }
            
            return default;
        }

        void Update() {
            // =========================================================================================
            // MOVEMENT DIRECTION
            var direction = Vector3.zero; 
            if (UnityEngine.Input.GetKey(KeyBindingPrefs.MoveUp)) {
                direction += new Vector3(-1, 0, 0);
            }

            if (UnityEngine.Input.GetKey(KeyBindingPrefs.MoveDown)) {
                direction += new Vector3(1, 0, 0);
            }
            
            if (UnityEngine.Input.GetKey(KeyBindingPrefs.MoveRight)) {
                direction += new Vector3(0, 0, 1);
            }
            
            if (UnityEngine.Input.GetKey(KeyBindingPrefs.MoveLeft)) {
                direction += new Vector3(0, 0, -1);
            }
            
            if (direction != Vector3.zero) {
                RequestMovement(direction);
            }
            
            // =========================================================================================
            // ABILITIES
            for (var i = 0; i < CharacterData.Abilities.Length; i++)
            {
                var inputKey = KeyBindingPrefs.AbilitiesKeys[i];
                if (UnityEngine.Input.GetKeyDown(inputKey))
                {
                    RequestAction(CharacterData.Abilities[i].name, SkillTriggerStyle.Down);
                } 
                // else if (UnityEngine.Input.GetKeyUp(inputKey))
                // {
                //     RequestAction(CharacterData.Abilities[i], SkillTriggerStyle.Release);
                // }
            }

            // --------------------- CANCEL
            if (UnityEngine.Input.GetKeyDown(KeyBindingPrefs.CancelAbility1) || UnityEngine.Input.GetKeyDown(KeyBindingPrefs.CancelAbility2))
            {
                RequestCancelAction();
            }
            
            // ---------------------- HEX ABILITIES
            // if (UnityEngine.Input.GetKeyDown(KeyBindingPrefs.HexAbilities))
            // {
            //     RequestAction(CharacterData.Skill1, SkillTriggerStyle.Down);
            // }
            // else if (UnityEngine.Input.GetKeyUp(KeyBindingPrefs.HexAbilities))
            // {
            //     RequestAction(CharacterData.Skill1, SkillTriggerStyle.Release);
            // }
            
            // =========================================================================================
            // MANA FLOW LEVEL
            for (var i=0; i < KeyBindingPrefs.ManaFlowLevelKeys.Length; i++)
            {
                if (UnityEngine.Input.GetKeyDown(KeyBindingPrefs.ManaFlowLevelKeys[i]))
                {
                    RequestManaFlowLevel(i+1);
                }
            }
        }
    }
}
