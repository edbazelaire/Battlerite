using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using _Scripts.Gameplay.Configuration;
using _Scripts.Gameplay.Configuration.AbilityEffects;
using _Scripts.Gameplay.GameplayObjects.Abilities;
using _Scripts.Gameplay.GameplayObjects.RuntimeDataContainers;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;

namespace _Scripts.Gameplay.GameplayObjects.Character
{
    /// <summary>
    /// Handle effects on character (AbilityEffects, Buffs...)
    /// </summary>
    public class ServerCharacterEffects: NetworkBehaviour
    {
        [SerializeField]
        ServerCharacter m_ServerCharacter;

        private Dictionary<AbilityEffect, float> _abilityEffects = new ();
        public Dictionary<AbilityEffect, float> AbilityEffects => _abilityEffects;
        private Dictionary<AbilityEffect, float> _abilityEffectsLastTick = new ();
        
        private Dictionary<StateEffectType, StateEffect> _stateEffects = new();
        private Dictionary<StateEffectType, float> _stateEffectTimers = new ();

        // Buffs in damages / heal
        public float PercentDamageReceived = 1;
        public float PercentHealingReceived = 1;
        public float PercentDamageDelt = 1;
        public float PercentHealingDealt = 1;

        public bool CanPerformAction => _stateEffectTimers[StateEffectType.Stun] == 0 
                                        && _stateEffectTimers[StateEffectType.Petrify] == 0;

        // =============================================================================================================
        #region Core

        private void Awake()
        {
            // init values of state effects
            foreach (StateEffectType stateEffectType in Enum.GetValues(typeof(StateEffectType)))
            {
                _stateEffectTimers[stateEffectType] = 0;
            }
        }

        public override void OnNetworkSpawn()
        {
            if (!IsServer)
            {
                enabled = false;
            }
        }

        public void Update()
        {
            UpdateAbilityEffects();
            UpdateStateEffects();
        }

        #endregion

        // =============================================================================================================
        #region AbilityEffects

        public void UpdateAbilityEffects()
        {
            // list of ability effects that needs to proc and be removed
            List<AbilityEffect> abilityEffectToProc = new ();
            var abilityEffects = new List<AbilityEffect>(_abilityEffects.Keys);
            
            foreach (var abilityEffect in abilityEffects)
            {
                _abilityEffects[abilityEffect] -= Time.deltaTime;
                // check if AbilityEffect must be removed
                if (_abilityEffects[abilityEffect] <= 0)
                {
                    abilityEffectToProc.Add(abilityEffect);
                    continue;
                }

                // check if AbilityEffect Tick must apply
                if (Time.time >= _abilityEffectsLastTick[abilityEffect] + abilityEffect.tickInterval)
                {
                    ApplyAbilityEffectTick(abilityEffect);
                }
            }

            // remove necessary AbilityEffects from dict of current AbilityEffects on the character 
            foreach (var abilityEffect in abilityEffectToProc)
            {
                ProcAbilityEffect(abilityEffect);
            }
        }

        public void AddAbilityEffects(ActionDescription.AbilityEffectDuration[] abilityEffects)
        {
            foreach (var abilityEffect in abilityEffects)
            {
                AddAbilityEffect(abilityEffect.AbilityEffect, abilityEffect.Duration);
            }
        }
        
        public void AddAbilityEffect(string abilityEffectName, float duration = 0)
        {
            AddAbilityEffect(GameDataSource.Instance.AbilityEffectsByName[abilityEffectName], duration);
        }
        
