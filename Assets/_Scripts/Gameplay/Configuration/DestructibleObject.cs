using UnityEngine;

namespace _Scripts.Gameplay.Configuration
{
    [CreateAssetMenu(menuName = "GameData/DestructibleObject")]
    public class DestructibleObject: ScriptableObject
    {
        [Tooltip("max hp of the object")] 
        public int hp;
    }
}