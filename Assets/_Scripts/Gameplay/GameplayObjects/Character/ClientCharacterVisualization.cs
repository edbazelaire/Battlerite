using System;
using System.Collections.Generic;
using _Scripts.Gameplay.Actions;
using _Scripts.Gameplay.Configuration;
using _Scripts.Gameplay.GameplayObjects.RuntimeDataContainers;
using _Scripts.Utils;
using Unity.Multiplayer.Samples.BossRoom;
using Unity.Multiplayer.Samples.BossRoom.Client;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;
using ClientInputSender = _Scripts.Gameplay.Input.ClientInputSender;

namespace _Scripts.Gameplay.GameplayObjects.Character
{
    /// <summary>
    /// <see cref="ClientCharacterVisualization"/> is responsible for displaying a character on the client's screen based on state information sent by the server.
    /// </summary>
    public class ClientCharacterVisualization : NetworkBehaviour
    {
        [SerializeField]
        Animator m_ClientVisualsAnimator;

        CharacterSwap m_CharacterSwapper;

        [SerializeField]
        VisualizationConfiguration m_VisualizationConfiguration;

        /// <summary>
        /// Returns a reference to the active Animator for this visualization
        /// </summary>
        public Animator OurAnimator => m_ClientVisualsAnimator;
        
        PhysicsWrapper m_PhysicsWrapper;
        public PhysicsWrapper PhysicsWrapper => m_PhysicsWrapper;

        public Camera CharacterCamera { get; private set; }
        public bool CanPerformActions => NetState.CanPerformActions;

        public NetworkCharacterState NetState { get; private set; }

        ActionVisualization m_ActionViz;

        PositionLerper m_PositionLerper;

        RotationLerper m_RotationLerper;

        // this value suffices for both positional and rotational interpolations; one may have a constant value for each
        const float k_LerpTime = 0.08f;

        Vector3 m_LerpedPosition;

        Quaternion m_LerpedRotation;

        bool m_IsHost;

        float m_CurrentSpeed;

        private Vector3 m_CurrentDirection;
        
        private float m_CastSpeed = 1f;

        void Awake()
        {
            enabled = false;
        }

        public override void OnNetworkSpawn()
        {
            if (!IsClient || transform.parent == null)
            {
                return;
            }

            enabled = true;

            m_IsHost = IsHost;

            m_ActionViz = new ActionVisualization(this);

            NetState = GetComponentInParent<NetworkCharacterState>();

            m_PhysicsWrapper = NetState.GetComponent<PhysicsWrapper>();

            NetState.DoActionStateEventClient += PerformActionFX;
            NetState.DoPrevisualizationEventClient += OnPrevisualizationReceived;
            NetState.CancelAbilityEventClient += OnCancelAbility;
            NetState.CancelPrevisualizationEventClient += OnPrevisualizationEnd;
            NetState.CancelAllActionsEventClient += CancelAllActionFXs;
            NetState.CancelActionsByNameEventClient += CancelActionFXByName;
            NetState.OnStopChargingUpClient += OnStoppedChargingUp;
            NetState.MovementStatus.OnValueChanged += OnMovementStatusChanged;
            OnMovementStatusChanged(MovementStatus.Normal, NetState.MovementStatus.Value);

            // sync our visualization position & rotation to the most up to date version received from server
            transform.SetPositionAndRotation(m_PhysicsWrapper.Transform.position, m_PhysicsWrapper.Transform.rotation);
            m_LerpedPosition = transform.position;
            m_LerpedRotation = transform.rotation;

            // similarly, initialize start position and rotation for smooth lerping purposes
            m_PositionLerper = new PositionLerper(m_PhysicsWrapper.Transform.position, k_LerpTime);
            m_RotationLerper = new RotationLerper(m_PhysicsWrapper.Transform.rotation, k_LerpTime);

            name = "AvatarGraphics" + NetState.OwnerClientId;

            if (NetState.TryGetComponent(out ClientAvatarGuidHandler clientAvatarGuidHandler))
            {
                m_ClientVisualsAnimator = clientAvatarGuidHandler.graphicsAnimator;
            }

            m_CharacterSwapper = GetComponentInChildren<CharacterSwap>();

            // ...and visualize the current char-select value that we know about
            SetAppearanceSwap();

            if (NetState.IsOwner)
            {
                // setting player's camera
                CharacterCamera = Camera.main;
                Assert.IsNotNull(CharacterCamera, 
                    "Unable to find MainCamera");
                
                var cameraController = CharacterCamera.GetComponent<CameraController>();
                cameraController.enabled = true;
                cameraController.SetPlayerToFollow(gameObject);

                if (NetState.TryGetComponent(out ClientInputSender inputSender))
                {
                    inputSender.ActionInputEvent += OnActionInput;
                }
            }
        }

