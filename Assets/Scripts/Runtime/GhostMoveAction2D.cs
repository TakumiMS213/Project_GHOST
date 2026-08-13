using System.Collections.Generic;
using UnityEngine;

namespace TelegGhost.Runtime
{
    public enum GhostMoveRouteMode
    {
        PingPong,
        OneWay
    }

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
        [SerializeField] private Vector2[] pathNodes;
        [SerializeField] private GhostMoveRouteMode routeMode = GhostMoveRouteMode.PingPong;
        [SerializeField] private Transform directionIndicator;
        [SerializeField] private float moveDurationPerUnit = 0.15f;
        [SerializeField] private float impactHoldDuration = 0.05f;
        [SerializeField] private LayerMask collisionMask;
        [SerializeField] private bool rideable;

        // Legacy fallback for old mockup prefabs. Production stages author pathNodes.
        [SerializeField] private Vector2 actionDirection = Vector2.right;
        [SerializeField, Min(1)] private int pathGridCount = 3;
        [SerializeField] private float gridSize = 1f;

        private readonly HashSet<Rigidbody2D> riders = new HashSet<Rigidbody2D>();
        private readonly RaycastHit2D[] castResults = new RaycastHit2D[8];
        private ContactFilter2D collisionFilter;
        private MovePhase phase;
        private Vector2 resetPosition;
        private Vector2 actionStart;
        private Vector2 actionTarget;
        private Vector2 actionPosition;
        private float phaseProgress;
        private float phaseDuration;
        private float impactTimer;
        private bool returnsAfterImpact;
        private int targetNodeIndex;
        private int currentNodeIndex;
        private int travelSign = 1;
        private float indicatorDistance;
        private float indicatorDepth;

        public Vector2 ActionDirection => GetNextDirection();
        public float GridSize => 1f;
        public bool Rideable => rideable;
        public GhostMoveRouteMode RouteMode => routeMode;
        public int PathGridCount => Mathf.Max(0, PathNodeCount - 1);
        public int PathNodeCount => pathNodes != null ? pathNodes.Length : 0;
        public int CurrentGridIndex => currentNodeIndex;
        public int CurrentNodeIndex => currentNodeIndex;
        public IReadOnlyList<Vector2> PathNodes => pathNodes;

        private void Awake()
        {
            if (controlledBody == null)
            {
                controlledBody = GetComponent<Rigidbody2D>();
            }
            if (movementCollider == null)
            {
                movementCollider = GetComponent<Collider2D>();
            }

            EnsurePath();
            resetPosition = pathNodes[0];
            actionPosition = resetPosition;
            currentNodeIndex = 0;
            travelSign = 1;
            if (controlledBody != null)
            {
                controlledBody.position = resetPosition;
            }

            if (directionIndicator != null)
            {
                Vector3 localPosition = directionIndicator.localPosition;
                indicatorDistance = new Vector2(localPosition.x, localPosition.y).magnitude;
                indicatorDepth = localPosition.z;
            }

            collisionFilter = new ContactFilter2D();
            collisionFilter.SetLayerMask(collisionMask);
            collisionFilter.useTriggers = false;
            UpdateDirectionIndicator();
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            TryRegisterRider(collision);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            TryRegisterRider(collision);
        }

        private void OnCollisionExit2D(Collision2D collision)
        {
            if (collision != null && collision.rigidbody != null)
            {
                riders.Remove(collision.rigidbody);
            }
        }

        public override void BeginAction()
        {
            if (IsExecuting || controlledBody == null || movementCollider == null || pathNodes.Length < 2)
            {
                return;
            }

            if (!TryGetNextNodeIndex(out targetNodeIndex))
            {
                return;
            }

            actionStart = controlledBody.position;
            actionPosition = actionStart;
            Vector2 fullTarget = pathNodes[targetNodeIndex];
            Vector2 offset = fullTarget - actionStart;
            float fullDistance = offset.magnitude;
            if (fullDistance <= 0.001f)
            {
                CommitSuccessfulRouteStep();
                return;
            }

            Vector2 direction = offset / fullDistance;
            int hitCount = movementCollider.Cast(direction, collisionFilter, castResults, fullDistance);
            int blockingHitIndex = FindBlockingHit(direction, hitCount);
            returnsAfterImpact = blockingHitIndex >= 0;
            float travelDistance = fullDistance;
            if (returnsAfterImpact)
            {
                travelDistance = Mathf.Clamp(
                    castResults[blockingHitIndex].distance - 0.015f,
                    0f,
                    fullDistance);
            }

            actionTarget = actionStart + direction * travelDistance;
            phaseDuration = moveDurationPerUnit * Mathf.Max(0.01f, travelDistance);
            phaseProgress = 0f;
            impactTimer = 0f;
            phase = MovePhase.Outbound;
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
                    impactTimer += Mathf.Max(0f, deltaTime);
                    if (impactTimer >= impactHoldDuration)
                    {
                        phase = MovePhase.Return;
                        phaseProgress = 0f;
                    }
                    break;
                case MovePhase.Return:
                    phaseProgress = Mathf.Min(
                        1f,
                        phaseProgress + Mathf.Max(0f, deltaTime) / Mathf.Max(0.01f, phaseDuration));
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
            currentNodeIndex = 0;
            travelSign = 1;
            actionPosition = resetPosition;
            if (controlledBody != null)
            {
                controlledBody.position = resetPosition;
                controlledBody.linearVelocity = Vector2.zero;
            }

            transform.position = resetPosition;
            UpdateDirectionIndicator();
        }

