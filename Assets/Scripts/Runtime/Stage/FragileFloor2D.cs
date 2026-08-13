using UnityEngine;

namespace TelegGhost.Runtime.Stage
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class FragileFloor2D : MonoBehaviour
    {
        [SerializeField] private Collider2D floorCollider;
        [SerializeField] private Renderer[] floorRenderers;
        [SerializeField] private LayerMask giantMask;
        [SerializeField] private float breakDelay = 0.1f;

        private readonly Collider2D[] overlapResults = new Collider2D[8];
        private bool breakScheduled;
        private float remainingDelay;

        public bool IsBroken { get; private set; }

        private void Awake()
        {
            if (floorCollider == null)
            {
                floorCollider = GetComponent<Collider2D>();
            }
        }

        private void FixedUpdate()
        {
            if (IsBroken || floorCollider == null)
            {
                return;
            }

            if (!breakScheduled && IsTouchingGiant())
            {
                breakScheduled = true;
                remainingDelay = Mathf.Max(0f, breakDelay);
            }

            if (!breakScheduled)
            {
                return;
            }

            remainingDelay -= Time.fixedDeltaTime;
            if (remainingDelay <= 0f)
            {
                BreakNow();
            }
        }

        public void ResetState()
        {
            breakScheduled = false;
            remainingDelay = 0f;
            IsBroken = false;
            SetEnabled(true);
        }

        public void BreakNow()
        {
            breakScheduled = false;
            IsBroken = true;
            SetEnabled(false);
        }

        private bool IsTouchingGiant()
        {
            Bounds bounds = floorCollider.bounds;
            int count = Physics2D.OverlapBoxNonAlloc(
                bounds.center,
                bounds.size,
                0f,
                overlapResults,
                giantMask);
            for (int i = 0; i < count; i++)
            {
                Collider2D candidate = overlapResults[i];
                if (candidate != null && candidate.GetComponentInParent<GiantTele2D>() != null)
                {
                    return true;
                }
            }
            return false;
        }

        private void SetEnabled(bool enabledState)
        {
            if (floorCollider != null)
            {
                floorCollider.enabled = enabledState;
            }
            if (floorRenderers == null)
            {
                return;
            }

            for (int i = 0; i < floorRenderers.Length; i++)
            {
                if (floorRenderers[i] != null)
                {
                    floorRenderers[i].enabled = enabledState;
                }
            }
        }
    }
}
