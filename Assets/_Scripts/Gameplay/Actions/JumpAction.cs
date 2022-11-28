using _Scripts.Gameplay.GameplayObjects.Character;
using UnityEngine;

namespace _Scripts.Gameplay.Actions
{
    public class JumpAction: Action
    {
        private Vector3 _initialPosition;
        private float _range;
        private Vector3 _direction;
        
        public JumpAction(ServerCharacter parent, ref ActionRequestData data) : base(parent, ref data)
        {
        }

        public override void Activate()
        {
            var mousePosition = m_Parent.NetState.MousePosition.Value;

            _initialPosition = m_Parent.transform.position;
            _range = GetFinalRange(mousePosition);

            m_Parent.Movement.Jump(GetFinalPosition(mousePosition), Description.DurationSeconds);
        }

        public override bool Update()
        {
            var travelledDistance = HelpFunctions.Distance2D(_initialPosition, m_Parent.physicsWrapper.Transform.position);
            
            // stop on max range or if ServerCharacterMovement has stopped forced movement for some reason
            if (travelledDistance >= _range || ! m_Parent.Movement.IsPerformingForcedMovement())
            {
                return false;
            }
        
            return true;
        }

        public override void Cancel()
        {
            m_Parent.Movement.StopForcedMovement();
            
            // safety : set y to 0
            var finalPosition = m_Parent.transform.position;
            finalPosition.y = 0;
            m_Parent.transform.position = finalPosition;
        }
    }
}