        /// <summary>
        /// Effect attached to an Ability that will be added on the Character
        /// </summary>
        /// <param name="abilityEffect">AbilityEffect to add on the character</param>
        /// <param name="duration">duration of the effect. Can not be below 0</param>
        public void AddAbilityEffect(AbilityEffect abilityEffect, float duration = 0)
        {
            Assert.IsTrue(duration >= 0, $"duration of {abilityEffect.name} cant be below 0 (given {duration})");
            if (duration == 0)
            {
                duration = abilityEffect.duration;
            }

            // ALREADY has the Effect
            if (_abilityEffects.ContainsKey(abilityEffect) && _abilityEffects[abilityEffect] > 0)
            {
                // make sure that given duration plus current duration doesn't exceed max duration of the effect
                duration = Mathf.Min(abilityEffect.duration, _abilityEffects[abilityEffect] + duration);

                if (abilityEffect.shield > 0)
                {
                    // get amount of shield to remove, which is the minimum between lasting shield and ability shield 
                    var shieldToRemove = Math.Min(m_ServerCharacter.NetState.Shield, abilityEffect.shield);
                    
                    // remove current ability shield (before adding it full back later)
                    m_ServerCharacter.ReceiveShield(-shieldToRemove);
                }
            }
            
            // NEW effect
            else
            {
                // update speed status
                m_ServerCharacter.Movement.AddBonusSpeed(abilityEffect.bonusSpeed);
            }
            
            // add shield if any
            m_ServerCharacter.ReceiveShield(abilityEffect.shield);
            
            // set duration and last tick of the ability
            _abilityEffects[abilityEffect] = duration;
            _abilityEffectsLastTick[abilityEffect] = Time.time;
            
            // send event to the client that a new ability effect was received
            m_ServerCharacter.NetState.RcvAbilityEffectClientRpc(abilityEffect.name, duration);
        }
        
        private void ApplyAbilityEffectTick(AbilityEffect abilityEffect)
        {
            _abilityEffectsLastTick[abilityEffect] = Time.time;
            
            if (abilityEffect.tickDamages > 0)
            {
                m_ServerCharacter.DamageReceiver.ReceiveHP(null, -abilityEffect.tickDamages);
            }
            
            if (abilityEffect.tickHeal > 0)
            {
                m_ServerCharacter.DamageReceiver.ReceiveHP(null, abilityEffect.tickHeal);
            }
        }

        public void ProcAbilityEffect(AbilityEffect abilityEffect)
        {
            if (_abilityEffects.ContainsKey(abilityEffect))
            {
                // Proc final heals/damages
                if (abilityEffect.finalDamages > 0)
                {
                    m_ServerCharacter.DamageReceiver.ReceiveHP(null, -abilityEffect.finalDamages);
                }
                
                if (abilityEffect.finalHeal > 0)
                {
                    m_ServerCharacter.DamageReceiver.ReceiveHP(null, abilityEffect.finalHeal);
                }
                
                // update speed status
                m_ServerCharacter.Movement.RemoveBonusSpeed(abilityEffect.bonusSpeed);
                
                // remove ability effect from list of current ability effects
                _abilityEffects.Remove(abilityEffect);
                
                // send event to the clients indicating that the ability effect has ended
                m_ServerCharacter.NetState.EndAbilityEffectClientRpc(abilityEffect.name);
                
                // check if there is a new effect to display on the UI
                CheckNewEffectToDisplay();
            }
        }

        #endregion

        // =============================================================================================================
        #region StateEffects

        public void ApplyStateEffects(StateEffect[] stateEffects)
        {
            foreach (var stateEffect in stateEffects)
            {
                // cant re-apply Petrify
                if (stateEffect.type == StateEffectType.Petrify && _stateEffectTimers[StateEffectType.Petrify] > 0) { continue; }

                StartCoroutine(ApplyStateEffect(stateEffect));
            }
        }

        private IEnumerator ApplyStateEffect(StateEffect stateEffect)
        {
            yield return new WaitForSeconds(stateEffect.delay);

            if (_stateEffectTimers[stateEffect.type] < stateEffect.duration)
            {
                _stateEffectTimers[stateEffect.type] = stateEffect.duration;
                _stateEffects[stateEffect.type] = stateEffect;

                if (stateEffect.shield > 0)
                {
                    m_ServerCharacter.ReceiveShield(stateEffect.shield);
                }

                if (CheckEffectOverride(stateEffect.type, m_ServerCharacter.NetState.NetworkControlState.StateEffectType.Value))
                {
                    m_ServerCharacter.NetState.NetworkControlState.StateEffectType.Value = stateEffect.type;
                }

                m_ServerCharacter.NetState.RcvStateEffectClientRpc(stateEffect.type.ToString(), stateEffect.duration, stateEffect.duration);
            }
        }

