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
        [SerializeField, Range(1f, 179f)] private float fieldOfView = 32f;
        [SerializeField] private float viewDistance = 12f;
        [SerializeField, Min(0f)] private float visualEdgeFadeDistance = 1.3f;
        [SerializeField, Range(0f, 15f)] private float visualSideFadeAngle = 8f;
        [SerializeField] private LayerMask obstructionMask;

        private readonly RaycastHit2D[] raycastResults = new RaycastHit2D[1];
        private ContactFilter2D obstructionFilter;
        private Mesh visionMesh;
        private Vector3[] meshVertices;
        private Color[] meshColors;
        private Vector3[] lightPath;
        private int[] triangles;

        public static IReadOnlyList<GhostObserver2D> ActiveObservers => Observers;
        public Vector2 ViewDirection => directionSource != null
            ? directionSource.FacingDirection
            : NormalizeDirection(fixedDirection);
        public Vector2 Origin => visionOrigin != null ? (Vector2)visionOrigin.position : transform.position;

        private void Awake()
        {
            if (visionOrigin == null)
            {
                visionOrigin = transform;
            }
            rayCount = Mathf.Max(3, rayCount);
            obstructionFilter = new ContactFilter2D();
            obstructionFilter.SetLayerMask(obstructionMask);
            obstructionFilter.useTriggers = true;

            meshVertices = new Vector3[(rayCount * 2) + 1];
            meshColors = new Color[meshVertices.Length];
            lightPath = new Vector3[rayCount + 2];
            triangles = new int[(rayCount - 1) * 9];

            meshColors[0] = new Color(1f, 1f, 1f, 0.9f);
            float visualFieldOfView = fieldOfView + (visualSideFadeAngle * 2f);
            float sideFadeRatio = visualFieldOfView > 0f
                ? visualSideFadeAngle / visualFieldOfView
                : 0f;

            for (int i = 0; i < rayCount - 1; i++)
            {
                int triangleIndex = i * 9;
                int inner = i + 1;
                int nextInner = inner + 1;
                int outer = rayCount + i + 1;
                int nextOuter = outer + 1;
                triangles[triangleIndex] = 0;
                triangles[triangleIndex + 1] = inner;
                triangles[triangleIndex + 2] = nextInner;
                triangles[triangleIndex + 3] = inner;
                triangles[triangleIndex + 4] = outer;
                triangles[triangleIndex + 5] = nextInner;
                triangles[triangleIndex + 6] = nextInner;
                triangles[triangleIndex + 7] = outer;
                triangles[triangleIndex + 8] = nextOuter;
            }

            for (int i = 0; i < rayCount; i++)
            {
                float t = i / (float)(rayCount - 1);
                float sideDistance = Mathf.Min(t, 1f - t);
                float sideWeight = Mathf.SmoothStep(
                    0f,
                    1f,
                    sideFadeRatio > 0f ? Mathf.Clamp01(sideDistance / sideFadeRatio) : 1f);
                meshColors[i + 1] = new Color(1f, 1f, 1f, sideWeight);
                meshColors[rayCount + i + 1] = Color.clear;
            }

            visionMesh = new Mesh { name = "TELEGHOST_ObserverVisionMesh" };
            visionMesh.MarkDynamic();
            visionMesh.vertices = meshVertices;
            visionMesh.colors = meshColors;
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
            float visualFieldOfView = fieldOfView + (visualSideFadeAngle * 2f);
            float startAngle = facingAngle - visualFieldOfView * 0.5f;
            meshVertices[0] = Vector3.zero;
            lightPath[0] = Vector3.zero;

            for (int i = 0; i < rayCount; i++)
            {
                float t = i / (float)(rayCount - 1);
                float angle = Mathf.Lerp(startAngle, startAngle + visualFieldOfView, t) * Mathf.Deg2Rad;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                bool blocked = TryGetObstruction(origin, direction, viewDistance, out RaycastHit2D hit);
                Vector2 worldPoint = blocked ? hit.point : origin + direction * viewDistance;
                float endpointDistance = Vector2.Distance(origin, worldPoint);
                float innerDistance = Mathf.Max(0f, endpointDistance - visualEdgeFadeDistance);
                Vector2 innerWorldPoint = origin + direction * innerDistance;
                Vector3 innerLocalPoint = visionOrigin.InverseTransformPoint(innerWorldPoint);
                Vector3 outerLocalPoint = visionOrigin.InverseTransformPoint(worldPoint);
                meshVertices[i + 1] = innerLocalPoint;
                meshVertices[rayCount + i + 1] = outerLocalPoint;
                lightPath[i + 1] = outerLocalPoint;
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
