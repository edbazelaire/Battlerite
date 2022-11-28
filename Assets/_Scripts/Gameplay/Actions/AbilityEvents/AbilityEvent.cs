using Unity.VisualScripting;

namespace _Scripts.Gameplay.Actions.AbilityEvents
{
    public class AbilityEvents
    {
        protected virtual void OnStart() {}
        protected virtual void OnActivation() {}
        
        protected virtual void OnCollision() {}
        
        protected virtual void OnEnd() {}
    }
}