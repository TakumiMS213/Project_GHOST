using UnityEngine;

namespace TelegGhost.Runtime.Stage
{
    public sealed class SpawnBridge2D : MonoBehaviour
    {
        [SerializeField] private GhostSwitch2D sourceSwitch;
        [SerializeField] private Collider2D[] bridgeColliders;
        [SerializeField] private Renderer[] bridgeRenderers;

        public bool IsActive { get; private set; }

        private void OnEnable()
        {
            if (sourceSwitch != null)
            {
                sourceSwitch.StateChanged += SetActive;
                SetActive(sourceSwitch.IsActive);
            }
            else
            {
                SetActive(false);
            }
        }

        private void OnDisable()
        {
            if (sourceSwitch != null)
            {
                sourceSwitch.StateChanged -= SetActive;
            }
        }

        public void ResetState()
        {
            SetActive(false);
        }

        public void SetActive(bool active)
        {
            IsActive = active;
            if (bridgeColliders != null)
            {
                for (int i = 0; i < bridgeColliders.Length; i++)
                {
                    if (bridgeColliders[i] != null)
                    {
                        bridgeColliders[i].enabled = active;
                    }
                }
            }

            if (bridgeRenderers != null)
            {
                for (int i = 0; i < bridgeRenderers.Length; i++)
                {
                    if (bridgeRenderers[i] != null)
                    {
                        bridgeRenderers[i].enabled = active;
                    }
                }
            }
        }
    }
}
