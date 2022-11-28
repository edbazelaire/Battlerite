using System;
using _Scripts.Gameplay.Actions;
using _Scripts.Gameplay.Configuration;
using _Scripts.Gameplay.GameplayObjects.Character;
using Unity.Multiplayer.Samples.BossRoom;
using Unity.Netcode;
using UnityEngine;
using Action = System.Action;

namespace _Scripts.Gameplay.GameplayObjects
{
    public enum LifeState
    {
        Alive,
        Dead,
    }

    /// <summary>
    /// Describes how the character's movement should be animated: as standing idle, running normally,
    /// magically slowed, sped up, etc. (Not all statuses are currently used by game content,
    /// but they are set up to be displayed correctly for future use.)
    /// </summary>
    [Serializable]
    public enum MovementStatus
    {
        Idle,         // not trying to move
        Normal,       // character is moving (normally)
        Uncontrolled, // character is being moved by e.g. a knockback -- they are not in control!
        Slowed,       // character's movement is magically hindered
        Hasted,       // character's movement is magically enhanced
        Walking,      // character should appear to be "walking" rather than normal running (e.g. for cut-scenes)
    }

    /// <summary>
    /// Contains all NetworkVariables and RPCs of a character. This component is present on both client and server objects.
    /// </summary>
    [RequireComponent(typeof(NetworkHealthState), typeof(NetworkLifeState))]
    public class NetworkCharacterState : NetworkBehaviour, ITargetable
    {
        /// Indicates how the character's movement should be depicted.
        public NetworkVariable<MovementStatus> MovementStatus { get; } = new NetworkVariable<MovementStatus>();
        
        /// Indicates character's movement direction
        public NetworkVariable<Vector3> MovementDirection { get; } = new ();
        
        /// Indicates character's movement direction
        public NetworkVariable<Quaternion> Rotation { get; } = new ();
        
        public NetworkVariable<Vector3> MousePosition { get; } = new ();

        /// <summary>
        /// Health handler of the Character 
        /// </summary>
        [SerializeField]
        NetworkHealthState m_NetworkHealthState;
        
        /// <summary>
        /// Current HP. This value is populated at startup time from CharacterClass data.
        /// </summary>
        public int HitPoints
        {
            get { return m_NetworkHealthState.HitPoints.Value; }
            set { m_NetworkHealthState.HitPoints.Value = value; }
        }
        
        /// <summary>
        /// Current Shield of the character
        /// </summary>
        public int Shield
        {
            get { return m_NetworkHealthState.Shield.Value; }
            set { m_NetworkHealthState.Shield.Value = value; }
        }

        [SerializeField]
        NetworkLifeState m_NetworkLifeState;
        public NetworkLifeState NetworkLifeState => m_NetworkLifeState;

        [SerializeField]
        NetworkControlState m_NetworkControlState;
        public NetworkControlState NetworkControlState => m_NetworkControlState;

        [SerializeField] 
        private NetworkManaState m_NetworkManaState;
        public NetworkManaState NetworkManaState => m_NetworkManaState;
        public float ManaReserve
        {
            get => m_NetworkManaState.ManaReserve.Value; 
            set => m_NetworkManaState.ManaReserve.Value = value;
        }
        public float Mana 
        {
            get => m_NetworkManaState.Mana.Value; 
            set => m_NetworkManaState.Mana.Value = value;
        }
        

        /// <summary>
        /// Current LifeState. Only Players should enter the FAINTED state.
        /// </summary>
        public LifeState LifeState
        {
            get => m_NetworkLifeState.LifeState.Value;
            set => m_NetworkLifeState.LifeState.Value = value;
        }
        
        public bool IsValidTarget => LifeState != LifeState.Dead;

        /// <summary>
        /// Returns true if the Character is currently in a state where it can play actions, false otherwise.
        /// </summary>
        public bool CanPerformActions => LifeState == LifeState.Alive;

