using UnityEngine;

namespace TelegGhost.Runtime
{
    public sealed class GhostPulseController2D : MonoBehaviour
    {
        [SerializeField] private GhostController2D ghostController;
        [SerializeField] private GhostAction2D ghostAction;
        [SerializeField] private GhostPulseFill2D pulseVisual;
        [SerializeField] private float pulseInterval = 0.5f;
        [SerializeField] private float completionFlashDuration = 0.06f;

        private bool actionPending;
        private float flashRemaining;

        public float PulseProgress { get; private set; }
        public float PulseInterval => pulseInterval;
        public bool IsActionExecuting => ghostAction != null && ghostAction.IsExecuting;

        private void Awake()
        {
            ghostController ??= GetComponent<GhostController2D>();
            ghostAction ??= GetComponent<GhostAction2D>();
            pulseVisual ??= GetComponentInChildren<GhostPulseFill2D>(true);
        }

        private void FixedUpdate()
        {
            if (ghostController == null)
            {
                return;
            }

            bool active = ghostController.IsActive;
            if (ghostAction != null && ghostAction.IsExecuting)
            {
                if (active)
                {
                    ghostAction.TickAction(Time.fixedDeltaTime);
                }
                return;
            }

            if (actionPending)
            {
                if (!active)
                {
                    return;
                }

                flashRemaining -= Time.fixedDeltaTime;
                if (flashRemaining <= 0f)
                {
                    actionPending = false;
                    PulseProgress = 0f;
                    pulseVisual?.SetFlash(false);
                    pulseVisual?.SetProgress(0f);
                    ghostAction?.BeginAction();
                }
                return;
            }

            PulseProgress = CalculateNextProgress(
                PulseProgress,
                active,
                Time.fixedDeltaTime,
                pulseInterval);
            pulseVisual?.SetProgress(PulseProgress);

            if (PulseProgress >= 1f)
            {
                actionPending = true;
                flashRemaining = Mathf.Max(0f, completionFlashDuration);
                pulseVisual?.SetFlash(true);
            }
        }

        public static float CalculateNextProgress(
            float currentProgress,
            bool active,
            float deltaTime,
            float interval)
        {
            if (!active)
            {
                return Mathf.Clamp01(currentProgress);
            }

            return Mathf.Clamp01(currentProgress + Mathf.Max(0f, deltaTime) / Mathf.Max(0.001f, interval));
        }

        public void ResetState()
        {
            actionPending = false;
            flashRemaining = 0f;
            PulseProgress = 0f;
            pulseVisual?.SetFlash(false);
            pulseVisual?.SetProgress(0f);
            ghostAction?.ResetAction();
        }
    }
}
