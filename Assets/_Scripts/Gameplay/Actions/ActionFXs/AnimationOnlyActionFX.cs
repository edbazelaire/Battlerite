using _Scripts.Gameplay.Configuration;
using _Scripts.Gameplay.GameplayObjects.Character;

namespace _Scripts.Gameplay.Actions.ActionFXs
{
    /// <summary>
    /// Used for simple Actions that only need to play a few animations (one at startup and optionally
    /// one at end). Lasts a fixed duration as specified in the ActionDescription
    /// </summary>
    public class AnimationOnlyActionFX : ActionFX
    {
        public AnimationOnlyActionFX(ref ActionRequestData data, ClientCharacterVisualization parent) : base(ref data, parent, new ActionDescription.ActionFXDescription()) { }

        public override bool Update()
        {
            return true;
        }
    }
}
