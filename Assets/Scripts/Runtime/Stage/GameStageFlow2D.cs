using UnityEngine;
using UnityEngine.SceneManagement;

namespace TelegGhost.Runtime.Stage
{
    public sealed class GameStageFlow2D : MonoBehaviour
    {
        [SerializeField] private PlayerController2D player;
        [SerializeField] private Transform respawnPoint;
        [SerializeField] private float playerFootOffset = 0.8f;
        [SerializeField] private GhostPulseController2D[] ghostPulses;
        [SerializeField] private GhostSwitch2D[] ghostSwitches;
        [SerializeField] private DoorController2D[] doors;
        [SerializeField] private SpawnBridge2D[] bridges;
        [SerializeField] private FragileFloor2D[] fragileFloors;
        [SerializeField] private string nextSceneName;

        private bool isCompleting;
        private Vector2 currentRespawnFootPosition;

        public Vector2 CurrentRespawnFootPosition => currentRespawnFootPosition;

        private void Awake()
        {
            currentRespawnFootPosition = respawnPoint != null
                ? (Vector2)respawnPoint.position
                : Vector2.zero;
        }

        private void OnEnable()
        {
            if (player != null)
            {
                player.ResetRequested += Respawn;
            }
        }

        private void OnDisable()
        {
            if (player != null)
            {
                player.ResetRequested -= Respawn;
            }
        }

        public void Respawn()
        {
            isCompleting = false;
            player?.SetInputEnabled(true);

            if (fragileFloors != null)
            {
                for (int i = 0; i < fragileFloors.Length; i++)
                {
                    fragileFloors[i]?.ResetState();
                }
            }

            if (bridges != null)
            {
                for (int i = 0; i < bridges.Length; i++)
                {
                    bridges[i]?.ResetState();
                }
            }

            if (ghostSwitches != null)
            {
                for (int i = 0; i < ghostSwitches.Length; i++)
                {
                    ghostSwitches[i]?.ResetState();
                }
            }

            if (doors != null)
            {
                for (int i = 0; i < doors.Length; i++)
                {
                    doors[i]?.ResetState();
                }
            }

            if (ghostPulses != null)
            {
                for (int i = 0; i < ghostPulses.Length; i++)
                {
                    ghostPulses[i]?.ResetState();
                }
            }

            if (player != null)
            {
                player.ResetView();
                player.Teleport(currentRespawnFootPosition + Vector2.up * playerFootOffset);
            }
        }

        public void SetCheckpoint(Vector2 footPosition)
        {
            currentRespawnFootPosition = footPosition;
        }

        public void CompleteStage()
        {
            if (isCompleting || string.IsNullOrWhiteSpace(nextSceneName))
            {
                return;
            }

            isCompleting = true;
            player?.SetInputEnabled(false);
            SceneManager.LoadScene(nextSceneName);
        }
    }
}
