using _Scripts.Gameplay.Configuration;
using _Scripts.Gameplay.GameplayObjects.Character;
using UnityEngine;
using UnityEngine.Assertions;

namespace _Scripts.Gameplay.Actions.ActionFXs
{
    public class ReplaceGfxActionFX: ActionFX
    {
        private GameObject _characterGfx;
        private GameObject m_ReplaceGfx;
        private GameObject _replacedGfx;

        public ReplaceGfxActionFX(ref ActionRequestData data, ClientCharacterVisualization parent, ActionDescription.ActionFXDescription actionFXDescription) : base(ref data, parent, actionFXDescription)
        {
        }
        
        public override bool Start()
        {
            if (Description.Prefab == null) { return true; }

            m_ReplaceGfx = Description.Prefab;
            _characterGfx = m_Parent.transform.GetChild(0).gameObject;
            
            Assert.IsNotNull(_characterGfx, $"object {m_Parent.gameObject} has no children graphics in ClientCharacterVisualization");

            SwapGraphics(true);
                
            return true;
        }

        public override bool Update()
        {
            return true;
        }

        private void SwapGraphics(bool toReplacement)
        {
            if (m_ReplaceGfx == null) { return; }
            
            if (toReplacement)
            {
                _characterGfx.gameObject.SetActive(false);
                _replacedGfx = GameObject.Instantiate(m_ReplaceGfx, m_Parent.gameObject.transform);
                _replacedGfx.transform.position += m_ReplaceGfx.transform.localPosition;
            }
            else
            {
                GameObject.Destroy(_replacedGfx.gameObject);
                _characterGfx.gameObject.SetActive(true);
            }
        }

        public override void Cancel()
        {
            if (m_ReplaceGfx == null) { return; }
            
            SwapGraphics(false);
        }
    }
}