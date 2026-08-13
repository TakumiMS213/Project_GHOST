using UnityEngine;

namespace TelegGhost.Runtime.Stage
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class StageGhostActivation2D : MonoBehaviour
    {
        [SerializeField] private GameObject[] targets;

        private Collider2D activationCollider;
        private bool activated;

        private void Awake()
        {
            activationCollider = GetComponent<Collider2D>();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (activated || other == null || other.GetComponentInParent<PlayerController2D>() == null)
            {
                return;
            }

            activated = true;
            SetTargetsActive(true);
            if (activationCollider != null)
            {
                activationCollider.enabled = false;
            }
        }

        public void ResetState()
        {
            activated = false;
            SetTargetsActive(false);
            if (activationCollider != null)
            {
                activationCollider.enabled = true;
            }
        }

        private void SetTargetsActive(bool active)
        {
            if (targets == null)
            {
                return;
            }

            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] != null)
                {
                    targets[i].SetActive(active);
                }
            }
        }
    }
}