        [SerializeField]
        CharacterClassContainer m_CharacterClassContainer;

        /// <summary>
        /// The CharacterData object associated with this Character. This is the static game data that defines its attack skills, HP, etc.
        /// </summary>
        public CharacterClass CharacterClass => m_CharacterClassContainer.CharacterClass;

        /// <summary>
        /// Character Type. This value is populated during character selection.
        /// </summary>
        public string CharacterName => m_CharacterClassContainer.CharacterClass.CharacterName;
        
        // =============================================================================================================
        // MOVEMENT SYSTEM
        /// <summary>
        /// Gets invoked when inputs are received from the client which own this networked character.
        /// </summary>
        public event Action<Vector3> ReceivedClientDirection;
        
        /// <summary>
        /// Gets invoked when inputs are received from the client which own this networked character.
        /// </summary>
        public event Action<Quaternion> ReceivedClientRotation;

        /// <summary>
        /// RPC to send inputs for this character from a client to a server.
        /// </summary>
        /// <param name="movementTarget">The position which this character should move towards.</param>
        [ServerRpc]
        public void SendCharacterDirectionServerRpc(Vector3 direction)
        {
            ReceivedClientDirection?.Invoke(direction);
        }
        
        /// <summary>
        /// Client->Server RPC that sends a request to rotate.
        /// </summary>
        /// <param name="data">Data about which action to play and its associated details. </param>
        [ServerRpc]
        public void RecvRotationServerRPC(Quaternion rotation)
        {
            ReceivedClientRotation?.Invoke(rotation);
        }
        
        /// <summary>
        /// Client->Server RPC that update mouse position.
        /// </summary>
        /// <param name="mousePosition">World position of the player's mouse. </param>
        [ServerRpc]
        public void RcvMousePositionServerRpc(Vector3 mousePosition)
        {
            MousePosition.Value = mousePosition;
        }
        
        // =============================================================================================================
        // ABILITY / STATE EFFECTS
        public event Action<string, float> AbilityEffectReceivedClient;
        public event Action<string> AbilityEffectEndsClient;
        
        public event Action<string, float, float> StateEffectReceivedClient;
        public event Action<string> StateEffectEndsClient;

        [ClientRpc]
        public void RcvAbilityEffectClientRpc(string abilityEffect, float currentDuration)
        {
            AbilityEffectReceivedClient?.Invoke(abilityEffect, currentDuration);
        }

        [ClientRpc]
        public void EndAbilityEffectClientRpc(string abilityEffect)
        {
            AbilityEffectEndsClient?.Invoke(abilityEffect);
        }

        [ClientRpc]
        public void RcvStateEffectClientRpc(string stateEffectName, float duration, float currentDuration)
        {
            StateEffectReceivedClient?.Invoke(stateEffectName, duration, currentDuration);
        }

        [ClientRpc]
        public void EndStateEffectClientRpc(string stateEffectName)
        {
            StateEffectEndsClient?.Invoke(stateEffectName);
        }

        // =============================================================================================================
        // ACTION SYSTEM
        /// <summary>
        /// This event is raised on the server when an action request arrives
        /// </summary>
        public event Action<ActionRequestData> DoActionEventServer;
        
        /// <summary>
        /// This event is raised on the client when an action is being played back.
        /// </summary>
        public event Action<ActionRequestData, ActionPlayer.ActionState> DoActionStateEventClient;

        /// <summary>
        /// This event is raised on the client when an action is being played back.
        /// </summary>
        public event Action<ActionRequestData> DoPrevisualizationEventClient;

        /// <summary>
        /// This event is raised on the server when mana flow level is asked to be changed
        /// </summary>
        public event Action<int> ReqManaFlowLevelServer;
        
        /// <summary>
        /// This event is raised in the server when the client request an ability cancel
        /// </summary>
        public event Action CancelAbilityEventServer;
        
        /// <summary>
        /// This event is raised on the client when a cancel when through
        /// </summary>
        public event Action<ActionRequestData> CancelAbilityEventClient;
        
