using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace TelegGhost.Runtime
{
    [DefaultExecutionOrder(-300)]
    public sealed class GhostObserver2D : MonoBehaviour
    {
        private static readonly List<GhostObserver2D> Observers = new List<GhostObserver2D>();

        [SerializeField] private PlayerController2D directionSource;
        [SerializeField] private Transform visionOrigin;
        [SerializeField] private Vector2 fixedDirection = Vector2.right;
        [SerializeField] private MeshFilter visionMeshFilter;
        [SerializeField] private Light2D visionLight;
        [SerializeField, Range(3, 121)] private int rayCount = 61;
        [SerializeField, Range(1f, 179f)] private float fieldOfView = 60f;
        [SerializeField] private float viewDistance = 10f;
        [SerializeField] private LayerMask obstructionMask;

        private readonly RaycastHit2D[] raycastResults = new RaycastHit2D[1];
        private ContactFilter2D obstructionFilter;
        private Mesh visionMesh;
        private Vector3[] meshVertices;
        private Vector3[] lightPath;
        private int[] triangles;

        public static IReadOnlyList<GhostObserver2D> ActiveObservers => Observers;
        public Vector2 ViewDirection => directionSource != null
            ? directionSource.FacingDirection
            : NormalizeDirection(fixedDirection);
        public Vector2 Origin => visionOrigin != null ? (Vector2)visionOrigin.position : transform.position;

        private void Awake()
        {
            visionOrigin ??= transform;
            rayCount = Mathf.Max(3, rayCount);
            obstructionFilter = new ContactFilter2D();
            obstructionFilter.SetLayerMask(obstructionMask);
            obstructionFilter.useTriggers = false;

            meshVertices = new Vector3[rayCount + 1];
            lightPath = new Vector3[rayCount + 2];
            triangles = new int[(rayCount - 1) * 3];

            for (int i = 0; i < rayCount - 1; i++)
            {
                int triangleIndex = i * 3;
                triangles[triangleIndex] = 0;
                triangles[triangleIndex + 1] = i + 1;
                triangles[triangleIndex + 2] = i + 2;
            }

            visionMesh = new Mesh { name = "TELEGHOST_ObserverVisionMesh" };
            visionMesh.MarkDynamic();
            visionMesh.vertices = meshVertices;
            visionMesh.triangles = triangles;

            if (visionMeshFilter != null)
            {
                visionMeshFilter.sharedMesh = visionMesh;
            }
        }

        private void OnEnable()
        {
            if (!Observers.Contains(this))
            {
                Observers.Add(this);
            }
        }

        private void OnDisable()
        {
            Observers.Remove(this);
        }

        private void OnDestroy()
        {
            Observers.Remove(this);
            if (visionMesh != null)
            {
                Destroy(visionMesh);
            }
        }

        private void Update()
        {
            RebuildVision();
        }

        public bool CanObserve(Vector2 worldPoint)
        {
            Vector2 origin = Origin;
            Vector2 offset = worldPoint - origin;
            float distance = offset.magnitude;
            if (distance <= 0.001f)
            {
                return true;
            }

            if (distance > viewDistance)
            {
                return false;
            }

            Vector2 direction = offset / distance;
            if (Vector2.Angle(ViewDirection, direction) > fieldOfView * 0.5f)
            {
                return false;
            }

            return !TryGetObstruction(origin, direction, distance, out _);
        }

        public void SetFixedDirection(Vector2 direction)
        {
            fixedDirection = NormalizeDirection(direction);
        }

        private static Vector2 NormalizeDirection(Vector2 direction)
        {
            return direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.right;
        }

        private void RebuildVision()
        {
            if (visionOrigin == null || meshVertices == null)
            {
                return;
            }

            Vector2 origin = visionOrigin.position;
            Vector2 viewDirection = ViewDirection;
            float facingAngle = Mathf.Atan2(viewDirection.y, viewDirection.x) * Mathf.Rad2Deg;
            float startAngle = facingAngle - fieldOfView * 0.5f;
            meshVertices[0] = Vector3.zero;
            lightPath[0] = Vector3.zero;

            for (int i = 0; i < rayCount; i++)
            {
                float t = i / (float)(rayCount - 1);
                float angle = Mathf.Lerp(startAngle, startAngle + fieldOfView, t) * Mathf.Deg2Rad;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                bool blocked = TryGetObstruction(origin, direction, viewDistance, out RaycastHit2D hit);
                Vector2 worldPoint = blocked ? hit.point : origin + direction * viewDistance;
                Vector3 localPoint = visionOrigin.InverseTransformPoint(worldPoint);
                meshVertices[i + 1] = localPoint;
                lightPath[i + 1] = localPoint;
            }

            lightPath[lightPath.Length - 1] = Vector3.zero;
            if (visionMesh != null)
            {
                visionMesh.vertices = meshVertices;
                visionMesh.RecalculateBounds();
            }

            visionLight?.SetShapePath(lightPath);
        }

        private bool TryGetObstruction(
            Vector2 origin,
            Vector2 direction,
            float distance,
            out RaycastHit2D hit)
        {
            int hitCount = Physics2D.Raycast(
                origin,
                direction,
                obstructionFilter,
                raycastResults,
                distance);
            hit = hitCount > 0 ? raycastResults[0] : default;
            return hitCount > 0;
        }
    }
}
