using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace TelegGhost.Runtime
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class GhostSwitch2D : MonoBehaviour
    {
        [SerializeField] private DoorController2D controlledDoor;
        [SerializeField] private SpriteRenderer indicatorRenderer;
        [SerializeField] private Color inactiveColor = new Color(0.25f, 0.25f, 0.25f, 1f);
        [SerializeField] private Color activeColor = Color.white;
        [SerializeField] private bool allowPlayer = true;
        [SerializeField] private bool allowGhosts = true;
        [SerializeField] private bool restrictGhostType;
        [SerializeField] private ObservationType allowedGhostType;
        [SerializeField] private UnityEvent<bool> stateChanged;

        private readonly HashSet<Collider2D> occupants = new HashSet<Collider2D>();
        private bool isActive;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (IsAccepted(other) && occupants.Add(other))
            {
                RefreshState();
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other != null && occupants.Remove(other))
            {
                RefreshState();
            }
        }

        public void ResetState()
        {
            occupants.Clear();
            RefreshState();
        }

        private void RefreshState()
        {
            bool active = occupants.Count > 0;
            controlledDoor?.SetOpen(active);
            if (indicatorRenderer != null)
            {
                indicatorRenderer.color = active ? activeColor : inactiveColor;
            }

            if (active != isActive)
            {
                isActive = active;
                stateChanged?.Invoke(active);
            }
        }

        private bool IsAccepted(Collider2D other)
        {
            if (other == null)
            {
                return false;
            }

            if (allowPlayer && other.GetComponentInParent<PlayerController2D>() != null)
            {
                return true;
            }

            if (!allowGhosts)
            {
                return false;
            }

            GhostController2D pulseGhost = other.GetComponentInParent<GhostController2D>();
            if (pulseGhost != null)
            {
                return !restrictGhostType || pulseGhost.ObservationType == allowedGhostType;
            }

            return other.GetComponentInParent<GhostMover2D>() != null;
        }
    }
}