        public override void OnNetworkDespawn()
        {
            if (NetState)
            {
                NetState.DoActionStateEventClient -= PerformActionFX;
                NetState.CancelAllActionsEventClient -= CancelAllActionFXs;
                NetState.CancelActionsByNameEventClient -= CancelActionFXByName;
                NetState.OnStopChargingUpClient -= OnStoppedChargingUp;

                if (NetState.TryGetComponent(out ClientInputSender sender))
                {
                    sender.ActionInputEvent -= OnActionInput;
                }
            }

            enabled = false;
        }

        void OnActionInput(ActionRequestData data)
        {
            m_ActionViz.AnticipateAction(ref data);
            
            // get casting speed of the action
            if (!GameDataSource.Instance.ActionDataByName.TryGetValue(data.ActionName, out var actionDesc))
            {
                return;
            }
            var exitTime = 0.5f; 
            m_CastSpeed = exitTime * 1  / Mathf.Max(actionDesc.ExecTimeSeconds, 0.001f);
        }

        void PerformActionFX(ActionRequestData data, ActionPlayer.ActionState state)
        {
            // Cancel current ActionFX of this Action that stop at this state
            m_ActionViz.EndActionsAtState(ref data, state);
            
            // Play FX actions that are at this ActionState for this Action
            m_ActionViz.StartActionsAtState(ref data, state);
        }

        void OnPrevisualizationReceived(ActionRequestData data)
        {
            // only owner can se previsualization
            if (!IsOwner) { return; }
            
            m_ActionViz.SetPrevisualization(ref data);
        }

        void OnCancelAbility(ActionRequestData data)
        {
            m_ActionViz.CancelAction(ref data);
        }

        void OnPrevisualizationEnd()
        {
            m_ActionViz.CancelPrevisualization();
        }

        void CancelAllActionFXs()
        {
            m_ActionViz.CancelAllActions();
        }

        void CancelActionFXByName(string actionName)
        {
            m_ActionViz.CancelAllActionsOfType(actionName);
        }

        void OnStoppedChargingUp(float finalChargeUpPercentage)
        {
            m_ActionViz.OnStoppedChargingUp(finalChargeUpPercentage);
        }

        void OnStealthyChanged(bool oldValue, bool newValue)
        {
            SetAppearanceSwap();
        }

        void SetAppearanceSwap()
        {
            if (m_CharacterSwapper)
            {
                var specialMaterialMode = CharacterSwap.SpecialMaterialMode.None;
                // if (NetState.IsStealthy.Value)
                // {
                //     if (NetState.IsOwner)
                //     {
                //         specialMaterialMode = CharacterSwap.SpecialMaterialMode.StealthySelf;
                //     }
                //     else
                //     {
                //         specialMaterialMode = CharacterSwap.SpecialMaterialMode.StealthyOther;
                //     }
                // }

                m_CharacterSwapper.SwapToModel(specialMaterialMode);
            }
        }

