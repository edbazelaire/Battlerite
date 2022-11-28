using _Scripts.Gameplay.GameState;

namespace Unity.Multiplayer.Samples.BossRoom.Client
{

    /// <summary>
    /// Client specialization of core BossRoom game logic.
    /// </summary>
    public class ClientArenaState : GameStateBehaviour
    {
        public override GameState ActiveState { get { return GameState.Arena1; } }


        public override void OnNetworkSpawn()
        {
            if (!IsClient) { this.enabled = false; }
        }

    }

}
