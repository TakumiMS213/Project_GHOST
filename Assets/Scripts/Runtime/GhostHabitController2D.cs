using UnityEngine;

namespace TelegGhost.Runtime
{
    public enum GhostHabitType
    {
        Rotate,
        Bloom
    }

    public enum GhostHabitState
    {
        Dormant,
        Primed,
        Acting,
        Frozen
    }

    [DefaultExecutionOrder(-100)]
    public sealed class GhostHabitController2D : MonoBehaviour
    {
        [SerializeField] private ObservationType observationType;
        [SerializeField] private GhostHabitType habitType;
        [SerializeField] private PlayerVision2D playerVision;
        [SerializeField] private Collider2D observationCollider;
        [SerializeField] private Transform affectedTransform;
        [SerializeField] private Rigidbody2D affectedRigidbody;
        [SerializeField] private float rotationSpeed = 55f;
        [SerializeField] private Vector3 bloomScale = new Vector3(5f, 0.45f, 1f);
        [SerializeField] private float bloomSpeed = 4f;
        [SerializeField] private Renderer[] visibilityRenderers;
        [SerializeField] private SpriteRenderer stateIndicator;

        private Vector3 initialScale;
        private float initialRotation;
        private bool currentlyObserved;

        public ObservationType ObservationType => observationType;
        public GhostHabitType HabitType => habitType;
        public GhostHabitState State { get; private set; }
        public bool IsObserved => currentlyObserved;
        public bool IsActing => State == GhostHabitState.Acting;

        private void Awake()
        {
            observationCollider ??= GetComponent<Collider2D>();
            affectedTransform ??= transform;
            affectedRigidbody ??= affectedTransform.GetComponent<Rigidbody2D>();

            initialScale = affectedTransform.localScale;
            initialRotation = affectedRigidbody != null
                ? affectedRigidbody.rotation
                : affectedTransform.eulerAngles.z;

            if (visibilityRenderers == null || visibilityRenderers.Length == 0)
            {
                visibilityRenderers = GetComponentsInChildren<Renderer>(true);
            }

            State = GhostHabitState.Dormant;
            UpdatePresentation();
        }

        private void Start()
        {
            EvaluateObservation();
        }

        private void Update()
        {
            EvaluateObservation();
        }

        private void FixedUpdate()
        {
            if (habitType == GhostHabitType.Rotate)
            {
                UpdateRotation();
                return;
            }

            UpdateBloom();
        }

        public static GhostHabitState ResolveState(
            ObservationType type,
            GhostHabitState currentState,
            bool observed)
        {
            if (currentState == GhostHabitState.Dormant)
            {
                if (!observed)
                {
                    return GhostHabitState.Dormant;
                }

                return type == ObservationType.Tele
                    ? GhostHabitState.Primed
                    : GhostHabitState.Acting;
            }

            if (type == ObservationType.Tele)
            {
                if (currentState == GhostHabitState.Primed && observed)
                {
                    return GhostHabitState.Primed;
                }

                return observed ? GhostHabitState.Frozen : GhostHabitState.Acting;
            }

            return observed ? GhostHabitState.Acting : GhostHabitState.Frozen;
        }

        public void ResetState()
        {
            currentlyObserved = false;
            State = GhostHabitState.Dormant;

            if (affectedRigidbody != null)
            {
                affectedRigidbody.rotation = initialRotation;
                affectedRigidbody.angularVelocity = 0f;
            }
            else if (affectedTransform != null)
            {
                Vector3 angles = affectedTransform.eulerAngles;
                angles.z = initialRotation;
                affectedTransform.eulerAngles = angles;
            }

            if (affectedTransform != null)
            {
                affectedTransform.localScale = initialScale;
            }

            UpdatePresentation();
        }

        private void EvaluateObservation()
        {
            currentlyObserved = playerVision != null &&
                observationCollider != null &&
                playerVision.IsVisible(observationCollider);

            GhostHabitState nextState = ResolveState(observationType, State, currentlyObserved);
            if (nextState == State)
            {
                return;
            }

            State = nextState;
            UpdatePresentation();
        }

        private void UpdateRotation()
        {
            if (State != GhostHabitState.Acting || affectedTransform == null)
            {
                return;
            }

            float delta = rotationSpeed * Time.fixedDeltaTime;
            if (affectedRigidbody != null)
            {
                affectedRigidbody.MoveRotation(affectedRigidbody.rotation + delta);
                return;
            }

            affectedTransform.Rotate(0f, 0f, delta, Space.Self);
        }

        private void UpdateBloom()
        {
            if (affectedTransform == null)
            {
                return;
            }

            Vector3 targetScale = State == GhostHabitState.Acting ? bloomScale : initialScale;
            affectedTransform.localScale = Vector3.MoveTowards(
                affectedTransform.localScale,
                targetScale,
                bloomSpeed * Time.fixedDeltaTime);
        }

        private void UpdatePresentation()
        {
            if (visibilityRenderers != null)
            {
                for (int i = 0; i < visibilityRenderers.Length; i++)
                {
                    Renderer targetRenderer = visibilityRenderers[i];
                    if (targetRenderer != null)
                    {
                        targetRenderer.enabled = currentlyObserved;
                    }
                }
            }

            if (stateIndicator == null)
            {
                return;
            }

            switch (State)
            {
                case GhostHabitState.Primed:
                    stateIndicator.color = Color.white;
                    break;
                case GhostHabitState.Acting:
                    stateIndicator.color = new Color(0.65f, 0.68f, 0.74f, 1f);
                    break;
                case GhostHabitState.Frozen:
                    stateIndicator.color = new Color(0.88f, 0.9f, 0.96f, 1f);
                    break;
                default:
                    stateIndicator.color = new Color(0.2f, 0.22f, 0.26f, 1f);
                    break;
            }
        }
    }
}
