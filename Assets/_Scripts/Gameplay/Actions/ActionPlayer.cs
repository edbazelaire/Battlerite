using System.Collections.Generic;
using _Scripts.Gameplay.Configuration;
using _Scripts.Gameplay.GameplayObjects.Character;
using _Scripts.Gameplay.GameplayObjects.RuntimeDataContainers;
using UnityEngine;
using BlockingMode = _Scripts.Gameplay.Configuration.ActionDescription.BlockingModeType;

namespace _Scripts.Gameplay.Actions
{
    /// <summary>
    /// Class responsible for playing back action inputs from user.
    /// </summary>
    public class ActionPlayer
    {
        ServerCharacter m_ServerCharacter;

        private Action _currentAction;
        private ActionRequestData _currentAbilityData;
        private ActionDescription _currentActionDescription;
        private bool _currentTriggerRealease;

        private Dictionary<string, float> _cooldowns = new ();
        private float _currentCastingTimer;
        private float _currentActivationTimer;

        private ActionState _currentActionState;

        public enum ActionState
        {
            None,
            Casting,
            Active,
            End
        }

        public ActionPlayer(ServerCharacter serverCharacter)
        {
            m_ServerCharacter = serverCharacter;
        }

        public void Update()
        {
            UpdateCooldowns();
            
            switch (_currentActionState)
            {
                case (ActionState.None):
                    return;
                
                case (ActionState.Casting):
                    _currentCastingTimer -= Time.deltaTime;
                    
                    if (_currentCastingTimer <= 0)
                    {
                        ActivateAbility();
                    }
                    
                    break;
                
                case (ActionState.Active):
                    if (_currentActionDescription.BlockingMode == ActionDescription.BlockingModeType.EntireDuration)
                    {
                        var keepGoing = true;
                        
                        // check duration timer if one was set
                        if (_currentActionDescription.DurationSeconds > 0)
                        {
                            _currentActivationTimer -= Time.deltaTime;
                            if (_currentActivationTimer <= 0)
                            {
                                keepGoing = false;
                            }    
                        }
                        
                        // check if ability still wants to keep going
                        if (keepGoing && _currentAction.Update())
                        {
                            return;
                        }
                        
                        // if for any reason the ability doesn't want to keep going, cancel it
                        m_ServerCharacter.NetState.CancelAbilityClientRPC(_currentAbilityData);
                        _currentAction.End();
                    }
                    
                    ResetCurrentAction();
                    break;
            }
        }

        private void UpdateCooldowns()
        {
            var actionsInCooldown = new List<string>(_cooldowns.Keys);
            
            foreach (var actionType in actionsInCooldown)
            {
                _cooldowns[actionType] = Mathf.Max(_cooldowns[actionType] - Time.deltaTime, 0);
            }
        }

        public void TryCastAbility(ref ActionRequestData action)
        {
            if (! IsUsable(action)) { return; }
            
            CastAbility(ref action);
        }

        private void CastAbility(ref ActionRequestData actionData)
        {
            if (! GameDataSource.Instance.ActionDataByName.TryGetValue(actionData.ActionName,
                    out ActionDescription actionDescription))
            {
                Debug.Log($"Unable to find action {actionData.ActionName} in GameDataSource");
                return;
            }
            
            // cancel current action if any
            CancelAbility();

            // make new action
            _currentAction = Action.MakeAction(m_ServerCharacter, ref actionData);

            // check if action cancels movement
            if (_currentAction.Description.CancelRotation)
            {
                m_ServerCharacter.ServerRotation.CancelRotation();
            }
            
            if (_currentAction.Description.CancelMovement)
            {
                m_ServerCharacter.Movement.CancelMovement();
            }
            
            // create previsualization prefab if any
            if (actionDescription.PrevisualisationPrefab != null)
            {
                m_ServerCharacter.NetState.RecvDoPrevisualizationClientRPC(actionData);
            }

            // set new action in casting state
            _currentAbilityData = actionData;
            _currentActionDescription = actionDescription;
            _currentCastingTimer = actionDescription.ExecTimeSeconds;
            _currentActionState = ActionState.Casting;

            // start the action
            _currentAction.Start();
            if (_currentActionDescription.Anim != null)
            {
                m_ServerCharacter.serverAnimationHandler.NetworkAnimator.SetTrigger(_currentActionDescription.Anim);
            } 
            
            m_ServerCharacter.NetState.RecvActionStateClientRPC(_currentAction.Data, _currentActionState);
        }

