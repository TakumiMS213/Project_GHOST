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

        private bool presentationInitialized;
        private bool lastObserved;

        public ObservationType ObservationType => observationType;
        public bool IsObserved => observationDetector != null && observationDetector.IsObserved;
        public bool IsActive => ShouldBeActive(observationType, IsObserved);

        private void Awake()
        {
            observationDetector ??= GetComponent<GhostObservationDetector2D>();
            RefreshPresentation();
        }

        private void Update()
        {
            RefreshPresentation();
        }

        public static bool ShouldBeActive(ObservationType type, bool observed)
        {
            return type == ObservationType.Tele ? !observed : observed;
        }

        public void ResetState()
        {
            presentationInitialized = false;
            RefreshPresentation();
        }

        private void RefreshPresentation()
        {
            bool observed = IsObserved;
            if (presentationInitialized && observed == lastObserved)
            {
                return;
            }

            presentationInitialized = true;
            lastObserved = observed;
            observedVisualRoot?.SetActive(observed);
            if (observedOnlyRenderers == null)
            {
                return;
            }

            for (int i = 0; i < observedOnlyRenderers.Length; i++)
            {
                Renderer target = observedOnlyRenderers[i];
                if (target != null)
                {
                    target.enabled = observed;
                }
            }
        }
    }
}
