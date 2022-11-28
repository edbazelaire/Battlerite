using UnityEngine;

namespace _Scripts.Utils
{
    public static class KeyBindingPrefs
    {
        public static KeyCode MoveUp = KeyCode.Z;
        public static KeyCode MoveDown = KeyCode.S;
        public static KeyCode MoveLeft = KeyCode.Q;
        public static KeyCode MoveRight = KeyCode.D;
        
        public static KeyCode Ability1 = KeyCode.Mouse0;
        public static KeyCode Ability2 = KeyCode.Mouse1;
        public static KeyCode Ability3 = KeyCode.Space;
        public static KeyCode Ability4 = KeyCode.A;
        public static KeyCode Ability5 = KeyCode.E;
        public static KeyCode Ability6 = KeyCode.R;
        public static KeyCode Ability7 = KeyCode.F;

        public static KeyCode ManaFlowLevel1 = KeyCode.Alpha1;
        public static KeyCode ManaFlowLevel2 = KeyCode.Alpha2;
        public static KeyCode ManaFlowLevel3 = KeyCode.Alpha3;
        public static KeyCode ManaFlowLevel4 = KeyCode.Alpha4;
        public static KeyCode ManaFlowLevel5 = KeyCode.Alpha5;
        
        public static KeyCode CancelAbility1 = KeyCode.C;
        public static KeyCode CancelAbility2 = KeyCode.Mouse5;
        public static KeyCode HexAbilities = KeyCode.LeftShift;
        
        public static KeyCode[] AbilitiesKeys {
            get
            {
                KeyCode[] abilities = { Ability1, Ability2, Ability3, Ability4, Ability5, Ability6, Ability7 };
                return abilities;
            }
        }
        public static KeyCode[] ManaFlowLevelKeys {
            get
            {
                KeyCode[] keys = { ManaFlowLevel1, ManaFlowLevel2, ManaFlowLevel3, ManaFlowLevel4, ManaFlowLevel5 };
                return keys;
            }
        }
    }
}