        public void TryReleaseAbility(ActionRequestData action)
        {
            // cant perform release if no action is running
            if (_currentActionState == ActionState.None) { return; }

            // cant perform release if current action is not requested action
            if (_currentAbilityData.ActionName != action.ActionName) { return; }

            // inform that trigger release has been sent
            _currentTriggerRealease = true;
        }
        
        private void ActivateAbility()
        {
            // set cooldown of the ability
            _cooldowns[_currentActionDescription.ActionName] = _currentActionDescription.ReuseTimeSeconds;
            
            m_ServerCharacter.ReceiveMana(-_currentActionDescription.ManaCost);
            
            _currentAction.Activate();
            _currentActionState = ActionState.Active;
            _currentActivationTimer = _currentActionDescription.DurationSeconds;
            
            // cancel previsualization when ability is activated
            m_ServerCharacter.NetState.CancelPrevisualizationClientRPC();
            
            // send action to client
            m_ServerCharacter.NetState.RecvActionStateClientRPC(_currentAction.Data, _currentActionState);
        }

        public void ClearActions(bool clearNonBlocking)
        {
            CancelAbility();
            m_ServerCharacter.NetState.RecvCancelAllActionsClientRpc();
        }
        
        public void CancelAbility()
        {
            switch (_currentActionState)
            {
                // check if ability is currently running
                case ActionState.None:
                    return;
                
                // check if ability is currently running
                case ActionState.Casting:
                    _currentAction.Cancel();
                    m_ServerCharacter.NetState.CancelPrevisualizationClientRPC();
                    break;
                
                // check if action can be cancel
                case ActionState.Active when !_currentActionDescription.ActionInterruptible:
                    return;
                
                case ActionState.Active:
                    _currentAction.End();
                    break;
            }

            // send information to the client that the action has been canceled
            m_ServerCharacter.NetState.CancelAbilityClientRPC(_currentAbilityData);
            
            // reset current action (timers, state, ...)
            ResetCurrentAction();
        }

        private void ResetCurrentAction()
        {
            // send cancel notice to the client
            if (_currentActionDescription.CancelAnimation != "")
            {
                m_ServerCharacter.serverAnimationHandler.NetworkAnimator.SetTrigger(_currentActionDescription.CancelAnimation);
            }
            
            // check if action cancels rotation to enable it back
            if (_currentAction.Description.CancelRotation)
            {
                m_ServerCharacter.ServerRotation.EnableRotation();
            }
            
            // check if action cancels rotation to enable it back
            if (_currentAction.Description.CancelMovement)
            {
                m_ServerCharacter.Movement.EnableMovement();
            }

            _currentActionState = ActionState.None;
            _currentAbilityData = default;
            _currentActionDescription = null;
            _currentTriggerRealease = false;
        }

        private bool IsUsable(ActionRequestData action)
        {
            // check cooldown
            if (! IsCooldownOver(action.ActionName))
            {
                Debug.Log($"action still in cooldown");
                return false;
            }

            // is this the same action as current action 
            if (_currentActionDescription != null && action.ActionName == _currentActionDescription.ActionName)
            {
                Debug.Log("action currently playing");
                return false;
            }

            // check if current action is playing
            if (! IsCurrentActionOverrideable())
            {
                Debug.Log("Cant override current action");
                return false;
            }
            
            // check if has enough mana
            var abilityDescirpition = GameDataSource.Instance.ActionDataByName[action.ActionName];
            if (m_ServerCharacter.NetState.Mana < abilityDescirpition.ManaCost)
            {
                Debug.Log("Not enough mana");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Figures out if an action can be played now, or if it would automatically fail because it was
        /// used too recently. (Meaning that its ReuseTimeSeconds hasn't elapsed since the last use.)
        /// </summary>
        /// <param name="actionType">the action we want to run</param>
        /// <returns>true if the action can be run now, false if more time must elapse before this action can be run</returns>
        private bool IsCooldownOver(string actionType)
        {
            if (_cooldowns.TryGetValue(actionType, out float cooldown))
            {
                if (cooldown > 0)
                {
                    return false;
                }
            }

            return true;
        }

        public bool IsCurrentActionOverrideable()
        {
            return _currentActionState != ActionState.Active || _currentActionDescription.ActionInterruptible;
        }
    }
}

