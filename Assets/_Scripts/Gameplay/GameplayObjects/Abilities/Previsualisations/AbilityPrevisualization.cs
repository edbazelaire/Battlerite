using System;
using _Scripts.Gameplay.Configuration;
using _Scripts.Gameplay.GameplayObjects.Character;
using _Scripts.Gameplay.GameplayObjects.RuntimeDataContainers;
using Unity.Netcode;
using UnityEngine;

namespace _Scripts.Gameplay.GameplayObjects.Abilities.Previsualisations
{
    public class AbilityPrevisualization: MonoBehaviour
    {
        protected ClientCharacterVisualization ClientCharacterViz;
        protected ActionRequestData Data;
        protected ActionDescription Description;

        public virtual void Initialize(ClientCharacterVisualization caster, ActionRequestData data)
        {
            ClientCharacterViz = caster;
            Data = data;
            Description = GameDataSource.Instance.ActionDataByName[data.ActionName];
        }

        protected virtual void Update() {}

        public void Kill()
        {
            var networkObject = gameObject.GetComponent<NetworkObject>();
            networkObject.Despawn();
        }
        
        protected Vector3 GetFinalPosition()
        {
            var pos = HelpFunctions.GetFinalPosition(
                ClientCharacterViz.transform.position,
                HelpFunctions.GetMousePosition(),
                Description.MinRange,
                Description.MaxRange,
                Description.IsOnMousePosition
            );
            pos.y = 0.01f;
            
            return pos;
        }
        
        protected float GetFinalRange()
        {
            return HelpFunctions.GetFinalRange(
                ClientCharacterViz.transform.position,
                HelpFunctions.GetMousePosition(),
                Description.MinRange,
                Description.MaxRange,
                Description.IsOnMousePosition
            );
        }
    }
}