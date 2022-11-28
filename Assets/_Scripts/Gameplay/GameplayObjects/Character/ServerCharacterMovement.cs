using System;
using System.Numerics;
using _Scripts.Gameplay.Configuration;
using _Scripts.Gameplay.GameplayObjects.Abilities;
using _Scripts.Gameplay.GameplayObjects.RuntimeDataContainers;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using Vector3 = UnityEngine.Vector3;

namespace _Scripts.Gameplay.GameplayObjects.Character
{
    public enum MovementState
    {
        Idle = 0,           // player is not doing anything
        Moving = 1,         // player is moving normally
        Charging = 2,       // player is charging to a location
        Knockback = 3,      // player is getting knocked back from a location
        Jump = 4,           // player is jumping to a position
        Cancel = 5,         // player movement is cancelled by an ability
        Dash = 6            // player is dashing to a position
    }

    /// <summary>
    /// Component responsible for moving a character on the server side based on inputs.
    /// </summary>
    /*[RequireComponent(typeof(NetworkCharacterState), typeof(NavMeshAgent), typeof(ServerCharacter)), RequireComponent(typeof(Rigidbody))]*/
    public class ServerCharacterMovement : NetworkBehaviour
    {
        [SerializeField]
        private Rigidbody m_Rigidbody;

        [SerializeField]
        private NetworkCharacterState m_NetworkCharacterState;

        [SerializeField]
        private ServerCharacter m_ServerCharacter;
        
        // movement status
        private MovementState _movementState;
        private MovementStatus _previousState;
        
        // bonus movement speed that impacts our final movement speed
        private float _bonusSpeed = 1;

        // movement direction
        private Vector3 _direction;
        private Vector3 _previousDirection;

        // when we are performing a "forced" movement, we use these additional variables
        private float _forcedSpeed;                         // movement speed during forced movement
        private Vector3 _forcedDirection;                   // direction of the forced movement
        private float _forcedMovementMaxDistance;           // max distance allowed during forced movement
        private Vector3 _forcedMovementInitialPosition;     // initial position when the forced movement started

        // jump forced movement
        private float JUMP_HIGHT = 10f;
        private Vector3 _forcedMovementFinalPosition;
        private float _forceMovementDuration;
        private float _forcedMovementProgression;
        private Vector3 _jumpControlPoint;

        void Awake()
        {
            // disable this NetworkBehavior until it is spawned
            enabled = false;
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                // Only enable server component on servers
                enabled = true;
            }
        }


        /// <summary>
        /// Returns true if the current movement-mode is unabortable (e.g. a knockback effect)
        /// </summary>
        /// <returns></returns>
        public bool IsPerformingForcedMovement()
        {
            return _movementState is MovementState.Knockback 
                or MovementState.Charging 
                or MovementState.Dash 
                or MovementState.Jump 
                or MovementState.Cancel;
        }

        /// <summary>
        /// Returns true if the character is actively moving, false otherwise.
        /// </summary>
        /// <returns></returns>
        public bool IsMoving()
        {
            return _movementState != MovementState.Idle;
        }

        /// <summary>
        /// Prevent player from moving
        /// </summary>
        public void CancelMovement()
        {
            _movementState = MovementState.Cancel;
        }

        /// <summary>
        /// Allow player to move 
        /// </summary>
        public void EnableMovement()
        {
            if (_movementState == MovementState.Cancel)
            {
                _movementState = MovementState.Idle;
            }
        }
        
        private void FixedUpdate()
        {
            PerformMovement();
            
            // Set new movement state
            var currentState = GetMovementStatus(_movementState);
            if (_previousState != currentState)
            {
                m_NetworkCharacterState.MovementStatus.Value = currentState;
                _previousState = currentState;
            }
            
            // Set new movement direction
            if (_previousDirection != _direction)
            {
                m_NetworkCharacterState.MovementDirection.Value = _direction;
                _previousDirection = _direction;
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                // Disable server components when despawning
                enabled = false;
            }
        }

