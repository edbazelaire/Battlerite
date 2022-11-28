using System;

namespace _Scripts.Gameplay.GameplayObjects.Abilities
{
    public enum StateEffectType
    {
        Normal,
        Slow, 
        Silence, 
        Stun, 
        AirBorn, 
        Petrify,
        Freez
    }
    
    [Serializable]
    public class StateEffect
    {
        public StateEffectType type;
        public float duration;
        public float fromValue;
        public float toValue;
        public float delay;
        public int shield;

        public StateEffect(StateEffectType _type, float _duration, float _fromValue, float _toValue, float _delay,
            int _shield)
        {
            type = _type;
            duration = _duration;
            fromValue = _fromValue;
            toValue = _toValue;
            delay = _delay;
            shield = _shield;
        }

        public static StateEffect Silence(float duration)
        {
            return new StateEffect(
                StateEffectType.Silence,
                duration,
                0,
                0,
                0,
                0
            );
        }
        
        public static StateEffect Stun(float duration)
        {
            return new StateEffect(
                StateEffectType.Stun,
                duration,
                0,
                0,
                0,
                0
            );
        }

        public static StateEffect Petrify(float duration, float delay, int shield)
        {
            return new StateEffect(
                StateEffectType.Petrify,
                duration,
                0,
                0,
                delay,
                shield
            );
        }

        public static StateEffect Freeze(float duration, int shield)
        {
            return new StateEffect(
                StateEffectType.Freez,
                duration,
                0,
                0,
                0,
                shield
            );
        }
    }
}