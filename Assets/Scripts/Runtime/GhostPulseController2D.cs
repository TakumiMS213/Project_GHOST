using System.Collections.Generic;
using UnityEngine;

namespace TelegGhost.Runtime
{
    public sealed class GhostPulseController2D : MonoBehaviour
    {
        private static readonly List<GhostPulseController2D> ActivePulseAudio =
            new List<GhostPulseController2D>();

        [SerializeField] private GhostController2D ghostController;
        [SerializeField] private GhostAction2D ghostAction;
        [SerializeField] private GhostPulseFill2D pulseVisual;
        [SerializeField] private Renderer visibilityRenderer;
        [SerializeField] private AudioSource pulseAudioSource;
        [SerializeField] private AudioClip pulseClip;
        [SerializeField, Range(0f, 1f)] private float pulseVolume = 0.1625f;
        [SerializeField] private float pulseInterval = 0.5f;
        [SerializeField] private float completionFlashDuration = 0.06f;

        private const string DefaultPulseClipPath = "Short_Accent07-1(Dry)";
        private static AudioClip cachedDefaultPulseClip;

        private bool actionPending;
        private bool actionInProgress;
        private float flashRemaining;
        private Camera audioCamera;
        private float pulseAudioEndTime;
        private bool pulseAudioRegistered;

        public float PulseProgress { get; private set; }
        public float PulseInterval => pulseInterval;
        public bool IsActionExecuting => ghostAction != null && ghostAction.IsExecuting;

        private void Awake()
        {
            if (ghostController == null)
            {
                ghostController = GetComponent<GhostController2D>();
            }
            if (ghostAction == null)
            {
                ghostAction = GetComponent<GhostAction2D>();
            }
            if (pulseVisual == null)
            {
                pulseVisual = GetComponentInChildren<GhostPulseFill2D>(true);
            }
            if (visibilityRenderer == null)
            {
                visibilityRenderer = GetComponentInChildren<Renderer>(true);
            }
            if (pulseAudioSource == null)
            {
                pulseAudioSource = GetComponent<AudioSource>();
            }
            if (pulseAudioSource == null)
            {
                pulseAudioSource = gameObject.AddComponent<AudioSource>();
            }
            if (pulseClip == null)
            {
                pulseClip = LoadDefaultPulseClip();
            }

            if (pulseAudioSource != null)
            {
                pulseAudioSource.playOnAwake = false;
                pulseAudioSource.loop = false;
                pulseAudioSource.spatialBlend = 0f;
            }
            audioCamera = Camera.main;
        }

        private void FixedUpdate()
        {
            UpdatePulseAudioRegistration();
            if (ghostController == null)
            {
                return;
            }

            bool active = ghostController.IsActive;
            if (actionInProgress)
            {
                if (ghostAction != null && ghostAction.IsExecuting)
                {
                    if (active)
                    {
                        ghostAction.TickAction(Time.fixedDeltaTime);
                    }
                    return;
                }

                CompletePulseCycle();
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
                    pulseVisual?.SetFlash(false);
                    PlayPulseFeedback();
                    ghostAction?.BeginAction();
                    actionInProgress = ghostAction != null && ghostAction.IsExecuting;
                    if (!actionInProgress)
                    {
                        CompletePulseCycle();
                    }
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
            actionInProgress = false;
            flashRemaining = 0f;
            PulseProgress = 0f;
            if (pulseAudioSource != null)
            {
                pulseAudioSource.Stop();
            }
            UnregisterPulseAudio();
            pulseVisual?.SetFlash(false);
            pulseVisual?.SetProgress(0f);
            pulseVisual?.ResetPulseFeedback();
            ghostAction?.ResetAction();
        }

        private void CompletePulseCycle()
        {
            actionInProgress = false;
            PulseProgress = 0f;
            pulseVisual?.SetProgress(0f);
        }

        private void PlayPulseFeedback()
        {
            pulseVisual?.PlayPulseAfterimage();
            if (pulseAudioSource != null && pulseClip != null && IsInsideAudioCamera())
            {
                RegisterPulseAudio();
                pulseAudioSource.PlayOneShot(pulseClip, pulseVolume);
            }
        }

        private void OnDisable()
        {
            UnregisterPulseAudio();
        }

        private void RegisterPulseAudio()
        {
            if (pulseAudioSource == null || pulseClip == null)
            {
                return;
            }

            float safePitch = Mathf.Max(0.01f, Mathf.Abs(pulseAudioSource.pitch));
            pulseAudioEndTime = Time.unscaledTime + pulseClip.length / safePitch;
            if (!pulseAudioRegistered)
            {
                ActivePulseAudio.Add(this);
                pulseAudioRegistered = true;
            }
            RebalancePulseAudio();
        }

        private void UpdatePulseAudioRegistration()
        {
            if (pulseAudioRegistered && Time.unscaledTime >= pulseAudioEndTime)
            {
                UnregisterPulseAudio();
            }
        }

        private void UnregisterPulseAudio()
        {
            if (!pulseAudioRegistered)
            {
                return;
            }

            pulseAudioRegistered = false;
            ActivePulseAudio.Remove(this);
            if (pulseAudioSource != null)
            {
                pulseAudioSource.volume = 1f;
            }
            RebalancePulseAudio();
        }

        private static void RebalancePulseAudio()
        {
            float now = Time.unscaledTime;
            for (int i = ActivePulseAudio.Count - 1; i >= 0; i--)
            {
                GhostPulseController2D pulse = ActivePulseAudio[i];
                if (pulse != null && pulse.pulseAudioRegistered && pulse.pulseAudioEndTime > now)
                {
                    continue;
                }

                if (pulse != null)
                {
                    pulse.pulseAudioRegistered = false;
                    if (pulse.pulseAudioSource != null)
                    {
                        pulse.pulseAudioSource.volume = 1f;
                    }
                }
                ActivePulseAudio.RemoveAt(i);
            }

            float overlapScale = 1f / Mathf.Sqrt(Mathf.Max(1, ActivePulseAudio.Count));
            for (int i = 0; i < ActivePulseAudio.Count; i++)
            {
                AudioSource source = ActivePulseAudio[i].pulseAudioSource;
                if (source != null)
                {
                    source.volume = overlapScale;
                }
            }
        }

        private bool IsInsideAudioCamera()
        {
            if (audioCamera == null || !audioCamera.isActiveAndEnabled || visibilityRenderer == null)
            {
                return false;
            }

            Bounds bounds = visibilityRenderer.bounds;
            Vector3 viewportMin = audioCamera.WorldToViewportPoint(bounds.min);
            Vector3 viewportMax = audioCamera.WorldToViewportPoint(bounds.max);
            if (viewportMin.z < 0f && viewportMax.z < 0f)
            {
                return false;
            }

            float minimumX = Mathf.Min(viewportMin.x, viewportMax.x);
            float maximumX = Mathf.Max(viewportMin.x, viewportMax.x);
            float minimumY = Mathf.Min(viewportMin.y, viewportMax.y);
            float maximumY = Mathf.Max(viewportMin.y, viewportMax.y);
            return maximumX >= 0f && minimumX <= 1f && maximumY >= 0f && minimumY <= 1f;
        }

        private static AudioClip LoadDefaultPulseClip()
        {
            if (cachedDefaultPulseClip == null)
            {
                cachedDefaultPulseClip = Resources.Load<AudioClip>(DefaultPulseClipPath);
            }
            return cachedDefaultPulseClip;
        }
    }
}
