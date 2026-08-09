using System;
using UnityEngine;

namespace TelegGhost.Runtime
{
    public sealed class LevelFlow2D : MonoBehaviour
    {
        [Serializable]
        private struct PulseRoomBinding
        {
            [SerializeField] private GhostPulseController2D pulse;
            [SerializeField] private int checkpointIndex;

            public GhostPulseController2D Pulse => pulse;
            public int CheckpointIndex => checkpointIndex;
        }

        [SerializeField] private PlayerController2D player;
        [SerializeField] private Transform[] checkpointSpawns;
        [SerializeField] private GhostMover2D[] ghosts;
        [SerializeField] private GhostHabitController2D[] ghostHabits;
        [SerializeField] private GhostPulseController2D[] ghostPulses;
        [SerializeField] private PulseRoomBinding[] pulseRoomBindings;
        [SerializeField] private GhostSwitch2D[] ghostSwitches;
        [SerializeField] private DoorController2D[] doors;
        [SerializeField] private GameObject completionDisplay;

        private int activeCheckpoint;

        private void Start()
        {
            ApplyRoomActivation();
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

        public void ActivateCheckpoint(int checkpointIndex)
        {
            if (checkpointSpawns == null || checkpointIndex < 0 || checkpointIndex >= checkpointSpawns.Length)
            {
                return;
            }

            if (checkpointIndex <= activeCheckpoint)
            {
                return;
            }

            activeCheckpoint = checkpointIndex;
            ResetLevelObjects();
            ApplyRoomActivation();
        }

        public void Respawn()
        {
            if (completionDisplay != null)
            {
                completionDisplay.SetActive(false);
            }

            player?.SetInputEnabled(true);

            ResetLevelObjects();
            ApplyRoomActivation();

            if (player != null && checkpointSpawns != null && activeCheckpoint < checkpointSpawns.Length)
            {
                Transform spawn = checkpointSpawns[activeCheckpoint];
                if (spawn != null)
                {
                    player.Teleport(spawn.position);
                }
            }
        }

        private void ResetLevelObjects()
        {

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

            if (ghosts != null)
            {
                for (int i = 0; i < ghosts.Length; i++)
                {
                    ghosts[i]?.ResetState();
                }
            }

            if (ghostHabits != null)
            {
                for (int i = 0; i < ghostHabits.Length; i++)
                {
                    ghostHabits[i]?.ResetState();
                }
            }

            if (ghostPulses != null)
            {
                for (int i = 0; i < ghostPulses.Length; i++)
                {
                    ghostPulses[i]?.ResetState();
                }
            }
        }

        private void ApplyRoomActivation()
        {
            if (pulseRoomBindings == null)
            {
                return;
            }

            for (int i = 0; i < pulseRoomBindings.Length; i++)
            {
                PulseRoomBinding binding = pulseRoomBindings[i];
                GhostPulseController2D pulse = binding.Pulse;
                if (pulse != null)
                {
                    pulse.gameObject.SetActive(binding.CheckpointIndex == activeCheckpoint);
                }
            }
        }

        public void Complete()
        {
            player?.SetInputEnabled(false);
            completionDisplay?.SetActive(true);
        }
    }
}
