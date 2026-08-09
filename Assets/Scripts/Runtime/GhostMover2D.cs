using System.Collections.Generic;
using UnityEngine;

namespace TelegGhost.Runtime
{
    public enum ObservationType
    {
        Tele,
        Star
    }

    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public sealed class GhostMover2D : MonoBehaviour
    {
        [SerializeField] private ObservationType observationType;
        [SerializeField] private PlayerVision2D playerVision;
        [SerializeField] private Collider2D observationCollider;
        [SerializeField] private Vector2 moveDirection = Vector2.right;
        [SerializeField] private float moveDistance = 4f;
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private bool rideable;
        [SerializeField] private Renderer[] visibilityRenderers;
        [SerializeField] private LineRenderer pathLine;
        [SerializeField] private Transform destinationMarker;

        private readonly HashSet<Rigidbody2D> riders = new HashSet<Rigidbody2D>();
        private Rigidbody2D cachedRigidbody;
        private Vector2 startPosition;
        private Vector2 endPosition;
        private Vector2 normalizedDirection;
        private bool isObserved;

        public bool IsObserved => isObserved;
        public ObservationType ObservationType => observationType;
        public Vector2 EndPosition => endPosition;

        private void Awake()
        {
            cachedRigidbody = GetComponent<Rigidbody2D>();
            observationCollider ??= GetComponent<Collider2D>();
            normalizedDirection = moveDirection.sqrMagnitude > 0.001f ? moveDirection.normalized : Vector2.right;
            startPosition = transform.position;
            endPosition = startPosition + normalizedDirection * moveDistance;

            if (visibilityRenderers == null || visibilityRenderers.Length == 0)
            {
                visibilityRenderers = GetComponentsInChildren<Renderer>(true);
            }

            UpdatePathVisual();
        }

        private void Start()
        {
            EvaluateObservation();
        }

        private void Update()
        {
            EvaluateObservation();
            UpdatePathVisual();
        }

        private void FixedUpdate()
        {
            if (cachedRigidbody == null || !ShouldMove(observationType, isObserved))
            {
                return;
            }

            Vector2 current = cachedRigidbody.position;
            float progress = Vector2.Dot(current - startPosition, normalizedDirection);
            if (progress >= moveDistance - 0.001f)
            {
                cachedRigidbody.MovePosition(endPosition);
                return;
            }

            float step = Mathf.Min(moveSpeed * Time.fixedDeltaTime, moveDistance - progress);
            Vector2 delta = normalizedDirection * step;

            if (rideable)
            {
                foreach (Rigidbody2D rider in riders)
                {
                    if (rider != null)
                    {
                        rider.position += delta;
                    }
                }
            }

            cachedRigidbody.MovePosition(current + delta);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            if (!rideable || collision == null || observationCollider == null)
            {
                return;
            }

            PlayerController2D player = collision.collider != null
                ? collision.collider.GetComponentInParent<PlayerController2D>()
                : null;
            Rigidbody2D rider = collision.rigidbody;
            if (player != null && rider != null && rider.position.y > observationCollider.bounds.center.y)
            {
                riders.Add(rider);
            }
        }

        private void OnCollisionExit2D(Collision2D collision)
        {
            if (collision?.rigidbody != null)
            {
                riders.Remove(collision.rigidbody);
            }
        }

        public static bool ShouldMove(ObservationType type, bool observed)
        {
            return type == ObservationType.Tele ? !observed : observed;
        }

        public void ResetState()
        {
            riders.Clear();
            if (cachedRigidbody != null)
            {
                cachedRigidbody.position = startPosition;
                cachedRigidbody.linearVelocity = Vector2.zero;
            }

            transform.position = startPosition;
            EvaluateObservation();
            UpdatePathVisual();
        }

        private void EvaluateObservation()
        {
            isObserved = playerVision != null && playerVision.IsVisible(observationCollider);
            if (visibilityRenderers == null)
            {
                return;
            }

            for (int i = 0; i < visibilityRenderers.Length; i++)
            {
                Renderer targetRenderer = visibilityRenderers[i];
                if (targetRenderer != null)
                {
                    targetRenderer.enabled = isObserved;
                }
            }
        }

        private void UpdatePathVisual()
        {
            if (pathLine != null)
            {
                pathLine.positionCount = 2;
                pathLine.SetPosition(0, transform.position);
                pathLine.SetPosition(1, endPosition);
            }

            if (destinationMarker != null)
            {
                destinationMarker.position = endPosition;
            }
        }
    }
}
