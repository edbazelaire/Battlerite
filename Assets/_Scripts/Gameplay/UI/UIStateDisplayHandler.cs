using System.Collections;
using _Scripts.Gameplay.Configuration.AbilityEffects;
using _Scripts.Gameplay.GameplayObjects;
using _Scripts.Gameplay.GameplayObjects.Abilities;
using _Scripts.Gameplay.GameplayObjects.Character;
using _Scripts.Gameplay.GameplayObjects.RuntimeDataContainers;
using _Scripts.Infrastructure.ScriptableObjectArchitecture;
using Unity.Multiplayer.Samples.BossRoom;
using Unity.Multiplayer.Samples.BossRoom.Client;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;

namespace _Scripts.Gameplay.UI
{
    /// <summary>
    /// Class designed to only run on a client. Add this to a world-space prefab to display health or name on UI.
    /// </summary>
    /// <remarks>
    /// Execution order is explicitly set such that it this class executes its LateUpdate after any Cinemachine
    /// LateUpdate calls, which may alter the final position of the game camera.
    /// </remarks>
    [DefaultExecutionOrder(300)]
    public class UIStateDisplayHandler : NetworkBehaviour
    { 
        [SerializeField]
        UIStateDisplay m_UIStatePrefab;

        // spawned in world (only one instance of this)
        UIStateDisplay m_UIState;

        RectTransform m_UIStateRectTransform;

        bool m_UIStateActive;
        
        [SerializeField]
        NetworkCharacterState m_NetworkCharacterState;

        [SerializeField]
        NetworkNameState m_NetworkNameState;

        [SerializeField]
        NetworkHealthState m_NetworkHealthState;

        [SerializeField]
        NetworkManaState m_NetworkManaState;

        [SerializeField]
        ServerCharacterEffects m_ServerCharacterEffects;

        [SerializeField]
        ClientCharacter m_ClientCharacter;

        ClientAvatarGuidHandler m_ClientAvatarGuidHandler;

        NetworkAvatarGuidState m_NetworkAvatarGuidState;

        [SerializeField]
        IntVariable m_BaseHP;
        
        int _baseManaReserve;
        int _maxManaPool;

        [Tooltip("UI object(s) will appear positioned at this transforms position.")]
        [SerializeField]
        Transform m_TransformToTrack;

        Camera m_Camera;

        Transform m_CanvasTransform;

        // as soon as any HP goes to 0, we wait this long before removing health bar UI object
        const float k_DurationSeconds = 2f;

        [Tooltip("World space vertical offset for positioning.")]
        [SerializeField]
        float m_VerticalWorldOffset;

        [Tooltip("Screen space vertical offset for positioning.")]
        [SerializeField]
        float m_VerticalScreenOffset;

        Vector3 m_VerticalOffset;

        // used to compute world position based on target and offsets
        Vector3 m_WorldPos;

        private AbilityEffect _currentAbilityEffect;
        private string _currentStateEffectType;

