// using System.Collections.Generic;
// using _Scripts.Gameplay.Configuration;
// using _Scripts.Gameplay.GameplayObjects.Abilities;
// using _Scripts.Gameplay.GameplayObjects.Abilities.Previsualisations;
// using _Scripts.Gameplay.GameplayObjects.RuntimeDataContainers;
// using UnityEngine;
// using UnityEngine.Assertions;
// using UnityEngine.Networking.Types;
//
// namespace _Scripts.Gameplay.GameplayObjects.Character
// {
//     public class AbilityHandler: MonoBehaviour
//     {
//         [SerializeField] 
//         ServerCharacter m_ServerCharacter;
//
//         private ServerAbility _currentAbility;
//         public ServerAbility CurrentAbility => _currentAbility;
//         private ServerAbilityPrevisualisation _currentPrevisualisation;
//         private ActionRequestData _currentAbilityData;
//         private ActionDescription _currentActionDescription;
//         private bool _currentTriggerRealease;
//
//         private Dictionary<ActionType, float> _cooldowns = new ();
//         private float _currentCastingTimer;
//         private float _currentActivationTimer;
//
//         private AbilityState _currentAbilityState;
//
//         public enum AbilityState
//         {
//             None,
//             Casting,
//             Active
//         }
//
//         public void FixedUpdate()
//         {
//             UpdateCooldowns();
//             
//             switch (_currentAbilityState)
//             {
//                 case (AbilityState.None):
//                     return;
//                 
//                 case (AbilityState.Casting):
//                     _currentCastingTimer -= Time.fixedDeltaTime;
//                     
//                     if (_currentCastingTimer <= 0)
//                     {
//                         ActivateAbility();
//                     }
//                     
//                     break;
//                 
//                 case (AbilityState.Active):
//                     if (_currentActionDescription.BlockingMode == ActionDescription.BlockingModeType.EntireDuration)
//                     {
//                         var keepGoing = true;
//                         
//                         // check duration timer if one was set
//                         if (_currentActionDescription.DurationSeconds > 0)
//                         {
//                             _currentActivationTimer -= Time.fixedDeltaTime;
//                             if (_currentActivationTimer <= 0)
//                             {
//                                 keepGoing = false;
//                             }    
//                         }
//                         
//                         // check if ability still wants to keep going
//                         if (keepGoing && _currentAbility.WhileActive())
//                         {
//                             return;
//                         }
//                         
//                         // if for any reason the ability doesn't want to keep going, kill it and then end it
//                         _currentAbility.Kill();
//                     }
//                     
//                     EndCurrentAbility();
//                     break;
//             }
//         }
//
//         private void UpdateCooldowns()
//         {
//             var actionsInCooldown = new List<ActionType>(_cooldowns.Keys);
//             
//             foreach (var actionType in actionsInCooldown)
//             {
//                 _cooldowns[actionType] = Mathf.Max(_cooldowns[actionType] - Time.fixedDeltaTime, 0);
//             }
//         }
//
//         public void TryCastAbility(ref ActionRequestData action)
//         {
//             if (! IsUsable(action)) { return; }
//             
//             CastAbility(ref action);
//         }
//
//         private void CastAbility(ref ActionRequestData action)
//         {
//             if (! GameDataSource.Instance.ActionDataByType.TryGetValue(action.ActionTypeEnum,
//                     out ActionDescription actionDescription))
//             {
//                 Debug.Log($"Unable to find action {action.ActionTypeEnum} in GameDataSource");
//                 return;
//             }
//             
//             
//             if (actionDescription.Previsualisation != ActionDescription.PrevisualisationType.None)
//             {
//                 Assert.IsNotNull(actionDescription.PrevisualisationPrefab,
//                     $"previsualisation required for ability {actionDescription.ActionTypeEnum} but no PrevisualisationPrefab was given");
//
//                 _currentPrevisualisation = actionDescription.PrevisualisationPrefab.Activate(
//                     m_ServerCharacter.NetworkObjectId, 
//                     actionDescription.Prefab.GetComponent<ServerAbility>(), 
//                     actionDescription.Previsualisation);
//             }
//
//             _currentAbilityData = action;
//             _currentActionDescription = actionDescription;
//             _currentCastingTimer = actionDescription.ExecTimeSeconds;
//             _currentAbilityState = AbilityState.Casting;
//
//             m_ServerCharacter.serverAnimationHandler.NetworkAnimator.SetTrigger(actionDescription.Anim);
//             m_ServerCharacter.NetState.RecvDoActionClientRPC(action);
//         }
//
//         public void TryReleaseAbility(ActionRequestData action)
//         {
//             // cant perform release if no action is running
//             if (_currentAbilityState == AbilityState.None) { return; }
//
//             // cant perform release if current action is not requested action
//             if (_currentAbilityData.ActionTypeEnum != action.ActionTypeEnum) { return; }
//
//             // inform that trigger release has been sent
//             _currentTriggerRealease = true;
//         }
//         
//         private void ActivateAbility()
//         {
//             // set mode to active
//             _cooldowns[_currentActionDescription.ActionTypeEnum] = _currentActionDescription.ReuseTimeSeconds;
//
//             var ability = _currentActionDescription.Prefab.GetComponent<ServerAbility>();
//             
//             m_ServerCharacter.ReceiveMana(-_currentActionDescription.ManaCost);
//
//             _currentAbility = ability.Activate(m_ServerCharacter);
//             _currentAbilityState = AbilityState.Active;
//             _currentActivationTimer = _currentActionDescription.DurationSeconds;
//         }
//         
//         public void CancelAbility()
//         {
//             // check if ability is currently running
//             if (_currentAbilityState == AbilityState.None) { return; }
//             
//             // check if action can be cancel
//             if (_currentAbilityState == AbilityState.Active)
//             {
//                 if (!_currentActionDescription.ActionInterruptible) { return; }
//                 
//                 _currentAbility.Kill();
//             }
//
//             // reset ability
//             EndCurrentAbility();
//         }
//
//         private void EndCurrentAbility()
//         {
//             // send cancel notice to the client
//             m_ServerCharacter.NetState.CancelAbilityClientRPC();
//             if (_currentActionDescription.CancelAnimation != "")
//             {
//                 m_ServerCharacter.serverAnimationHandler.NetworkAnimator.SetTrigger(_currentActionDescription.CancelAnimation);
//             }
//
//             if (_currentPrevisualisation != null)
//             {
//                 _currentPrevisualisation.Kill();
//             } 
//             
//             _currentAbilityState = AbilityState.None;
//             _currentPrevisualisation = null;
//             _currentAbilityData = default;
//             _currentActionDescription = null;
//             _currentTriggerRealease = false;
//         }
//
//         private bool IsUsable(ActionRequestData action)
//         {
//             
//             // check cooldown
//             if (! IsCooldownOver(action.ActionTypeEnum))
//             {
//                 Debug.Log($"action still in cooldown");
//                 return false;
//             }
//
//             // is this the same action as current action 
//             if (_currentActionDescription != null && action.ActionTypeEnum == _currentActionDescription.ActionTypeEnum)
//             {
//                 Debug.Log("action currently playing");
//                 return false;
//             }
//
//             // check if current action is playing
//             if (! IsCurrentActionOverrideable())
//             {
//                 Debug.Log("Cant override current action");
//                 return false;
//             }
//             
//             // check if has enought mana
//             var abilityDescirpition = GameDataSource.Instance.ActionDataByType[action.ActionTypeEnum];
//             if (m_ServerCharacter.NetState.Mana < abilityDescirpition.ManaCost)
//             {
//                 Debug.Log("Not enough mana");
//                 return false;
//             }
//
//             return true;
//         }
//
//         /// <summary>
//         /// Figures out if an action can be played now, or if it would automatically fail because it was
//         /// used too recently. (Meaning that its ReuseTimeSeconds hasn't elapsed since the last use.)
//         /// </summary>
//         /// <param name="actionType">the action we want to run</param>
//         /// <returns>true if the action can be run now, false if more time must elapse before this action can be run</returns>
//         private bool IsCooldownOver(ActionType actionType)
//         {
//             if (_cooldowns.TryGetValue(actionType, out float cooldown))
//             {
//                 if (cooldown > 0)
//                 {
//                     return false;
//                 }
//             }
//
//             return true;
//         }
//
//         public bool IsCurrentActionOverrideable()
//         {
//             return _currentAbilityState != AbilityState.Active || _currentActionDescription.ActionInterruptible;
//         }
//     }
// }