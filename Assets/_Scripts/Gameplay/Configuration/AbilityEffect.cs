using Unity.VisualScripting.FullSerializer;
using UnityEngine;

namespace _Scripts.Gameplay.Configuration.AbilityEffects
{
    [CreateAssetMenu(menuName = "GameData/AbilityEffect")]
    public class AbilityEffect: ScriptableObject
    {
        [Tooltip("Duration of the effect")] 
        public float duration;

        [Tooltip("Color of the effect when displayed on the client StateBar")] 
        public Color color;
        
        [Tooltip("Animation during the effect")] 
        public string anim;
        
        [Tooltip("Bonus Shield given")] 
        public int shield;
        

        // =============================================================================================================
        [Header("State Effect")]
        [Tooltip("Bonus Movement speed applied to the target (1 = None)")] 
        public float bonusSpeed = 1;
        
        [Tooltip("Bonus Movement speed applied to the target (1 = None)")] 
        public bool stun;
        
        [Tooltip("Bonus Movement speed applied to the target (1 = None)")] 
        public bool petrify;
        
        
        // =============================================================================================================
        [Header("Tick Effect")]
        [Tooltip("Interval of effect proc (time duration between damages, heals...)")] 
        public float tickInterval;
        
        [Tooltip("Damages at each tick")] 
        public int tickDamages;
        
        [Tooltip("Heal at each tick")] 
        public int tickHeal;
        
        [Tooltip("Radius of the heals/damages at each tick of the effect (0 for only on target)")] 
        public int tickRadius;
        
        [Tooltip("Animation at each tick")] 
        public string animTick;
        
        
        // =============================================================================================================
        [Header("Final Effect")]
        [Tooltip("Damages at the end of the duration")] 
        public int finalDamages;

        [Tooltip("Heal at the end of the duration")] 
        public int finalHeal;
        
        [Tooltip("Radius of the heals/damages at the end of the effect (0 for only on target)")] 
        public int finalRadius;
        
        [Tooltip("Animation at the end of the effect")] 
        public string animEnd;
    }
}