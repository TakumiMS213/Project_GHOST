using UnityEngine;

namespace TelegGhost.Runtime
{
    public enum LevelTriggerType
    {
        Checkpoint,
        Respawn,
        Goal
    }

    [RequireComponent(typeof(Collider2D))]
    public sealed class LevelTrigger2D : MonoBehaviour
    {
        [SerializeField] private LevelFlow2D levelFlow;
        [SerializeField] private LevelTriggerType triggerType;
        [SerializeField] private int checkpointIndex;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (levelFlow == null || other == null || other.GetComponentInParent<PlayerController2D>() == null)
            {
                return;
            }

            switch (triggerType)
            {
                case LevelTriggerType.Checkpoint:
                    levelFlow.ActivateCheckpoint(checkpointIndex);
                    break;
                case LevelTriggerType.Respawn:
                    levelFlow.Respawn();
                    break;
                case LevelTriggerType.Goal:
                    levelFlow.Complete();
                    break;
            }
        }
    }
}
