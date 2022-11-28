using System;
using _Scripts.Gameplay.Actions;
using _Scripts.Gameplay.Actions.ActionFXs;
using _Scripts.Gameplay.Configuration.AbilityEffects;
using _Scripts.Gameplay.GameplayObjects.Abilities;
using _Scripts.Gameplay.GameplayObjects.Abilities.Previsualisations;
using UnityEngine;

namespace _Scripts.Gameplay.Configuration
{
    /// <summary>
    /// Data description of a single Action, including the information to visualize it (animations etc), and the information
    /// to play it back on the server.
    /// </summary>
    [CreateAssetMenu(menuName = "GameData/ActionDescription", order = 1)]
    public class ActionDescription : ScriptableObject
    {
        [Tooltip("ActionLogic that drives this Action. This corresponds to the actual block of code that executes it.")]
        public ActionLogic Logic;
        
        [Tooltip("If this Action spawns a projectile, describes it. (\"Charged\" projectiles can list multiple possible shots, ordered from weakest to strongest)")]
        public GameObject Prefab;

        // =============================================================================================================
        [Header("Ability Properties")]
        
        [Tooltip("Mana required to perform the Ability")]
        public int ManaCost;

        [Tooltip("Damage dealt by the ability")]
        public int Damage;
        
        [Tooltip("Heal dealt by the ability")]
        public int Heal;
        
        [Tooltip("The radius of effect at the activation of this action. Default is 0 if not needed")]
        public float RadiusStart;
        
        [Tooltip("The radius of effect for this action. Default is 0 if not needed")]
        public float Radius;

        [Tooltip("Speed in m/s of the spell (only used in projectiles and jumps)")]
        public float Speed;
        
        [Tooltip("Maximum number of victim that the spell can hit (0 = no max victims)")]
        public int MaxVictims;
        
        [Tooltip("Ignore walls during collision check")]
        public bool IgnoreWalls;
        
        [Tooltip("Does the action prevent player from moving ?")]
        public bool CancelMovement;
        
        [Tooltip("Does the action prevent player from rotating ?")]
        public bool CancelRotation;
        

        // =============================================================================================================
        [Header("Range")]
        
        [Tooltip("Minimum distance of the action")]
        public float MinRange;
        
        [Tooltip("How far the Action performer can be from the Target")]
        public float MaxRange;
        
        [Tooltip("Is action position located on mouse position")]
        public bool IsOnMousePosition;


        // =============================================================================================================
        [Header("Timers & Activations properties")]
        
        [Tooltip("Time when the Action should do its \"main thing\" (e.g. when a melee attack should apply damage")]
        public float ExecTimeSeconds;

        [Tooltip("Time it takes for the action to proc its effect after activation")] 
        public float ActivationDelay;
        
        [Tooltip("Duration in seconds that this Action takes to play")]
        public float DurationSeconds;

        [Tooltip("After this Action is successfully started, the server will discard any attempts to perform it again until this amount of time has elapsed.")]
        public float ReuseTimeSeconds;

        [Tooltip("Is this Action interruptible by other action-plays or by movement? (Implicitly stops movement when action starts.) Generally, actions with short exec times should not be interruptible in this way.")]
        public bool ActionInterruptible;
        
        [Serializable]
        public enum BlockingModeType
        {
            EntireDuration,
            OnlyDuringExecTime,
        }
        [Tooltip("Indicates how long this action blocks other actions from happening: during the execution stage, or for as long as it runs?")]
        public BlockingModeType BlockingMode;
        
        
        // =============================================================================================================
        [Header("Animations & Effects")]
        
        [Tooltip("The Anticipation Animation trigger that gets raised when user starts using this Action, but while the server confirmation hasn't returned")]
        public string AnimAnticipation;

        [Tooltip("The primary Animation trigger that gets raised when visualizing this Action")]
        public string Anim;

        [Tooltip("The auxiliary Animation trigger for this Action (e.g. to end an animation loop)")]
        public string CancelAnimation;

        [Tooltip("Spawn to allowed clients during previsualisation")]
        public AbilityPrevisualization PrevisualisationPrefab;

        [Serializable]
        public struct ActionFXDescription
        {
            [Tooltip("Prefab of the Object / VFX that will spawn on client side")]
            public GameObject SpawnPrefab;

            [Tooltip("State when this object is spawning")]
            public ActionPlayer.ActionState ActionStateStart;

            [Tooltip("Type of display")] 
            public ActionFXType FXType;

            [Tooltip("Where the object is supposed to be positioned")] 
            public ActionFXPositionType PositionType;

            [Tooltip("Who can see this action")] 
            public ActionFXVisibilityType VisibilityType;

            [Tooltip("Can the action be cancelled (by ActionState changes, cancel requests, interruptions...")] 
            public bool IsInterruptible;
        }
        
        [Tooltip("Objects / VFX effects that are spawned on clients during different states of the action")]
        public ActionFXDescription[] Spawns;
        
        
        // =============================================================================================================
        [Header("On Hit Effects")]
        
        [Tooltip("Travelled speed during knock back")]
        public float KnockBackSpeed;
        
        [Tooltip("Distance of the knock back")]
        public float KnockBackDistance;
        
        [Serializable]
        public struct AbilityEffectDuration
        {
            [Tooltip("Ability effect that is applied")]
            public AbilityEffect AbilityEffect;

            [Tooltip("Duration of the effect that is applied (0 for default max duration)")]
            public float Duration;
        }
        [Tooltip("List of effects that will be apply on the target on hit")]
        public AbilityEffectDuration[] AbilityEffects;
        
        [Tooltip("List of effects impacting state of the target on hit (stun, slow, ...)")]
        public StateEffect[] StateEffects;
        

        // =============================================================================================================
        [Header("In-game description info (Only used for player abilities!)")]
        
        [Tooltip("If this Action describes a player ability, this is the ability's iconic representation")]
        public Sprite Icon;

        [Tooltip("If this Action describes a player ability, this is the name we show for the ability")]
        public string DisplayedName;

        [Tooltip("If this Action describes a player ability, this is the tooltip description we show for the ability")]
        [Multiline]
        public string Description;

        // Name of the action (set as prop if we need to apply modification on basic name)
        public string ActionName => name;
        

        // =============================================================================================================
        // ACTION STATES : function activated at different states
        public virtual bool Start() { return true; }
        
        public virtual void Activate() { }
        
        public virtual bool Update() { return true; }
        
        public virtual void End() { }
    }
}

