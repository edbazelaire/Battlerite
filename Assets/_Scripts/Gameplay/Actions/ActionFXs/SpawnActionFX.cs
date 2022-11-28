using System;
using _Scripts.Gameplay.Configuration;
using _Scripts.Gameplay.GameplayObjects.Character;
using UnityEngine;

namespace _Scripts.Gameplay.Actions.ActionFXs
{
    public class SpawnActionFX: ActionFX
    {
        public SpawnActionFX(ref ActionRequestData data, ClientCharacterVisualization parent, ActionDescription.ActionFXDescription actionFXDescription)
            : base(ref data, parent, actionFXDescription) { }

        private GameObject _spawnInstance;

        public override bool Start()
        {
            base.Start();
            
            if (Description.Spawns.Length == 0)
            {
                return false;
            }
           
            GameObject spawnPrefab =  ActionFXDescription.SpawnPrefab;

            _spawnInstance = ActionFXDescription.PositionType switch
            {
                ActionFXPositionType.OnCharacter     => GameObject.Instantiate(spawnPrefab, m_Parent.gameObject.transform),
                ActionFXPositionType.OnDataPosition  => GameObject.Instantiate(spawnPrefab, Data.Position, Quaternion.identity),
                ActionFXPositionType.OnFirePoint     => GameObject.Instantiate(spawnPrefab, m_Parent.PhysicsWrapper.FirePoint),
                _ => throw new NotImplementedException($"Unknown position type {ActionFXDescription.PositionType}")
            };
            
            return true;
        }

        public override bool Update()
        {
            return true;
        }

        public override void Cancel()
        {
            GameObject.Destroy(_spawnInstance);
        }
    }
}