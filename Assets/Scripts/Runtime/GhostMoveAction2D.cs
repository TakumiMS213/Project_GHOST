using System.Collections.Generic;
using UnityEngine;

namespace TelegGhost.Runtime
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public sealed class GhostMoveAction2D : GhostAction2D
    {
        private enum MovePhase
        {
            Idle,
            Outbound,
            Impact,
            Return
        }

        [SerializeField] private Rigidbody2D controlledBody;
        [SerializeField] private Collider2D movementCollider;
        [SerializeField] private Vector2 actionDirection = Vector2.right;
        [SerializeField] private float gridSize = 1.5f;
        [SerializeField] private float moveDuration = 0.14f;
        [SerializeField] private float impactHoldDuration = 0.05f;
        [SerializeField] private LayerMask collisionMask;
        [SerializeField] private bool rideable;

        private readonly HashSet<Rigidbody2D> riders = new HashSet<Rigidbody2D>();
        private readonly RaycastHit2D[] castResults = new RaycastHit2D[1];
        private ContactFilter2D collisionFilter;
        private MovePhase phase;
        private Vector2 resetPosition;
        private Vector2 actionStart;
        private Vector2 actionTarget;
        private Vector2 actionPosition;
        private float phaseProgress;
        private float impactTimer;
        private bool returnsAfterImpact;

        public Vector2 ActionDirection => actionDirection;
        public float GridSize => gridSize;
        public bool Rideable => rideable;

        private void Awake()
        {
            controlledBody ??= GetComponent<Rigidbody2D>();
            movementCollider ??= GetComponent<Collider2D>();
            resetPosition = controlledBody != null ? controlledBody.position : (Vector2)transform.position;
            actionPosition = resetPosition;

            collisionFilter = new ContactFilter2D();
            collisionFilter.SetLayerMask(collisionMask);
            collisionFilter.useTriggers = false;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            TryRegisterRider(collision);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            TryRegisterRider(collision);
        }

        private void TryRegisterRider(Collision2D collision)
        {
            if (!rideable || collision == null || movementCollider == null)
            {
                return;
            }

            PlayerController2D player = collision.collider != null
                ? collision.collider.GetComponentInParent<PlayerController2D>()
                : null;
            Rigidbody2D rider = collision.rigidbody;
            if (player != null && rider != null && rider.position.y > movementCollider.bounds.center.y)
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

        public override void BeginAction()
        {
            if (IsExecuting || controlledBody == null || movementCollider == null)
            {
                return;
            }

            Vector2 direction = actionDirection.sqrMagnitude > 0.001f
                ? actionDirection.normalized
                : Vector2.right;
            actionStart = controlledBody.position;
            actionPosition = actionStart;
            float travelDistance = Mathf.Max(0f, gridSize);
            int hitCount = movementCollider.Cast(
                direction,
                collisionFilter,
                castResults,
                travelDistance);

            returnsAfterImpact = hitCount > 0;
            if (returnsAfterImpact)
            {
                travelDistance = Mathf.Clamp(castResults[0].distance - 0.015f, 0f, travelDistance);
            }

            actionTarget = actionStart + direction * travelDistance;
            phase = MovePhase.Outbound;
            phaseProgress = 0f;
            impactTimer = 0f;
            IsExecuting = true;
        }

        public override void TickAction(float deltaTime)
        {
            if (!IsExecuting || controlledBody == null)
            {
                return;
            }

            switch (phase)
            {
                case MovePhase.Outbound:
                    TickOutbound(deltaTime);
                    break;
                case MovePhase.Impact:
                    impactTimer += deltaTime;
                    if (impactTimer >= impactHoldDuration)
                    {
                        phase = MovePhase.Return;
                        phaseProgress = 0f;
                    }
                    break;
                case MovePhase.Return:
                    phaseProgress = Mathf.Min(1f, phaseProgress + deltaTime / Mathf.Max(0.01f, moveDuration));
                    MoveTo(Vector2.Lerp(actionTarget, actionStart, EaseSharp(phaseProgress)));
                    if (phaseProgress >= 1f)
                    {
                        CompleteAction();
                    }
                    break;
            }
        }

        public override void ResetAction()
        {
            riders.Clear();
            IsExecuting = false;
            phase = MovePhase.Idle;
            actionPosition = resetPosition;
            if (controlledBody != null)
            {
                controlledBody.position = resetPosition;
                controlledBody.linearVelocity = Vector2.zero;
            }

            transform.position = resetPosition;
        }

        private void TickOutbound(float deltaTime)
        {
            phaseProgress = Mathf.Min(1f, phaseProgress + deltaTime / Mathf.Max(0.01f, moveDuration));
            MoveTo(Vector2.Lerp(actionStart, actionTarget, EaseSharp(phaseProgress)));
            if (phaseProgress < 1f)
            {
                return;
            }

            if (returnsAfterImpact)
            {
                phase = MovePhase.Impact;
                impactTimer = 0f;
                return;
            }

            CompleteAction();
        }

        private void MoveTo(Vector2 position)
        {
            Vector2 delta = position - actionPosition;
            actionPosition = position;
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

            controlledBody.MovePosition(position);
        }

        private void CompleteAction()
        {
            phase = MovePhase.Idle;
            IsExecuting = false;
        }

        private static float EaseSharp(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }
    }
}
