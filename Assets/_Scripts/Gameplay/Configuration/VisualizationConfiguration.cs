using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom
{
    /// <summary>
    /// Describes how a specific character visualization should be animated.
    /// </summary>
    [CreateAssetMenu]
    public class VisualizationConfiguration : ScriptableObject
    {
        [Header("Animation Triggers")]
        [Tooltip("Trigger for when a monster using this visualization becomes dead")]
        [SerializeField] string m_DeadStateTrigger = "Dead";
        [Tooltip("Trigger for when we expect to start moving very soon (to play a short animation in anticipation of moving soon)")]
        [SerializeField] string m_AnticipateMoveTrigger = "AnticipateMove";

        [Header("Abilities Trigger")]
        [Tooltip("Trigger when player casts an ability")]
        [SerializeField] string m_CastTrigger = "cast";

        [Header("Other Animation Variables")]
        [Tooltip("Variable that drives the character's movement animations")]
        [SerializeField] string m_SpeedVariable = "PlayerSpeed";
        [Tooltip("Variable that drives the character's movement animations")]
        [SerializeField] string m_CastSpeedVariable = "CastSpeed";
        [Tooltip("Variable that drives the character's movement animations")]
        [SerializeField] string m_RadDirectionVariable = "RadDirection";
        [Tooltip("Tag that should be on the \"do nothing\" default nodes of each animator layer")]
        [SerializeField] string m_BaseNodeTag = "BaseNode";

        [Header("Animation Speeds")]
        [Tooltip("The animator Speed value when character is dead")]
        public float SpeedDead = 0;
        [Tooltip("The animator Speed value when character is standing idle")]
        public float SpeedIdle = 0;
        [Tooltip("The animator Speed value when character is moving normally")]
        public float SpeedNormal = 1;
        [Tooltip("The animator Speed value when character is being pushed or knocked back")]
        public float SpeedUncontrolled = 0; // no leg movement; character appears to be sliding helplessly
        [Tooltip("The animator Speed value when character is magically slowed")]
        public float SpeedSlowed = 2; // hyper leg movement (character appears to be working very hard to move very little)
        [Tooltip("The animator Speed value when character is magically hasted")]
        public float SpeedHasted = 1.5f;
        [Tooltip("The animator Speed value when character is moving at a slower walking pace")]
        public float SpeedWalking = 0.5f;


        // These are maintained by our OnValidate(). Code refers to these hashed values, not the string versions!
        [SerializeField] [HideInInspector] public int AliveStateTriggerID;
        [SerializeField] [HideInInspector] public int DeadStateTriggerID;
        [SerializeField] [HideInInspector] public int AnticipateMoveTriggerID;

        [SerializeField] [HideInInspector] public int CastTriggerID;
        
        [SerializeField] [HideInInspector] public int SpeedVariableID;
        [SerializeField] [HideInInspector] public int RadDirectionVariableID;
        [SerializeField] [HideInInspector] public int CastSpeedVariableID;
        [SerializeField] [HideInInspector] public int BaseNodeTagID;

        void OnValidate()
        {
            DeadStateTriggerID = Animator.StringToHash(m_DeadStateTrigger);
            AnticipateMoveTriggerID = Animator.StringToHash(m_AnticipateMoveTrigger);

            CastTriggerID = Animator.StringToHash(m_CastTrigger);

            SpeedVariableID = Animator.StringToHash(m_SpeedVariable);
            RadDirectionVariableID = Animator.StringToHash(m_RadDirectionVariable);
            CastSpeedVariableID = Animator.StringToHash(m_CastSpeedVariable);
            BaseNodeTagID = Animator.StringToHash(m_BaseNodeTag);
        }
    }
}
