using System;
using System.Collections.Generic;
using _Scripts.Gameplay.Configuration.AbilityEffects;
using _Scripts.Gameplay.GameplayObjects.Character;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Windows.WebCam;

namespace _Scripts.Gameplay.GameplayObjects.Abilities
{
    public class ServerHitAbilityLogic: ServerAbility
    {
        [SerializeField]
        protected NetworkAbilityState m_NetState;
        
        [SerializeField]
        protected SphereCollider m_OurCollider;

        protected int Damage => Description.Damage;
        protected int Heal => Description.Heal;
        protected int MaxVictims => Description.MaxVictims;


        /// <summary>
        /// Time when the ability is destroyed
        /// </summary>
        protected float _destroyAtSec;

        protected int _collisionMask;                   // mask containing everything we test for while moving
        protected int _blockerMask;                     // physics mask for things that block the projectile flight.
        protected int _hittableMask;                    // physics mask for things that can be hit by the projectile
                    
        protected const int k_MaxCollisions = 15;
        protected Collider[] m_CollisionCache = new Collider[k_MaxCollisions];

        /// <summary>
        /// List of everyone we've hit and dealt damage to.
        /// </summary>
        /// <remarks>
        /// Note that it's possible for entries in this list to become null if they're Destroyed post-impact.
        /// But that's fine by us! We use <c>m_HitTargets.Count</c> to tell us how many total enemies we've hit,
        /// so those nulls still count as hits.
        /// </remarks>
        protected List<GameObject> m_HitTargets = new List<GameObject>();
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            if (!IsServer)
            {
                enabled = false;
                return;
            }
            
            _isStarted = true;

            m_HitTargets = new List<GameObject>();
            _isDead = false;

            _collisionMask = LayerMask.GetMask(new[] { "Players", "Default", "Environment", "DestructibleObjects" });
            _blockerMask = LayerMask.GetMask(new[] { "Default", "Environment", "BlockingAbilities" });
            _hittableMask = LayerMask.GetMask(new[] {"Players", "DestructibleObjects"});
        }
        
        protected virtual void FixedUpdate() {}

        protected void DetectCollisions()
        {
            DetectCollisions(transform.localToWorldMatrix.MultiplyPoint(m_OurCollider.center), m_OurCollider.radius, Description.IgnoreWalls);
        }
        
        protected void DetectCollisions(Vector3 position, float radius, bool ignoreWalls)
        {
            var numCollisions = Physics.OverlapSphereNonAlloc(position, radius, m_CollisionCache, _collisionMask);

            for (int i = 0; i < numCollisions; i++)
            {
                int layerTest = 1 << m_CollisionCache[i].gameObject.layer;
                
                if (! ignoreWalls && (layerTest & _blockerMask) != 0)
                {
                    // hit a wall
                    Debug.Log($"Hit blockable object : {m_CollisionCache[i].gameObject.name} with layer {m_CollisionCache[i].gameObject.layer}" );
                    Kill();
                    return;
                }

                if ((layerTest & _hittableMask) != 0 && !m_HitTargets.Contains(m_CollisionCache[i].gameObject))
                {
                    // check if object is a NetworkObject and not the spell caster
                    var targetNetObj = m_CollisionCache[i].GetComponentInParent<NetworkObject>();
                    if (! targetNetObj || targetNetObj.NetworkObjectId == _spawnerId)
                    {
                        continue;
                    }
                    
                    // all hittable layer entities should have one of these.
                    var success = OnHit(targetNetObj);

                    if (success)
                    {
                        m_HitTargets.Add(m_CollisionCache[i].gameObject);
                        if (MaxVictims > 0 && m_HitTargets.Count >= MaxVictims)
                        {
                            Kill();
                        }
                    }
                }
            }
        }

        protected virtual bool OnHit(NetworkObject targetNetObj)
        {
            var success = false;

            //retrieve the person that created us, if he's still around.
            NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(_spawnerId, out var spawnerNet);
            var spawnerObj = spawnerNet != null ? spawnerNet.GetComponent<ServerCharacter>() : null;

            var serverCharacterEffects = targetNetObj.GetComponent<ServerCharacterEffects>();

            var targetPlayer = targetNetObj.GetComponent<ServerCharacter>();
            
            var isAlly = targetPlayer != null && targetPlayer.Team == spawnerObj.Team;

            if (isAlly && Heal > 0)
            {
                success = true;
                OnAllyHit(spawnerObj, targetNetObj, serverCharacterEffects);
            }

            if (! isAlly)
            {
                success = true;
                OnEnemyHit(spawnerObj, targetNetObj, serverCharacterEffects);
            }

            if (success)
            {
                // Apply AbilityEffects if possible
                if (serverCharacterEffects != null)
                {
                    serverCharacterEffects.AddAbilityEffects(Description.AbilityEffects);
                }
            }
            
            return success;
        }

        protected virtual void OnAllyHit(ServerCharacter spawnerObj, NetworkObject target, ServerCharacterEffects serverCharacterEffects)
        {
            target.GetComponent<IDamageable>().ReceiveHP(spawnerObj, Heal);
        }

        protected virtual void OnEnemyHit(ServerCharacter spawnerObj, NetworkObject target, ServerCharacterEffects serverCharacterEffects)
        {
            target.GetComponent<IDamageable>().ReceiveHP(spawnerObj, -Damage);
            
            // only apply state effects to enemies
            if (serverCharacterEffects != null)
            {
                serverCharacterEffects.ApplyStateEffects(Description.StateEffects);
            }
                
            // only apply knock back to enemies
            var serverCharacter = target.GetComponent<ServerCharacter>();
            if (serverCharacter != null && Description.KnockBackDistance > 0)
            {
                serverCharacter.Movement.StartKnockback(transform.position, Description.KnockBackSpeed, Description.KnockBackDistance);
            }
                
            m_NetState.RecvHitEnemyClientRPC(target.NetworkObjectId);
        }
    }
}