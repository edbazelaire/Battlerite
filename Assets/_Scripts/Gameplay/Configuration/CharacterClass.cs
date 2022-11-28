using _Scripts.Gameplay.GameplayObjects.Character;
using _Scripts.Infrastructure.ScriptableObjectArchitecture;
using UnityEngine;

namespace _Scripts.Gameplay.Configuration
{
    /// <summary>
    /// Data representation of a Character, containing such things as its starting HP and Mana, and what attacks it can do.
    /// </summary>
    [CreateAssetMenu(menuName = "GameData/CharacterClass", order = 1)]
    public class CharacterClass : ScriptableObject
    {
        public ActionDescription[] Abilities;

        [Tooltip("Starting HP of this character class")]
        public IntVariable BaseHP;

        [Tooltip("Total reserve mana of the character")]
        public int ManaReserve;

        [Tooltip("Size of the mana pool")]
        public int ManaPool;

        [Tooltip("Base movement speed of this character class (in meters/sec)")]
        public float Speed;
        
        [Tooltip("For players, this is the displayed \"class name\". (Not used for monsters)")]
        public string DisplayedName;

        public string CharacterName => name;
    }
}
