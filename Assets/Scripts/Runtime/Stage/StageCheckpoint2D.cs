using UnityEngine;

namespace TelegGhost.Runtime.Stage
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class StageCheckpoint2D : MonoBehaviour
    {
        [SerializeField] private GameStageFlow2D stageFlow;
        [SerializeField] private SpriteRenderer indicatorRenderer;
        [SerializeField] private GameObject[] activateTargets;
        [SerializeField] private Color inactiveColor = new Color(0.3f, 0.32f, 0.36f, 1f);
        [SerializeField] private Color activeColor = Color.white;

        private bool activated;

        public bool IsActivated => activated;

        private void Awake()
        {
            if (indicatorRenderer != null)
            {
                indicatorRenderer.color = inactiveColor;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (activated || other == null || other.GetComponentInParent<PlayerController2D>() == null)
            {
                return;
            }

            activated = true;
            if (indicatorRenderer != null)
            {
                indicatorRenderer.color = activeColor;
            }
            if (stageFlow != null)
            {
                stageFlow.SetCheckpoint(transform.position);
            }
            if (activateTargets != null)
            {
                for (int i = 0; i < activateTargets.Length; i++)
                {
                    if (activateTargets[i] != null)
                    {
                        activateTargets[i].SetActive(true);
                    }
                }
            }
        }
    }
}