        public override void OnNetworkSpawn()
        {
            if (!NetworkManager.Singleton.IsClient)
            {
                enabled = false;
                return;
            }

            var cameraGameObject = GameObject.FindWithTag("MainCamera");
            if (cameraGameObject)
            {
                m_Camera = cameraGameObject.GetComponent<Camera>();
            }
            Assert.IsNotNull(m_Camera);

            var canvasGameObject = GameObject.FindWithTag("GameCanvas");
            if (canvasGameObject)
            {
                m_CanvasTransform = canvasGameObject.transform;
            }
            Assert.IsNotNull(m_CanvasTransform);
            
            Assert.IsNotNull(m_NetworkHealthState, "A NetworkHealthState component needs to be attached!");
            

            m_VerticalOffset = new Vector3(0f, m_VerticalScreenOffset, 0f);

            // if PC, find our graphics transform and update health through callbacks, if displayed
            if (TryGetComponent(out m_ClientAvatarGuidHandler) && TryGetComponent(out m_NetworkAvatarGuidState))
            {
                m_BaseHP = m_NetworkAvatarGuidState.RegisteredAvatar.CharacterClass.BaseHP;
                _baseManaReserve = m_NetworkAvatarGuidState.RegisteredAvatar.CharacterClass.ManaReserve;
                _maxManaPool = m_NetworkAvatarGuidState.RegisteredAvatar.CharacterClass.ManaPool;

                if (m_ClientCharacter != null && m_ClientCharacter.ChildVizObject)
                {
                    TrackGraphicsTransform(m_ClientCharacter.ChildVizObject.gameObject);
                }
                else
                {
                    m_ClientAvatarGuidHandler.AvatarGraphicsSpawned += TrackGraphicsTransform;
                }
                
                m_NetworkHealthState.hitPointsReplenished += DisplayUIHealth;
                m_NetworkHealthState.hitPointsDepleted += RemoveUIHealth;
            }

            if (m_ServerCharacterEffects != null)
            {
                m_NetworkCharacterState.AbilityEffectReceivedClient += OnAbilityEffecReceived; 
                m_NetworkCharacterState.AbilityEffectEndsClient += OnAbilityEffectEnds; 
                m_NetworkCharacterState.StateEffectReceivedClient += OnStateEffectReceived; 
                m_NetworkCharacterState.StateEffectEndsClient += OnStateEffectEnds; 
            } 

            DisplayUIName(); 
            DisplayUIHealth();
            DisplayUIMana();
        }

        void OnDisable()
        {
            if (m_NetworkHealthState != null)
            {
                m_NetworkHealthState.hitPointsReplenished -= DisplayUIHealth;
                m_NetworkHealthState.hitPointsDepleted -= RemoveUIHealth;
            }

            if (m_ClientAvatarGuidHandler)
            {
                m_ClientAvatarGuidHandler.AvatarGraphicsSpawned -= TrackGraphicsTransform;
            }
        }

        void OnAbilityEffecReceived(string abilityEffectName, float currentDuration)
        {
            print("Received effect : " + abilityEffectName);
            
            if (_currentStateEffectType != null) { return; }

            var abilityEffect = GameDataSource.Instance.AbilityEffectsByName[abilityEffectName];
            
            if (_currentAbilityEffect != null && _currentAbilityEffect != abilityEffect) { return; }

            _currentAbilityEffect = abilityEffect;
            DisplayUIEffect(abilityEffect.name, abilityEffect.duration, currentDuration, abilityEffect.color);
        }

        void OnAbilityEffectEnds(string abilityEffectName)
        {
            // check if state effect is currently displayed (overrides ability effects)
            if (_currentStateEffectType != null) { return; }

            var abilityEffect = GameDataSource.Instance.AbilityEffectsByName[abilityEffectName]; 
            
            // check if ability that ended was the ability that we are currently displaying
            if (_currentAbilityEffect != abilityEffect) { return; }

            _currentAbilityEffect = null;

            // when current ability ends, check for new ability effect to display
            if (m_ServerCharacterEffects.TryGetNewAbilityEffectToDisplay(out AbilityEffect newAbilityEffect, out float timer))
            {
                _currentAbilityEffect = newAbilityEffect;
                DisplayUIEffect(newAbilityEffect.name, newAbilityEffect.duration, timer, newAbilityEffect.color);
            } 
            
            // if no new ability to display : display name
            else
            {
                DisplayUIName();
            }
        }

        void OnStateEffectReceived(string stateEffect, float duration, float currentDuration)
        {
            if (_currentStateEffectType != null && _currentStateEffectType != stateEffect) { return; }
            
            // remove _currentAbilityEffect (gets override)
            _currentAbilityEffect = null;   
            
            // set and display state effect
            _currentStateEffectType = stateEffect;
            DisplayUIEffect(stateEffect, duration, currentDuration, Color.red);
        }

