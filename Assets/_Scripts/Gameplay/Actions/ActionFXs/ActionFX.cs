using _Scripts.Gameplay.Configuration;
using _Scripts.Gameplay.GameplayObjects.Character;
using UnityEngine;

namespace _Scripts.Gameplay.Actions.ActionFXs
{
    public enum ActionFXType
    {
        None,
        ReplaceGfx,
        AoE,
        Spawn,
        ReplaceTexture,
        Previsualization
    }
    public enum ActionFXPositionType
    {
        OnDataPosition,
        OnCharacter,
        OnFirePoint,
    }

    public enum ActionFXVisibilityType
    {
        All,        // everyone can see the action
        Others,     // everyone but the caster can see the action
        Ally,       // only ally of the caster can see the action
        Enemy,      // only enemies of the caster can see the action
        Self        // only caster can see the action
    }
    
    /// <summary>
    /// Abstract base class for playing back the visual feedback of an Action.
    /// </summary>
    public abstract class ActionFX : ActionBase
    {
        protected ClientCharacterVisualization m_Parent;

        public ActionDescription.ActionFXDescription ActionFXDescription;

        /// <summary>
        /// True if this actionFX began running immediately, prior to getting a confirmation from the server.
        /// </summary>
        public bool Anticipated { get; protected set; }

        public ActionFX(ref ActionRequestData data, ClientCharacterVisualization parent, ActionDescription.ActionFXDescription actionFXDescription) : base(ref data)
        {
            m_Parent = parent;
            ActionFXDescription = actionFXDescription;
        }

        /// <summary>
        /// Starts the ActionFX. Derived classes may return false if they wish to end immediately without their Update being called.
        /// </summary>
        /// <remarks>
        /// Derived class should be sure to call base.Start() in their implementation, but note that this resets "Anticipated" to false.
        /// </remarks>
        /// <returns>true to play, false to be immediately cleaned up.</returns>
        public virtual bool Start()
        {
            Anticipated = false; //once you start for real you are no longer an anticipated action.
            TimeStarted = Time.time;
            return true;
        }

        public abstract bool Update();

        /// <summary>
        /// End is always called when the ActionFX finishes playing. This is a good place for derived classes to put
        /// wrap-up logic (perhaps playing the "puff of smoke" that rises when a persistent fire AOE goes away). Derived
        /// classes should aren't required to call base.End(); by default, the method just calls 'Cancel', to handle the
        /// common case where Cancel and End do the same thing.
        /// </summary>
        public virtual void End()
        {
            Cancel();
        }

        /// <summary>
        /// Cancel is called when an ActionFX is interrupted prematurely. It is kept logically distinct from End to allow
        /// for the possibility that an Action might want to play something different if it is interrupted, rather than
        /// completing. For example, a "ChargeShot" action might want to emit a projectile object in its End method, but
        /// instead play a "Stagger" animation in its Cancel method.
        /// </summary>
        public virtual void Cancel() { }

        public static ActionFX MakeActionFX(ref ActionRequestData data, ClientCharacterVisualization parent, ActionDescription.ActionFXDescription actionFXDescription)
        {
            switch (actionFXDescription.FXType)
            {
                case ActionFXType.ReplaceGfx: return new ReplaceGfxActionFX(ref data, parent, actionFXDescription);
                case ActionFXType.ReplaceTexture: return new ReplaceTextureActionFX(ref data, parent, actionFXDescription);
                case ActionFXType.AoE: return new AoeActionFX(ref data, parent, actionFXDescription);
                case ActionFXType.Spawn: return new SpawnActionFX(ref data, parent, actionFXDescription);
                
                default: throw new System.NotImplementedException();
            }
        }

        /// <summary>
        /// Called when the visualization receives an animation event.
        /// </summary>
        public virtual void OnAnimEvent(string id) { }

        /// <summary>
        /// Called when this action has finished "charging up". (Which is only meaningful for a
        /// few types of actions -- it is not called for other actions.)
        /// </summary>
        /// <param name="finalChargeUpPercentage"></param>
        public virtual void OnStoppedChargingUp(float finalChargeUpPercentage) { }
        

        /// <summary>
        /// Called when the action is being "anticipated" on the client. For example, if you are the owner of a tank and you swing your hammer,
        /// you get this call immediately on the client, before the server round-trip.
        /// Overriders should always call the base class in their implementation!
        /// </summary>
        public virtual void AnticipateAction()
        {
            Anticipated = true;
            TimeStarted = UnityEngine.Time.time;

            if (!string.IsNullOrEmpty(Description.AnimAnticipation))
            {
                m_Parent.OurAnimator.SetTrigger(Description.AnimAnticipation);
            }
        }
    }
}


