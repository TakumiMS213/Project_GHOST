using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace TelegGhost.Runtime
{
    [DefaultExecutionOrder(-200)]
    public sealed class PlayerVision2D : MonoBehaviour
    {
        [SerializeField] private PlayerController2D player;
        [SerializeField] private Transform visionOrigin;
        [SerializeField] private MeshFilter visionMeshFilter;
        [SerializeField] private Light2D visionLight;
        [SerializeField, Range(3, 121)] private int rayCount = 61;
        [SerializeField, Range(1f, 179f)] private float fieldOfView = 70f;
        [SerializeField] private float viewDistance = 9f;
        [SerializeField] private LayerMask obstructionMask;

        private readonly Vector2[] observationSamples = new Vector2[5];
        private readonly RaycastHit2D[] raycastResults = new RaycastHit2D[1];
        private Mesh visionMesh;
        private ContactFilter2D obstructionFilter;
        private Vector3[] meshVertices;
        private Vector3[] lightPath;
        private int[] triangles;

        private void Awake()
        {
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

            visionMesh = new Mesh { name = "TELEGHOST_VisionMesh" };
            visionMesh.MarkDynamic();
            visionMesh.vertices = meshVertices;
            visionMesh.triangles = triangles;

            if (visionMeshFilter != null)
            {
                visionMeshFilter.sharedMesh = visionMesh;
            }
        }

        private void OnDestroy()
        {
            if (visionMesh != null)
            {
                Destroy(visionMesh);
            }
        }

        private void Update()
        {
            RebuildVision();
        }

        public bool IsVisible(Collider2D targetCollider)
        {
            if (targetCollider == null || !targetCollider.enabled || player == null || visionOrigin == null)
            {
                return false;
            }

            Bounds bounds = targetCollider.bounds;
            Vector3 extents = bounds.extents * 0.92f;
            observationSamples[0] = bounds.center;
            observationSamples[1] = new Vector2(bounds.center.x - extents.x, bounds.center.y - extents.y);
            observationSamples[2] = new Vector2(bounds.center.x - extents.x, bounds.center.y + extents.y);
            observationSamples[3] = new Vector2(bounds.center.x + extents.x, bounds.center.y - extents.y);
            observationSamples[4] = new Vector2(bounds.center.x + extents.x, bounds.center.y + extents.y);

            for (int i = 0; i < observationSamples.Length; i++)
            {
                if (IsPointVisible(observationSamples[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsPointVisible(Vector2 target)
        {
            Vector2 origin = visionOrigin.position;
            Vector2 offset = target - origin;
            float distance = offset.magnitude;
            if (distance <= 0.001f || distance > viewDistance)
            {
                return false;
            }

            Vector2 direction = offset / distance;
            if (Vector2.Angle(player.FacingDirection, direction) > fieldOfView * 0.5f)
            {
                return false;
            }

            return !TryGetObstruction(origin, direction, distance, out _);
        }

        private void RebuildVision()
        {
            if (player == null || visionOrigin == null || meshVertices == null)
            {
                return;
            }

            Vector2 origin = visionOrigin.position;
            Vector2 facingDirection = player.FacingDirection;
            float facingAngle = Mathf.Atan2(facingDirection.y, facingDirection.x) * Mathf.Rad2Deg;
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

        private bool TryGetObstruction(Vector2 origin, Vector2 direction, float distance, out RaycastHit2D hit)
        {
            int hitCount = Physics2D.Raycast(origin, direction, obstructionFilter, raycastResults, distance);
            hit = hitCount > 0 ? raycastResults[0] : default;
            return hitCount > 0;
        }
    }
}