        /// <summary>
        /// Returns the value we should set the Animator's "Speed" variable, given current gameplay conditions.
        /// </summary>
        float GetVisualMovementSpeed(MovementStatus movementStatus)
        {
            if (NetState.NetworkLifeState.LifeState.Value != LifeState.Alive)
            {
                return m_VisualizationConfiguration.SpeedDead;
            }

            switch (movementStatus)
            {
                case MovementStatus.Idle:
                    return m_VisualizationConfiguration.SpeedIdle;
                case MovementStatus.Normal:
                    return m_VisualizationConfiguration.SpeedNormal;
                case MovementStatus.Uncontrolled:
                    return m_VisualizationConfiguration.SpeedUncontrolled;
                case MovementStatus.Slowed:
                    return m_VisualizationConfiguration.SpeedSlowed;
                case MovementStatus.Hasted:
                    return m_VisualizationConfiguration.SpeedHasted;
                case MovementStatus.Walking:
                    return m_VisualizationConfiguration.SpeedWalking;
                default:
                    throw new Exception($"Unknown MovementStatus {movementStatus}");
            }
        }

        void OnMovementStatusChanged(MovementStatus previousValue, MovementStatus newValue)
        {
            m_CurrentSpeed = GetVisualMovementSpeed(newValue);
        }

        void Update()
        {
            // On the host, Characters are translated via ServerCharacterMovement's FixedUpdate method. To ensure that
            // the game camera tracks a GameObject moving in the Update loop and therefore eliminate any camera jitter,
            // this graphics GameObject's position is smoothed over time on the host. Clients do not need to perform any
            // positional smoothing since NetworkTransform will interpolate position updates on the root GameObject.
            if (m_IsHost)
            {
                // Note: a cached position (m_LerpedPosition) and rotation (m_LerpedRotation) are created and used as
                // the starting point for each interpolation since the root's position and rotation are modified in
                // FixedUpdate, thus altering this transform (being a child) in the process.
                m_LerpedPosition = m_PositionLerper.LerpPosition(m_LerpedPosition,
                    m_PhysicsWrapper.Transform.position);
                m_LerpedRotation = m_RotationLerper.LerpRotation(m_LerpedRotation,
                    m_PhysicsWrapper.Transform.rotation);
                transform.SetPositionAndRotation(m_LerpedPosition, m_LerpedRotation);
            }

            if (m_ClientVisualsAnimator)
            {
                // set Animator PlayerSpeed
                OurAnimator.SetFloat(m_VisualizationConfiguration.SpeedVariableID, m_CurrentSpeed);
                
                // set Animator RadDirection between movement direction and rotation
                var direction = NetState.MovementDirection.Value;
                var angleDirection = Vector3.Angle(new Vector3(0, 0, 1), direction);
                var angleDiff = angleDirection - transform.eulerAngles.y;
                if (direction.x < 0)
                {
                    angleDiff = transform.eulerAngles.y + angleDirection;
                }
                var angleRad = angleDiff * Mathf.Deg2Rad;
                float radDirection = MathF.Abs(Mathf.Cos(angleRad / 2));
                OurAnimator.SetFloat(m_VisualizationConfiguration.RadDirectionVariableID, radDirection);
                
                // Set Casting Speed
                OurAnimator.SetFloat(m_VisualizationConfiguration.CastSpeedVariableID, m_CastSpeed);
            }

            m_ActionViz.Update();
        }

        void OnAnimEvent(string id)
        {
            //if you are trying to figure out who calls this method, it's "magic". The Unity Animation Event system takes method names as strings,
            //and calls a method of the same name on a component on the same GameObject as the Animator. See the "attack1" Animation Clip as one
            //example of where this is configured.

            m_ActionViz.OnAnimEvent(id);
        }

        public bool IsAnimating()
        {
            if (OurAnimator.GetFloat(m_VisualizationConfiguration.SpeedVariableID) > 0.0) { return true; }

            for (int i = 0; i < OurAnimator.layerCount; i++)
            {
                if (OurAnimator.GetCurrentAnimatorStateInfo(i).tagHash != m_VisualizationConfiguration.BaseNodeTagID)
                {
                    //we are in an active node, not the default "nothing" node.
                    return true;
                }
            }

            return false;
        }

        private List<ActionDescription.ActionFXDescription> GetActionFXsAtState(ActionDescription description, ActionPlayer.ActionState state)
        {
            List<ActionDescription.ActionFXDescription> returns = new();
            
            foreach (var spawn in description.Spawns)
            {
                if (spawn.ActionStateStart == state)
                {
                    returns.Add(spawn);
                }
            }

            return returns;
        }
    }
}
