using UnityEngine;

namespace TelegGhost.Runtime.Stage
{
    public enum GameStageTriggerType
    {
        Respawn,
        Complete
    }

    [RequireComponent(typeof(Collider2D))]
    public sealed class GameStageTrigger2D : MonoBehaviour
    {
        [SerializeField] private GameStageFlow2D stageFlow;
        [SerializeField] private GameStageTriggerType triggerType;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (stageFlow == null || other == null || other.GetComponentInParent<PlayerController2D>() == null)
            {
                return;
            }

            if (triggerType == GameStageTriggerType.Respawn)
            {
                stageFlow.Respawn();
            }
            else
            {
                stageFlow.CompleteStage();
            }
        }
    }
}
