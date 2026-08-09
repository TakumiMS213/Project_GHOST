using UnityEngine;

namespace TelegGhost.Runtime
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class DoorController2D : MonoBehaviour
    {
        [SerializeField] private Vector2 openOffset = Vector2.up * 3f;
        [SerializeField] private float moveSpeed = 4f;

        private Rigidbody2D cachedRigidbody;
        private Vector2 closedPosition;
        private bool isOpen;

        private void Awake()
        {
            cachedRigidbody = GetComponent<Rigidbody2D>();
            if (cachedRigidbody == null)
            {
                enabled = false;
                return;
            }

            closedPosition = transform.position;
        }

        private void FixedUpdate()
        {
            if (cachedRigidbody == null)
            {
                return;
            }

            Vector2 target = isOpen ? closedPosition + openOffset : closedPosition;
            cachedRigidbody.MovePosition(Vector2.MoveTowards(cachedRigidbody.position, target, moveSpeed * Time.fixedDeltaTime));
        }

        public void SetOpen(bool shouldOpen)
        {
            isOpen = shouldOpen;
        }

        public void ResetState()
        {
            isOpen = false;
            if (cachedRigidbody != null)
            {
                cachedRigidbody.position = closedPosition;
                cachedRigidbody.linearVelocity = Vector2.zero;
            }
        }
    }
}
