using UnityEngine;

namespace TelegGhost.Runtime
{
    public sealed class GhostPulseFill2D : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private SpriteRenderer fillRenderer;
        [SerializeField] private Transform fillTransform;
        [SerializeField] private Transform flashRoot;
        [SerializeField] private Color fillColor = Color.white;
        [SerializeField] private float flashScale = 1.08f;

        private Vector3 fullScale;
        private Vector3 fullPosition;
        private Vector3 baseRootScale;
        private Color bodyColor;
        private float fullWidth;

        private void Awake()
        {
            fillTransform ??= fillRenderer != null ? fillRenderer.transform : null;
            flashRoot ??= transform;
            if (fillTransform != null)
            {
                fullScale = fillTransform.localScale;
                fullPosition = fillTransform.localPosition;
                if (fillRenderer != null && fillRenderer.sprite != null)
                {
                    fullWidth = fillRenderer.sprite.bounds.size.x * fullScale.x;
                }
            }

            baseRootScale = flashRoot.localScale;
            bodyColor = bodyRenderer != null ? bodyRenderer.color : Color.white;
            SetProgress(0f);
        }

        public void SetProgress(float progress)
        {
            if (fillRenderer == null || fillTransform == null)
            {
                return;
            }

            float clamped = Mathf.Clamp01(progress);
            fillRenderer.enabled = clamped > 0.001f;
            fillRenderer.color = fillColor;
            Vector3 scale = fullScale;
            scale.x = fullScale.x * clamped;
            fillTransform.localScale = scale;
            Vector3 position = fullPosition;
            position.x -= fullWidth * (1f - clamped) * 0.5f;
            fillTransform.localPosition = position;
        }

        public void SetFlash(bool enabled)
        {
            if (bodyRenderer != null)
            {
                bodyRenderer.color = enabled ? Color.white : bodyColor;
            }

            if (flashRoot != null)
            {
                flashRoot.localScale = enabled ? baseRootScale * flashScale : baseRootScale;
            }
        }
    }
}
