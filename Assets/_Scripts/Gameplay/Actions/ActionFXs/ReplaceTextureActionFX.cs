using System.Collections.Generic;
using _Scripts.Gameplay.Configuration;
using _Scripts.Gameplay.GameplayObjects.Character;
using UnityEngine;

namespace _Scripts.Gameplay.Actions.ActionFXs
{
    public class ReplaceTextureActionFX: ActionFX
    {
        private List<Material> _originalMaterials = new ();
        private SkinnedMeshRenderer[] _parentMeshRenderers;
        public ReplaceTextureActionFX(ref ActionRequestData data, ClientCharacterVisualization parent, ActionDescription.ActionFXDescription actionFXDescription) : base(ref data, parent, actionFXDescription)
        {
        }

        public override bool Start()
        {
            base.Start();

            // extract material from Prefab
            var clone = GameObject.Instantiate(ActionFXDescription.SpawnPrefab);
            var newMaterial = clone.GetComponent<Renderer>().material;
            GameObject.Destroy(clone);

            // Apply material to all children with SkinnedMeshRenderer of the Parent
            _parentMeshRenderers = m_Parent.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (var meshRenderer in _parentMeshRenderers)
            {
                _originalMaterials.Add(meshRenderer.material);
                meshRenderer.material = newMaterial;
            }
            
            return true;
        }

        public override bool Update()
        {
            return true;
        }

        public override void Cancel()
        {
            // Apply original material of all the Parent children with SkinnedMeshRenderer
            for (var i = 0; i < _parentMeshRenderers.Length; i++)
            {
                _parentMeshRenderers[i].material = _originalMaterials[i];
            }
        }
    }
}