using System;
using _Scripts.Gameplay.Input;
using Unity.Netcode;
using UnityEngine;

namespace _Scripts.Gameplay
{
    /// <summary>
    /// List of all Types of Actions. There is a many-to-one mapping of Actions to ActionLogics.
    /// </summary>
    public enum ActionLogic
    {
        LaunchProjectile,
        Toss,
        Melee,
        AoE,
        Emote,
        Shield,
        Dash,
        Jump,
        Counter,
        
        Trample,
        ChargedShield,
        Stunned,
        ChargedLaunchProjectile,
        StealthMode,
        DashAttack,
        //O__O adding a new ActionLogic branch? Update Action.MakeAction!
    }

    /// <summary>
    /// Comprehensive class that contains information needed to play back any action on the server. This is what gets sent client->server when
    /// the Action gets played, and also what gets sent server->client to broadcast the action event. Note that the OUTCOMES of the action effect
    /// don't ride along with this object when it is broadcast to clients; that information is sync'd separately, usually by NetworkVariables.
    /// </summary>
    public struct ActionRequestData : INetworkSerializable
    {
        public string ActionName;       //the action to play.
        public Vector3 Position;                // position of the action.
        public ClientInputSender.SkillTriggerStyle TriggerStyle;    // type of trigger done on this action
        public bool CancelMovement;             // if true, movement is cancelled before playing this action

        //O__O Hey, are you adding something? Be sure to update ActionLogicInfo, as well as the methods below.

        [Flags]
        private enum PackFlags
        {
            None = 0,
            HasTriggerStyle = 1 << 2,
            CancelMovement = 1 << 3,
            //currently serialized with a byte. Change Read/Write if you add more than 8 fields.
        }
        
        private PackFlags GetPackFlags()
        {
            PackFlags flags = PackFlags.None;
            if (TriggerStyle != ClientInputSender.SkillTriggerStyle.None) { flags |= PackFlags.HasTriggerStyle; }
            if (CancelMovement) { flags |= PackFlags.CancelMovement; }
            
            return flags;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            PackFlags flags = PackFlags.None;
            if (!serializer.IsReader)
            {
                flags = GetPackFlags();
            }

            serializer.SerializeValue(ref ActionName);
            serializer.SerializeValue(ref Position);
            serializer.SerializeValue(ref flags);

            if (serializer.IsReader)
            {
                CancelMovement = (flags & PackFlags.CancelMovement) != 0;
            }
            
            if ((flags & PackFlags.HasTriggerStyle) != 0)
            {
                serializer.SerializeValue(ref TriggerStyle);
            }
        }
    }
}
