using UnityEngine;

namespace TelegGhost.Runtime
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class DoorController2D : MonoBehaviour
    {
        [SerializeField] private Collider2D blockingCollider;
        [SerializeField] private GameObject closedVisual;

        public bool IsOpen { get; private set; }

        private void Awake()
        {
            if (blockingCollider == null)
            {
                blockingCollider = GetComponent<Collider2D>();
            }
            ApplyState();
        }

        public void SetOpen(bool shouldOpen)
        {
            if (IsOpen == shouldOpen)
            {
                return;
            }

            IsOpen = shouldOpen;
            ApplyState();
        }

        public void ResetState()
        {
            IsOpen = false;
            ApplyState();
        }

        private void ApplyState()
        {
            if (blockingCollider != null)
            {
                blockingCollider.enabled = !IsOpen;
            }
            if (closedVisual != null)
            {
                closedVisual.SetActive(!IsOpen);
            }
        }
    }
}