        public void ConfigurePath(Vector2[] nodes, GhostMoveRouteMode mode)
        {
            if (nodes == null || nodes.Length < 2)
            {
                return;
            }

            pathNodes = nodes;
            routeMode = mode;
            resetPosition = pathNodes[0];
            ResetAction();
        }

        public static void CalculateNextPingPongStep(
            int currentIndex,
            int currentTravelSign,
            int gridCount,
            out int nextIndex,
            out int nextTravelSign)
        {
            int safeGridCount = Mathf.Max(1, gridCount);
            int safeTravelSign = currentTravelSign < 0 ? -1 : 1;
            nextIndex = Mathf.Clamp(currentIndex + safeTravelSign, 0, safeGridCount);
            nextTravelSign = nextIndex == 0 || nextIndex == safeGridCount
                ? -safeTravelSign
                : safeTravelSign;
        }

        private void EnsurePath()
        {
            if (pathNodes != null && pathNodes.Length >= 2)
            {
                return;
            }

            Vector2 start = controlledBody != null ? controlledBody.position : (Vector2)transform.position;
            Vector2 direction = actionDirection.sqrMagnitude > 0.001f
                ? actionDirection.normalized
                : Vector2.right;
            int count = Mathf.Max(1, pathGridCount);
            float step = Mathf.Max(0.001f, gridSize);
            pathNodes = new Vector2[count + 1];
            for (int i = 0; i <= count; i++)
            {
                pathNodes[i] = start + direction * step * i;
            }
        }

        private bool TryGetNextNodeIndex(out int nextIndex)
        {
            nextIndex = currentNodeIndex + travelSign;
            if (nextIndex >= 0 && nextIndex < pathNodes.Length)
            {
                return true;
            }

            if (routeMode == GhostMoveRouteMode.OneWay)
            {
                return false;
            }

            travelSign = -travelSign;
            nextIndex = currentNodeIndex + travelSign;
            return nextIndex >= 0 && nextIndex < pathNodes.Length;
        }

        private int FindBlockingHit(Vector2 direction, int hitCount)
        {
            int selected = -1;
            float selectedDistance = float.MaxValue;
            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit2D hit = castResults[i];
                if (hit.collider == null || Vector2.Dot(hit.normal, direction) > -0.5f)
                {
                    continue;
                }
                if (hit.distance < selectedDistance)
                {
                    selected = i;
                    selectedDistance = hit.distance;
                }
            }
            return selected;
        }

        private Vector2 GetNextDirection()
        {
            if (pathNodes == null || pathNodes.Length < 2)
            {
                return Vector2.right;
            }

            int nextIndex = currentNodeIndex + travelSign;
            if (nextIndex < 0 || nextIndex >= pathNodes.Length)
            {
                nextIndex = currentNodeIndex - travelSign;
            }

            Vector2 offset = pathNodes[Mathf.Clamp(nextIndex, 0, pathNodes.Length - 1)]
                - pathNodes[Mathf.Clamp(currentNodeIndex, 0, pathNodes.Length - 1)];
            return offset.sqrMagnitude > 0.001f ? offset.normalized : Vector2.right;
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

        private void TickOutbound(float deltaTime)
        {
            phaseProgress = Mathf.Min(
                1f,
                phaseProgress + Mathf.Max(0f, deltaTime) / Mathf.Max(0.01f, phaseDuration));
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

            CommitSuccessfulRouteStep();
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

        private void CommitSuccessfulRouteStep()
        {
            currentNodeIndex = targetNodeIndex;
            if (routeMode == GhostMoveRouteMode.PingPong
                && (currentNodeIndex == 0 || currentNodeIndex == pathNodes.Length - 1))
            {
                travelSign = -travelSign;
            }
            UpdateDirectionIndicator();
        }

        private void CompleteAction()
        {
            phase = MovePhase.Idle;
            IsExecuting = false;
        }

        private void UpdateDirectionIndicator()
        {
            if (directionIndicator == null)
            {
                return;
            }

            Vector2 direction = GetNextDirection();
            directionIndicator.localPosition = new Vector3(
                direction.x * indicatorDistance,
                direction.y * indicatorDistance,
                indicatorDepth);
            directionIndicator.localRotation = Quaternion.Euler(
                0f,
                0f,
                Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        }

        private static float EaseSharp(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }
    }
}
