using System;
using System.Collections;
using _Scripts.Gameplay.Actions;
using _Scripts.Gameplay.Input;
using Unity.Netcode;
using UnityEngine;
using Unity.Multiplayer.Samples.BossRoom;
using UnityEngine.Assertions;

namespace _Scripts.Gameplay.GameplayObjects.Character
{
    public class ServerCharacter : NetworkBehaviour
    {
        [SerializeField]
        NetworkCharacterState m_NetworkCharacterState;

        public NetworkCharacterState NetState => m_NetworkCharacterState;
        
        /// <summary>
        /// The Character's ActionPlayer. This is mainly exposed for use by other Actions. In particular, users are discouraged from
        /// calling 'PlayAction' directly on this, as the ServerCharacter has certain game-level checks it performs in its own wrapper.
        /// </summary>
        // public ActionPlayer RunningActions { get { return m_ActionPlayer; } }
        
        [SerializeField]
        [Tooltip("Setting negative value disables destroying object after it is killed.")]
        private float m_KilledDestroyDelaySeconds = 1.0f;

        [SerializeField]
        DamageReceiver m_DamageReceiver;
        public DamageReceiver DamageReceiver => m_DamageReceiver;

        [SerializeField]
        ServerCharacterMovement m_Movement;
        public ServerCharacterMovement Movement => m_Movement;


        [SerializeField]
        ServerCharacterRotation m_ServerRotation;
        public ServerCharacterRotation ServerRotation => m_ServerRotation;


        // [SerializeField]
        // ServerCharacterMovement m_Movement;
        // public ServerCharacterMovement Movement => m_Rotatio;

        [SerializeField]
        PhysicsWrapper m_PhysicsWrapper;
        public PhysicsWrapper physicsWrapper => m_PhysicsWrapper;
        
        [SerializeField]
        ServerAnimationHandler m_ServerAnimationHandler;
        public ServerAnimationHandler serverAnimationHandler => m_ServerAnimationHandler;

        [SerializeField] 
        ServerCharacterEffects m_ServerCharacterEffects;
        
        [SerializeField] 
        ClientInputSender m_ClientInputSender;
        public ClientInputSender ClientInputSender => m_ClientInputSender;

        private ActionPlayer m_ActionPlayer;
        private ManaFlowHandler m_ManaFlowHander;
        private int _team;
        public int Team => _team;

        private void Awake()
        {
            m_ActionPlayer = new ActionPlayer(this);
            m_ManaFlowHander = new ManaFlowHandler(this);
        }

        public override void OnNetworkSpawn()
        {
            if (!IsServer)
            {
                enabled = false;
                return;
            }
 
            NetState.DoActionEventServer += OnActionPlayRequest;
            NetState.ReqManaFlowLevelServer += OnManaFlowLevelRequest;
            NetState.CancelAbilityEventServer += OnCancelAbilityRequest;
            NetState.OnStopChargingUpServer += OnStoppedChargingUp;
            NetState.ReceivedClientDirection += OnClientMoveRequest;
            NetState.ReceivedClientRotation += OnClientRotationRequest;
            NetState.NetworkLifeState.LifeState.OnValueChanged += OnLifeStateChanged;
            m_DamageReceiver.DamageReceived += ReceiveHP;

            InitializeHitPoints();
            InitializeMana();
            InitializeTeam();
            
            GameHandler.Instance.ConnectPlayer(NetworkObjectId, _team);
        }

        public override void OnNetworkDespawn()
        {
            if (NetState)
            {
                NetState.DoActionEventServer -= OnActionPlayRequest;
                NetState.ReqManaFlowLevelServer -= OnManaFlowLevelRequest;
                NetState.CancelAbilityEventServer -= OnCancelAbilityRequest;
                NetState.ReceivedClientDirection -= OnClientMoveRequest;
                NetState.OnStopChargingUpServer -= OnStoppedChargingUp;
                NetState.NetworkLifeState.LifeState.OnValueChanged -= OnLifeStateChanged;
            }

            if (m_DamageReceiver)
            {
                m_DamageReceiver.DamageReceived -= ReceiveHP;
            }
        }

        void InitializeHitPoints()
        {
            NetState.HitPoints = NetState.CharacterClass.BaseHP.Value;
            SessionPlayerData? sessionPlayerData = SessionManager<SessionPlayerData>.Instance.GetPlayerData(OwnerClientId);
            if (sessionPlayerData is { HasCharacterSpawned: true })
            {
                NetState.HitPoints = sessionPlayerData.Value.CurrentHitPoints;
                if (NetState.HitPoints <= 0)
                {
                    NetState.LifeState = LifeState.Dead;
                }
            }
            
        }
        
        void InitializeMana()
        {
            NetState.ManaReserve = NetState.CharacterClass.ManaReserve;
            NetState.NetworkManaState.MaxManaReserve = NetState.CharacterClass.ManaReserve;
            NetState.Mana = NetState.CharacterClass.ManaPool;
            NetState.NetworkManaState.MaxManaPool = NetState.CharacterClass.ManaPool;

            SessionPlayerData? sessionPlayerData = SessionManager<SessionPlayerData>.Instance.GetPlayerData(OwnerClientId);
            if (sessionPlayerData is { HasCharacterSpawned: true })
            {
                NetState.Mana = sessionPlayerData.Value.CurrentMana;
                NetState.ManaReserve = sessionPlayerData.Value.CurrentManaReserve;
            }
        }

