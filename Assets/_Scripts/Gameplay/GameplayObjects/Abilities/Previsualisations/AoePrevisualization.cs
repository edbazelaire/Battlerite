using _Scripts.Gameplay.GameplayObjects.Character;
using UnityEngine;

namespace _Scripts.Gameplay.GameplayObjects.Abilities.Previsualisations
{
    public class AoePrevisualization: AbilityPrevisualization
    {
        [SerializeField] 
        private SphereCollider m_Collider;

        public override void Initialize(ClientCharacterVisualization caster, ActionRequestData data)
        {
            base.Initialize(caster, data);
            
            transform.localScale = Vector3.one * (Description.Radius / m_Collider.radius);
        }

        protected override void Update()
        {
            transform.position = GetFinalPosition();
        }
    }
}