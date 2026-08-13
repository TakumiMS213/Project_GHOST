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
        [SerializeField, Min(1)] private int afterimageCount = 3;
        [SerializeField] private float afterimageEmissionDuration = 0.12f;
        [SerializeField] private float afterimageLifetime = 0.24f;
        [SerializeField] private Color afterimageColor = new Color(0.82f, 0.9f, 1f, 0.58f);

        private Vector3 fullScale;
        private Vector3 fullPosition;
        private Vector3 baseRootScale;
        private Color bodyColor;
        private float fullWidth;
        private Transform afterimageRoot;
        private SpriteRenderer[] afterimageRenderers;
        private float[] afterimageAges;
        private float emissionRemaining;
        private float emissionInterval;
        private float emissionTimer;
        private int afterimageCursor;

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
            CreateAfterimagePool();
            SetProgress(0f);
        }

        private void Update()
        {
            TickEmission(Time.deltaTime);
            TickAfterimages(Time.deltaTime);
        }

        private void OnDisable()
        {
            ResetPulseFeedback();
        }

        private void OnDestroy()
        {
            if (afterimageRoot != null)
            {
                Destroy(afterimageRoot.gameObject);
            }
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

        public void PlayPulseAfterimage()
        {
            if (bodyRenderer == null || afterimageRenderers == null)
            {
                return;
            }

            emissionRemaining = Mathf.Max(0f, afterimageEmissionDuration);
            emissionTimer = 0f;
            EmitAfterimage();
        }

        public void ResetPulseFeedback()
        {
            emissionRemaining = 0f;
            if (afterimageRenderers == null || afterimageAges == null)
            {
                return;
            }

            for (int i = 0; i < afterimageRenderers.Length; i++)
            {
                afterimageAges[i] = afterimageLifetime;
                if (afterimageRenderers[i] != null)
                {
                    afterimageRenderers[i].enabled = false;
                }
            }
        }

        private void CreateAfterimagePool()
        {
            if (bodyRenderer == null)
            {
                return;
            }

            int safeCount = Mathf.Max(1, afterimageCount);
            afterimageRenderers = new SpriteRenderer[safeCount];
            afterimageAges = new float[safeCount];
            emissionInterval = Mathf.Max(0.01f, afterimageEmissionDuration / safeCount);
            afterimageRoot = new GameObject($"{name}_Afterimages").transform;

            for (int i = 0; i < safeCount; i++)
            {
                GameObject afterimageObject = new GameObject($"Afterimage_{i + 1:00}");
                afterimageObject.transform.SetParent(afterimageRoot, false);
                SpriteRenderer renderer = afterimageObject.AddComponent<SpriteRenderer>();
                CopyRendererSettings(renderer);
                renderer.enabled = false;
                afterimageRenderers[i] = renderer;
                afterimageAges[i] = afterimageLifetime;
            }
        }

        private void TickEmission(float deltaTime)
        {
            if (emissionRemaining <= 0f)
            {
                return;
            }

            emissionRemaining = Mathf.Max(0f, emissionRemaining - deltaTime);
            emissionTimer -= deltaTime;
            if (emissionTimer > 0f)
            {
                return;
            }

            emissionTimer += emissionInterval;
            EmitAfterimage();
        }

        private void TickAfterimages(float deltaTime)
        {
            if (afterimageRenderers == null || afterimageAges == null)
            {
                return;
            }

            float safeLifetime = Mathf.Max(0.01f, afterimageLifetime);
            for (int i = 0; i < afterimageRenderers.Length; i++)
            {
                SpriteRenderer renderer = afterimageRenderers[i];
                if (renderer == null || !renderer.enabled)
                {
                    continue;
                }

                afterimageAges[i] += deltaTime;
                float remaining = 1f - afterimageAges[i] / safeLifetime;
                if (remaining <= 0f)
                {
                    renderer.enabled = false;
                    continue;
                }

                Color color = afterimageColor;
                color.a *= remaining;
                renderer.color = color;
            }
        }

        private void EmitAfterimage()
        {
            if (bodyRenderer == null || afterimageRenderers == null || afterimageRenderers.Length == 0)
            {
                return;
            }

            SpriteRenderer renderer = afterimageRenderers[afterimageCursor];
            afterimageCursor = (afterimageCursor + 1) % afterimageRenderers.Length;
            if (renderer == null)
            {
                return;
            }

            CopyRendererSettings(renderer);
            Transform source = bodyRenderer.transform;
            Transform target = renderer.transform;
            target.SetPositionAndRotation(source.position, source.rotation);
            target.localScale = source.lossyScale;
            renderer.color = afterimageColor;
            renderer.enabled = true;
            afterimageAges[afterimageCursor == 0 ? afterimageRenderers.Length - 1 : afterimageCursor - 1] = 0f;
        }

        private void CopyRendererSettings(SpriteRenderer target)
        {
            if (target == null || bodyRenderer == null)
            {
                return;
            }

            target.sprite = bodyRenderer.sprite;
            target.sharedMaterial = bodyRenderer.sharedMaterial;
            target.sortingLayerID = bodyRenderer.sortingLayerID;
            target.sortingOrder = bodyRenderer.sortingOrder - 1;
            target.flipX = bodyRenderer.flipX;
            target.flipY = bodyRenderer.flipY;
            target.drawMode = bodyRenderer.drawMode;
            target.size = bodyRenderer.size;
            target.maskInteraction = bodyRenderer.maskInteraction;
        }
    }
}
