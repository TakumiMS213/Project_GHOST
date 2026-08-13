using UnityEngine;

namespace TelegGhost.Runtime
{
    [DefaultExecutionOrder(-200)]
    public sealed class GhostController2D : MonoBehaviour
    {
        [SerializeField] private ObservationType observationType;
        [SerializeField] private GhostObservationDetector2D observationDetector;
        [SerializeField] private GameObject observedVisualRoot;
        [SerializeField] private Renderer[] observedOnlyRenderers;
        [SerializeField, Range(0f, 1f)] private float unobservedAlpha;

        private SpriteRenderer[] visibilitySpriteRenderers;
        private TextMesh[] visibilityTextMeshes;
        private float[] spriteBaseAlphas;
        private float[] textBaseAlphas;

        public ObservationType ObservationType => observationType;
        public bool IsObserved => observationDetector != null && observationDetector.IsObserved;
        public bool IsActive => ShouldBeActive(observationType, IsObserved);

        private void Awake()
        {
            observationDetector ??= GetComponent<GhostObservationDetector2D>();
            visibilitySpriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
            visibilityTextMeshes = GetComponentsInChildren<TextMesh>(true);
            spriteBaseAlphas = CacheSpriteAlphas(visibilitySpriteRenderers);
            textBaseAlphas = CacheTextAlphas(visibilityTextMeshes);
            ApplyObservationVisibility();
        }

        private void LateUpdate()
        {
            ApplyObservationVisibility();
        }

        public static bool ShouldBeActive(ObservationType type, bool observed)
        {
            return type == ObservationType.Tele ? !observed : observed;
        }

        public void ResetState()
        {
            ApplyObservationVisibility();
        }

        private void ApplyObservationVisibility()
        {
            observedVisualRoot?.SetActive(true);
            bool observed = IsObserved;
            if (observedOnlyRenderers != null)
            {
                for (int i = 0; i < observedOnlyRenderers.Length; i++)
                {
                    Renderer target = observedOnlyRenderers[i];
                    if (target != null)
                    {
                        target.enabled = observed;
                    }
                }
            }

            float alphaMultiplier = observed ? 1f : unobservedAlpha;
            ApplySpriteAlpha(alphaMultiplier);
            ApplyTextAlpha(alphaMultiplier);
        }

        private void ApplySpriteAlpha(float alphaMultiplier)
        {
            if (visibilitySpriteRenderers == null || spriteBaseAlphas == null)
            {
                return;
            }

            for (int i = 0; i < visibilitySpriteRenderers.Length; i++)
            {
                SpriteRenderer target = visibilitySpriteRenderers[i];
                if (target == null)
                {
                    continue;
                }

                float targetAlpha = spriteBaseAlphas[i] * alphaMultiplier;
                Color color = target.color;
                if (Mathf.Approximately(color.a, targetAlpha))
                {
                    continue;
                }

                color.a = targetAlpha;
                target.color = color;
            }
        }

        private void ApplyTextAlpha(float alphaMultiplier)
        {
            if (visibilityTextMeshes == null || textBaseAlphas == null)
            {
                return;
            }

            for (int i = 0; i < visibilityTextMeshes.Length; i++)
            {
                TextMesh target = visibilityTextMeshes[i];
                if (target == null)
                {
                    continue;
                }

                float targetAlpha = textBaseAlphas[i] * alphaMultiplier;
                Color color = target.color;
                if (Mathf.Approximately(color.a, targetAlpha))
                {
                    continue;
                }

                color.a = targetAlpha;
                target.color = color;
            }
        }

        private static float[] CacheSpriteAlphas(SpriteRenderer[] renderers)
        {
            if (renderers == null)
            {
                return null;
            }

            float[] alphas = new float[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                alphas[i] = renderers[i] != null ? renderers[i].color.a : 1f;
            }

            return alphas;
        }

        private static float[] CacheTextAlphas(TextMesh[] textMeshes)
        {
            if (textMeshes == null)
            {
                return null;
            }

            float[] alphas = new float[textMeshes.Length];
            for (int i = 0; i < textMeshes.Length; i++)
            {
                alphas[i] = textMeshes[i] != null ? textMeshes[i].color.a : 1f;
            }

            return alphas;
        }
    }
}
