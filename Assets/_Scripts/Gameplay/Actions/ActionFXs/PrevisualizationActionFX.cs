using _Scripts.Gameplay.Configuration;
using _Scripts.Gameplay.GameplayObjects.Abilities.Previsualisations;
using _Scripts.Gameplay.GameplayObjects.Character;
using UnityEngine;

namespace _Scripts.Gameplay.Actions.ActionFXs
{
    public class PrevisualizationActionFX: ActionFX
    {
        private AbilityPrevisualization _instance;
        
        public PrevisualizationActionFX(ref ActionRequestData data, ClientCharacterVisualization parent, ActionDescription.ActionFXDescription actionFXDescription) : base(ref data, parent, actionFXDescription)
        {
        }

        public void Activate()
        {
            _instance = GameObject.Instantiate(Description.PrevisualisationPrefab.gameObject).GetComponent<AbilityPrevisualization>();
            _instance.Initialize(m_Parent, Data);
        }

        public override bool Update()
        {
            return true;
        }

        public override void Cancel()
        {
            GameObject.Destroy(_instance.gameObject);
        }
    }
}