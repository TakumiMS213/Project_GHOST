using UnityEngine;

namespace TelegGhost.Runtime
{
    public sealed class CameraFollow2D : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private PlayerController2D viewSource;
        [SerializeField] private float minimumX = -8f;
        [SerializeField] private float maximumX = 12f;
        [SerializeField] private float minimumY = -4f;
        [SerializeField] private float maximumY = 8f;
        [SerializeField] private float fixedY;
        [SerializeField] private bool followTargetY;
        [SerializeField] private Vector2 viewOffset = new Vector2(1.5f, 0.8f);
        [SerializeField] private float smoothTime = 0.18f;

        private Vector3 smoothVelocity;

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            Vector3 current = transform.position;
            Vector2 direction = viewSource != null ? viewSource.FacingDirection : Vector2.zero;
            float desiredX = target.position.x + direction.x * viewOffset.x;
            float baseY = followTargetY ? target.position.y : fixedY;
            float desiredY = baseY + direction.y * viewOffset.y;
            Vector3 desired = new Vector3(
                Mathf.Clamp(desiredX, minimumX, maximumX),
                Mathf.Clamp(desiredY, minimumY, maximumY),
                current.z);
            transform.position = Vector3.SmoothDamp(current, desired, ref smoothVelocity, smoothTime);
        }
    }
}