        private void EndStateEffect(StateEffectType stateEffectType)
        {
            // if state effect not currently active, dont try to end it
            if (_stateEffects[stateEffectType] == null) { return; }
            
            // remove shield if had one
            if (_stateEffects[stateEffectType].shield > 0)
            {
                m_ServerCharacter.ReceiveShield(-_stateEffects[stateEffectType].shield);    
            }
            
            // reset values of current effect
            _stateEffectTimers[stateEffectType] = 0;
            _stateEffects[stateEffectType] = null;
            m_ServerCharacter.NetState.EndStateEffectClientRpc(stateEffectType.ToString());
            
            // check if there is a new state effect to display to the NetworkControlState
            CheckEffectControlDisplay();
            
            // check if there is a new effect to display on the UI
            CheckNewEffectToDisplay();
        }

        private bool CheckEffectOverride(StateEffectType stateEffectType, StateEffectType currentEffect)
        {
            return stateEffectType > currentEffect;
        }

        private void CheckEffectControlDisplay()
        {
            var currentStateEffect = StateEffectType.Normal;
            foreach (var row in _stateEffectTimers)
            {
                if (row.Value > 0 && CheckEffectOverride(row.Key, currentStateEffect))
                {
                    currentStateEffect = row.Key;
                }
            }
            
            m_ServerCharacter.NetState.NetworkControlState.StateEffectType.Value = currentStateEffect;
        }

        private void UpdateStateEffects()
        {
            var stateEffectTypes = new List<StateEffectType>(_stateEffects.Keys);
            foreach (var stateEffectType in stateEffectTypes)
            {
                Assert.IsTrue(_stateEffectTimers[stateEffectType] >= 0, 
                    $"timer of {stateEffectType} = {_stateEffectTimers[stateEffectType]}");
                
                if (_stateEffectTimers[stateEffectType] == 0) { continue; }
                
                _stateEffectTimers[stateEffectType] -= Time.deltaTime;
                if (_stateEffectTimers[stateEffectType] <= 0)
                {
                    EndStateEffect(stateEffectType);
                    continue;
                }

                // petrify ends when shield is gone
                if (stateEffectType == StateEffectType.Petrify && m_ServerCharacter.NetState.Shield == 0)
                {
                    EndStateEffect(stateEffectType);
                }
            }
        }

        #endregion

        // =============================================================================================================
        #region UICheckNewDisplay

        public void CheckNewEffectToDisplay()
        {
            float timer;
            if (TryGetNewStateEffectToDisplay(out StateEffect stateEffect, out timer))
            {
                m_ServerCharacter.NetState.RcvStateEffectClientRpc(stateEffect.type.ToString(), stateEffect.duration, timer);
            } 
            
            else if (TryGetNewAbilityEffectToDisplay(out AbilityEffect abilityEffect, out timer))
            {
                m_ServerCharacter.NetState.RcvAbilityEffectClientRpc(abilityEffect.name, timer);
            }
        }

        public bool TryGetNewStateEffectToDisplay(out StateEffect stateEffect, out float timer)
        {
            var stateEffectTypes = new List<StateEffectType>(_stateEffects.Keys);
            foreach (var stateEffectType in stateEffectTypes)
            {
                if (_stateEffectTimers[stateEffectType] > 0)
                {
                    stateEffect = _stateEffects[stateEffectType];
                    timer = _stateEffectTimers[stateEffectType];

                    return true;
                }
            }

            stateEffect = null;
            timer = 0;
            return false;
        }

        public bool TryGetNewAbilityEffectToDisplay(out AbilityEffect abilityEffect, out float timer)
        {
            if (_abilityEffects.Count == 0)
            {
                abilityEffect = null;
                timer = 0;
                return false;
            }

            var firstAbility = _abilityEffects.First();
            abilityEffect = firstAbility.Key;
            timer = firstAbility.Value;
            return true;
        }

        #endregion
    }
}