        private void PerformMovement() {
            switch (_movementState)
            {
                case(MovementState.Knockback):
                case(MovementState.Dash):
                    transform.position += _forcedDirection.normalized * (_forcedSpeed * Time.fixedDeltaTime);

                    // check if movement has reach maximum distance (if a max distance was required)
                    if (_forcedMovementMaxDistance > 0 &&
                        HelpFunctions.Distance2D(_forcedMovementInitialPosition, transform.position) >= _forcedMovementMaxDistance)
                    {
                        StopForcedMovement();
                    }
                    
                    break;
                    
                case(MovementState.Jump):
                    _forcedMovementProgression += 1.0f * Time.fixedDeltaTime / _forceMovementDuration;

                    Vector3 m1 = Vector3.Lerp( _forcedMovementInitialPosition, _jumpControlPoint, _forcedMovementProgression );
                    Vector3 m2 = Vector3.Lerp( _jumpControlPoint, _forcedMovementFinalPosition, _forcedMovementProgression );
                    transform.position = Vector3.Lerp(m1, m2, _forcedMovementProgression);

                    if (_forcedMovementProgression >= 1)
                    {
                        print("Stopped by ServerCharacterMovement");
                        StopForcedMovement();
                    }
                    
                    break;
                    
                case(MovementState.Charging):
                    throw new NotImplementedException();
                    
                case(MovementState.Cancel):
                    return;
                
                default:
                    if (_direction == Vector3.zero)
                    {
                        _movementState = MovementState.Idle;
                        return;
                    }
                
                    _movementState = MovementState.Moving;
                    var position = transform.position + _direction.normalized * (GetBaseMovementSpeed() * _bonusSpeed * Time.fixedDeltaTime);
                    position.y = 0;
                    transform.position = position;
                    
                    break;
            }

            // After moving adjust the position of the dynamic rigidbody.
            m_Rigidbody.position = transform.position;
            
            // reset direction (will be set again by events)
            _direction = Vector3.zero;
        }

        public void Dash(Vector3 direction, float speed)
        {
            _movementState = MovementState.Dash;
            _forcedDirection = direction;
            _forcedSpeed = speed;
        }

        public void Jump(Vector3 position, float duration)
        {
            _movementState = MovementState.Jump;
            _forcedMovementInitialPosition = transform.position;
            _forcedMovementFinalPosition = position;
            _forceMovementDuration = duration;
            _jumpControlPoint = _forcedMovementInitialPosition + (_forcedMovementFinalPosition -_forcedMovementInitialPosition) / 2 + Vector3.up * JUMP_HIGHT;

            // set max distance to know when to stop the jump
            _forcedMovementMaxDistance = HelpFunctions.Distance2D(transform.position, position);
            EnableCollision(false);
        }
        
        public void StartKnockback(Vector3 knocker, float speed, float distance)
        {
            _movementState = MovementState.Knockback;
            _forcedDirection = transform.position - knocker;
            _forcedSpeed = speed;
            _forcedMovementMaxDistance = distance;
        }
        
        /// <summary>
        /// Reset all forced movement parameters
        /// </summary>
        public void StopForcedMovement()
        {
            _movementState = MovementState.Idle;
            _forcedDirection = Vector3.zero;
            _forcedSpeed = 0;
            _forcedMovementInitialPosition = Vector3.zero;
            _forcedMovementMaxDistance = 0;
            
            _forcedMovementFinalPosition = Vector3.zero;
            _jumpControlPoint = Vector3.zero;
            _forceMovementDuration = 0;
            _forcedMovementProgression = 0;

            EnableCollision(true);
        }
        
        private float GetBaseMovementSpeed()
        {
            CharacterClass characterClass = GameDataSource.Instance.CharacterDataByType[m_ServerCharacter.NetState.CharacterName];
            Assert.IsNotNull(characterClass, $"No CharacterClass data for character type {m_ServerCharacter.NetState.CharacterName}");
            return characterClass.Speed;
        }

        public void AddBonusSpeed(float bonusSpeed)
        {
            _bonusSpeed *= bonusSpeed;
        }

        public void RemoveBonusSpeed(float bonusSpeed)
        {
            _bonusSpeed /= bonusSpeed;
        }
        
        /// <summary>
        /// Sets a movement target. We will path to this position, avoiding static obstacles.
        /// </summary>
        /// <param name="direction">Direction to follow</param>
        public void SetDirection(Vector3 direction)
        {
            if (!CanMove()) { return; }    
            _direction = direction;
        }
        
        /// <summary>
        /// Determines the appropriate MovementStatus for the character. The
        /// MovementStatus is used by the client code when animating the character.
        /// </summary>
        private MovementStatus GetMovementStatus(MovementState movementState)
        {
            switch (movementState)
            {
                case MovementState.Idle:
                    return MovementStatus.Idle;
                case MovementState.Knockback:
                case MovementState.Dash:
                case MovementState.Jump:
                case MovementState.Cancel:
                    return MovementStatus.Uncontrolled;
                default:
                    return MovementStatus.Normal;
            }
        }

        private bool CanMove()
        {
            return !IsPerformingForcedMovement()
               && m_NetworkCharacterState.NetworkControlState.StateEffectType.Value != StateEffectType.Stun
               && m_NetworkCharacterState.NetworkControlState.StateEffectType.Value != StateEffectType.AirBorn
               && m_NetworkCharacterState.NetworkControlState.StateEffectType.Value != StateEffectType.Petrify;
        }

        public void EnableCollision(bool enable)
        {
            m_Rigidbody.detectCollisions = enable;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (IsPerformingForcedMovement())
            {
                StopForcedMovement();
            }
        }
    }
}