        void InitializeTeam()
        {
            _team = TeamHandler.Instance.GetTeam();
        }

        /// <summary>
        /// Play a sequence of actions!
        /// </summary>
        public void PlayAction(ref ActionRequestData data)
        {
            // m_ActionPlayer.PlayAction(ref action);
            m_ActionPlayer.TryCastAbility(ref data);
        }

        public void ReleaseAction(ref ActionRequestData action)
        {
            if (NetState.LifeState != LifeState.Alive) { return; }

            // m_ActionPlayer.TryReleaseAbility(action);
        }

        private void OnClientMoveRequest(Vector3 direction)
        {
            m_Movement.SetDirection(direction);
        }
        
        private void OnClientRotationRequest(Quaternion rotation)
        {
            m_ServerRotation.SetRotation(rotation);
        }

        private void OnLifeStateChanged(LifeState prevLifeState, LifeState lifeState)
        {
            if (lifeState != LifeState.Alive)
            {
                m_ActionPlayer.ClearActions(true);
                m_Movement.CancelMovement();

                StartCoroutine(KilledDestroyProcess());
            }
        }

        private void OnActionPlayRequest(ActionRequestData data)
        {
            if (! CanPerformAction()) { return; }
            
            if (data.TriggerStyle == ClientInputSender.SkillTriggerStyle.Release)
            {
                ReleaseAction(ref data);
            }
            else
            {
                PlayAction(ref data);
            }
        }

        private void OnManaFlowLevelRequest(int level)
        {
            if (m_ManaFlowHander.ManaFlowLevel == level) { return; }

            m_ManaFlowHander.SetManaFlowLevel(level);
        }

        private void OnCancelAbilityRequest()
        {
            m_ActionPlayer.CancelAbility();
        }

        IEnumerator KilledDestroyProcess()
        {
            yield return new WaitForSeconds(m_KilledDestroyDelaySeconds);

            GameHandler.Instance.PlayerDied(NetworkObjectId);

            if (NetworkObject != null)
            {
                NetworkObject.Despawn(true);
            }
        }

        /// <summary>
        /// Receive an HP change from somewhere. Could be healing or damage.
        /// </summary>
        /// <param name="inflicter">Person dishing out this damage/healing. Can be null. </param>
        /// <param name="HP">The HP to receive. Positive value is healing. Negative is damage.  </param>
        void ReceiveHP(ServerCharacter inflicter, int amount)
        {
            //to our own effects, and modify the damage or healing as appropriate. But in this game, we just take it straight.
            if (amount > 0)
            {
                Heal(amount);
            }
            else
            {
                Hit(-amount);
            }
            
            //we can't currently heal a dead character back to Alive state.
            //that's handled by a separate function.
            if (NetState.HitPoints <= 0)
            {
                NetState.LifeState = LifeState.Dead;
            }
        }

        private void Hit(int damages)
        {
            float damageMod = m_ServerCharacterEffects.PercentDamageReceived;
            damages = Mathf.RoundToInt(damages * damageMod);
                
            // check shield
            var hp_hit = 0;
            var shield_hit = damages;
            
            var shield = NetState.Shield - shield_hit;
            if (shield < 0)
            {
                // hp hit is amount that shield cant absorb
                hp_hit = Math.Abs(shield);
                shield_hit -= hp_hit;
            }
            
            NetState.Shield -= shield_hit;
            NetState.HitPoints = Mathf.Clamp(NetState.HitPoints - hp_hit, 0, NetState.CharacterClass.BaseHP.Value);
        }

        private void Heal(int heal)
        {
            float healingMod = m_ServerCharacterEffects.PercentHealingReceived;
            heal = Mathf.RoundToInt(heal * healingMod);
            
            NetState.HitPoints = Mathf.Clamp(NetState.HitPoints + heal, 0, NetState.CharacterClass.BaseHP.Value);
        }

        public void ReceiveShield(int shield)
        {
            NetState.Shield = Math.Max(NetState.Shield + shield, 0);
        }
        
        public void ReceiveMana(float mana)
        {
            var totalMana = mana + NetState.Mana;
            Assert.IsFalse(totalMana < 0,
                "Total mana pool went below 0");

            NetState.Mana = Math.Min(NetState.NetworkManaState.MaxManaPool, totalMana);
        }
        
        public void ReceiveManaReserve(float mana)
        {
            var totalMana = mana + NetState.ManaReserve;
            Assert.IsFalse(totalMana < 0,"Total mana reserve went below 0");

            NetState.ManaReserve = Mathf.Clamp(totalMana, 0, NetState.CharacterClass.ManaReserve);
        }

        private void OnStoppedChargingUp()
        {
            // m_ActionPlayer.OnGameplayActivity(Action.GameplayActivity.StoppedChargingUp);
        }

        private bool CanPerformAction()
        {
            if (NetState.LifeState != LifeState.Alive)
            {
                Debug.Log("Player is dead");
                return false;
            }
            
            if (! m_ServerCharacterEffects.CanPerformAction)
            {
                Debug.Log("Player status dont allow to perform new action");
                return false;
            }
            
            if (m_Movement.IsPerformingForcedMovement())
            {
                Debug.Log("Player is performing forced movement");
                return false;
            }
            
            if (! m_ActionPlayer.IsCurrentActionOverrideable())
            {
                Debug.Log($"Current action cant be override");
                return false;
            }

            return true;
        }

        private void Update()
        {
            m_ActionPlayer.Update();
            m_ManaFlowHander.Update();
        }
    }
}
