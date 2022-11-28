using UnityEngine;

namespace _Scripts.Gameplay.GameplayObjects
{
    public class TeamHandler: MonoBehaviour
    {
        public static TeamHandler Instance { get; private set; }
        public static int CurrentTeamValue;
        
        private void Awake()
        {
            if (Instance != null)
            {
                throw new System.Exception("Multiple TeamHandler defined!");
            }

            DontDestroyOnLoad(gameObject);
            Instance = this;
        }

        public int GetTeam()
        {
            CurrentTeamValue += 1;
            return CurrentTeamValue;
        }
    }
}