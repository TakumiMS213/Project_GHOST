using System.Collections.Generic;
using UnityEngine;

namespace TelegGhost.Runtime
{
    [DefaultExecutionOrder(-250)]
    public sealed class GhostObservationDetector2D : MonoBehaviour
    {
        [SerializeField] private Transform observationPoint;

        public bool IsObserved { get; private set; }
        public Vector2 ObservationPoint => observationPoint != null
            ? (Vector2)observationPoint.position
            : transform.position;

        private void Awake()
        {
            observationPoint ??= transform;
            RefreshObservation();
        }

        private void OnEnable()
        {
            RefreshObservation();
        }

        private void Update()
        {
            RefreshObservation();
        }

        public void RefreshObservation()
        {
            IsObserved = IsObservedByAny(ObservationPoint, GhostObserver2D.ActiveObservers);
        }

        public static bool IsObservedByAny(
            Vector2 worldPoint,
            IReadOnlyList<GhostObserver2D> observers)
        {
            if (observers == null)
            {
                return false;
            }

            for (int i = 0; i < observers.Count; i++)
            {
                GhostObserver2D observer = observers[i];
                if (observer != null && observer.isActiveAndEnabled && observer.CanObserve(worldPoint))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