        void OnStateEffectEnds(string stateEffectType)
        {
            if (_currentStateEffectType != stateEffectType) { return; }
          
            _currentStateEffectType = null;
            _currentAbilityEffect = null;
            float timer;

            // when current ability ends, check for new state effect to display
            if (m_ServerCharacterEffects.TryGetNewStateEffectToDisplay(out StateEffect newStateEffect, out timer))
            {
                _currentStateEffectType = newStateEffect.type.ToString();
                DisplayUIEffect(_currentStateEffectType, newStateEffect.duration, timer, Color.red);
            } 
            
            // otherwise, check for new ability effect to display
            else if (m_ServerCharacterEffects.TryGetNewAbilityEffectToDisplay(out AbilityEffect newAbilityEffect, out timer))
            {
                _currentAbilityEffect = newAbilityEffect;
                DisplayUIEffect(newAbilityEffect.name, newAbilityEffect.duration, timer, newAbilityEffect.color);
            }
            
            // if no state or ability effect to display : display name
            else
            {
                DisplayUIName();
            }
        }

        void DisplayUIName()
        {
            if (m_NetworkNameState == null)
            {
                return;
            }

            if (m_UIState == null)
            {
                SpawnUIState();
            }

            m_UIState.HideEffect();
            m_UIState.DisplayName(m_NetworkNameState.Name);
            m_UIStateActive = true;
        }

        void DisplayUIHealth()
        {
            if (m_NetworkHealthState == null)
            {
                return;
            }

            if (m_UIState == null)
            {
                SpawnUIState();
            }

            m_UIState.DisplayHealth(m_NetworkHealthState.HitPoints, m_BaseHP.Value);
            m_UIStateActive = true;
        }

        void DisplayUIMana()
        {
            if (m_NetworkManaState == null)
            {
                return;
            }

            if (m_UIState == null)
            {
                SpawnUIState();
            }

            m_UIState.DisplayMana(m_NetworkManaState.ManaReserve, m_NetworkManaState.Mana, _baseManaReserve, _maxManaPool);
            m_UIStateActive = true;
        }

        void DisplayUIEffect(string name, float maxDuration, float currentDuration, Color color)
        {
            if (m_ServerCharacterEffects == null)
            {
                return;
            }

            if (m_UIState == null)
            {
                SpawnUIState();
            }
            
            m_UIState.HideName();
            m_UIState.DisplayEffect(name, maxDuration, currentDuration, color);
            m_UIStateActive = true;
        }

        void SpawnUIState()
        {
            m_UIState = Instantiate(m_UIStatePrefab, m_CanvasTransform);
            // make in world UI state draw under other UI elements
            m_UIState.transform.SetAsFirstSibling();
            m_UIStateRectTransform = m_UIState.GetComponent<RectTransform>();
        }

        void RemoveUIHealth()
        {
            StartCoroutine(WaitToHideHealthBar());
        }

        IEnumerator WaitToHideHealthBar()
        {
            yield return new WaitForSeconds(k_DurationSeconds);

            m_UIState.HideHealth();
        }

        void TrackGraphicsTransform(GameObject graphicsGameObject)
        {
            m_TransformToTrack = graphicsGameObject.transform;
        }

        /// <remarks>
        /// Moving UI objects on LateUpdate ensures that the game camera is at its final position pre-render.
        /// </remarks>
        void LateUpdate()
        {
            if (m_UIStateActive && m_TransformToTrack)
            {
                // set world position with world offset added
                m_WorldPos.Set(m_TransformToTrack.position.x,
                    m_TransformToTrack.position.y + m_VerticalWorldOffset,
                    m_TransformToTrack.position.z);

                m_UIStateRectTransform.position = m_Camera.WorldToScreenPoint(m_WorldPos) + m_VerticalOffset;
            }
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            if (m_UIState != null)
            {
                Destroy(m_UIState.gameObject);
            }
        }
    }
}
