using System;
using _Scripts.Gameplay.Configuration;
using _Scripts.Gameplay.GameplayObjects.Character;
using UnityEngine;
using UnityEngine.Assertions;

namespace _Scripts.Gameplay.Actions.ActionFXs
{
    public class AoeActionFX: ActionFX
    {
        public AoeActionFX(ref ActionRequestData data, ClientCharacterVisualization parent, ActionDescription.ActionFXDescription actionFXDescription)
            : base(ref data, parent, actionFXDescription) { }

        public override bool Start()
        {
            base.Start();
            
            if (Description.Spawns.Length == 0)
            {
                return false;
            }
           
            GameObject spawnPrefab =  ActionFXDescription.SpawnPrefab;

            var position = ActionFXDescription.PositionType switch
            {
                ActionFXPositionType.OnCharacter     => m_Parent.transform.position,
                ActionFXPositionType.OnDataPosition  => Data.Position,
                ActionFXPositionType.OnFirePoint     => throw new NotImplementedException("FirePoint not implemented"),
                _ => throw new NotImplementedException($"Unknown position type {ActionFXDescription.PositionType}")
            };

            // Also, if spawns, spawn them
            var spawnClone = GameObject.Instantiate(spawnPrefab, Data.Position, Quaternion.identity);

            SetParticlesLifetime(spawnClone, Description.ActivationDelay);
                
            if (Description.Radius > 0)
            {
                var collider = spawnClone.GetComponent<SphereCollider>();
                Assert.IsNotNull(collider, $"Missing SphereCollider in {spawnClone.name}");
                spawnClone.transform.localScale = Vector3.one * Description.Radius / collider.radius;
            }
            
            GameObject.Destroy(spawnClone, Description.ActivationDelay);
            
            return false;
        }
        
        void SetParticlesLifetime(GameObject gameObject, float timer)
        {
            ParticleSystem[] particleSystems = gameObject.GetComponentsInChildren<ParticleSystem>();
            foreach (var particleSystem in particleSystems)
            {
                var main = particleSystem.main;
                main.startLifetime = timer;     // set duration of the effects
            }
        }

        public override bool Update()
        {
            throw new Exception("This should not execute");
        }
    }
}