using _Scripts.Gameplay.GameplayObjects.Character;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

namespace _Scripts.Gameplay.Actions
{
    public class DashAction: Action
    {
        private Vector3 _initialPosition;
        private float _range;
        private Vector3 _direction;
        
        public DashAction(ServerCharacter parent, ref ActionRequestData data) : base(parent, ref data)
        {
        }

        public override void Activate()
        {
            var mousePosition = m_Parent.NetState.MousePosition.Value;

            _initialPosition = m_Parent.transform.position;
            _range = GetFinalRange(mousePosition);
            _direction = mousePosition - _initialPosition;
            _direction.y = 0;
            
            m_Parent.Movement.Dash(_direction, Description.Speed);
        }

        public override bool Update()
        {
            var travelledDistance = HelpFunctions.Distance2D(_initialPosition, m_Parent.physicsWrapper.Transform.position);
            if (travelledDistance >= _range)
            {
                return false;
            }
        
            return true;
        }

        public override void Cancel()
        {
            m_Parent.Movement.StopForcedMovement();
        }

    }
}