        /// <summary>
        /// This event is raised on the client when a cancel when through
        /// </summary>
        public event Action CancelPrevisualizationEventClient;
        
        /// <summary>
        /// This event is raised on the client when the active action FXs need to be cancelled (e.g. when the character has been stunned)
        /// </summary>
        public event Action CancelAllActionsEventClient;
        
        /// <summary>
        /// This event is raised on the client when active action FXs of a certain type need to be cancelled (e.g. when the Stealth action ends)
        /// </summary>
        public event Action<string> CancelActionsByNameEventClient;
        
        /// <summary>
        /// Client->Server RPC that sends a request to play an action.
        /// </summary>
        /// <param name="data">Data about which action to play and its associated details. </param>
        [ServerRpc]
        public void RecvDoActionServerRPC(ActionRequestData data)
        {
            DoActionEventServer?.Invoke(data);
        }
        
        /// <summary>
        /// Server to Client RPC that broadcasts this action play to all clients with its current state.
        /// </summary>
        /// <param name="data"> Data about which action to play and its associated details. </param>
        [ClientRpc]
        public void RecvActionStateClientRPC(ActionRequestData data, ActionPlayer.ActionState state)
        {
            DoActionStateEventClient?.Invoke(data, state);
        }
        
        /// <summary>
        /// Server to Client RPC that broadcasts this action play to all clients.
        /// </summary>
        /// <param name="data"> Data about which action to play and its associated details. </param>
        [ClientRpc]
        public void RecvDoPrevisualizationClientRPC(ActionRequestData data)
        {
            DoPrevisualizationEventClient?.Invoke(data);
        }
        
        /// <summary>
        /// Client->Server RPC that sends a request to change mana flow level.
        /// </summary>
        /// <param name="data"> Data about which action to play and its associated details. </param>
        [ServerRpc]
        public void RequestManaFlowLevelServerRpc(int level)
        {
            ReqManaFlowLevelServer?.Invoke(level);
        }

        /// <summary>
        /// Client->Server RPC that sends a request to cancel an ability.
        /// </summary>
        [ServerRpc]
        public void CancelAbilityServerRPC()
        {
            CancelAbilityEventServer?.Invoke();
        }
        
        /// <summary>
        /// Server->Client RPC that sends a request to cancel an ability.
        /// </summary>
        [ClientRpc]
        public void CancelAbilityClientRPC(ActionRequestData data)
        {
            CancelAbilityEventClient?.Invoke(data);
        }
        
        /// <summary>
        /// Server->Client RPC that sends a request to cancel an ability.
        /// </summary>
        [ClientRpc]
        public void CancelPrevisualizationClientRPC()
        {
            CancelPrevisualizationEventClient?.Invoke();
        }

        [ClientRpc]
        public void RecvCancelAllActionsClientRpc()
        {
            CancelAllActionsEventClient?.Invoke();
        }
        
        [ClientRpc]
        public void RecvCancelActionsByTypeClientRpc(string action)
        {
            CancelActionsByNameEventClient?.Invoke(action);
        }

        // UTILITY AND SPECIAL-PURPOSE RPCs
        
        /// <summary>
        /// Called on server when the character's client decides they have stopped "charging up" an attack.
        /// </summary>
        public event Action OnStopChargingUpServer;
        
        /// <summary>
        /// Called on all clients when this character has stopped "charging up" an attack.
        /// Provides a value between 0 and 1 inclusive which indicates how "charged up" the attack ended up being.
        /// </summary>
        public event Action<float> OnStopChargingUpClient;
        
        [ServerRpc]
        public void RecvStopChargingUpServerRpc()
        {
            OnStopChargingUpServer?.Invoke();
        }
        
        [ClientRpc]
        public void RecvStopChargingUpClientRpc(float percentCharged)
        {
            OnStopChargingUpClient?.Invoke(percentCharged);
        }
    }
}
