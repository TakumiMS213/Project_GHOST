using UnityEngine;
using UnityEngine.Events;

namespace TelegGhost.Runtime.Stage
{
    public sealed class MultiSwitchGate2D : MonoBehaviour
    {
        [SerializeField] private GhostSwitch2D[] requiredSwitches;
        [SerializeField] private DoorController2D controlledDoor;
        [SerializeField] private UnityEvent<bool> stateChanged;

        private bool isOpen;

        private void OnEnable()
        {
            if (requiredSwitches != null)
            {
                for (int i = 0; i < requiredSwitches.Length; i++)
                {
                    GhostSwitch2D target = requiredSwitches[i];
                    if (target != null)
                    {
                        target.StateChanged += OnSwitchStateChanged;
                    }
                }
            }

            RefreshState();
        }

        private void OnDisable()
        {
            if (requiredSwitches == null)
            {
                return;
            }

            for (int i = 0; i < requiredSwitches.Length; i++)
            {
                GhostSwitch2D target = requiredSwitches[i];
                if (target != null)
                {
                    target.StateChanged -= OnSwitchStateChanged;
                }
            }
        }

        private void OnSwitchStateChanged(bool _)
        {
            RefreshState();
        }

        private void RefreshState()
        {
            bool shouldOpen = requiredSwitches != null && requiredSwitches.Length > 0;
            if (requiredSwitches != null)
            {
                for (int i = 0; i < requiredSwitches.Length; i++)
                {
                    if (requiredSwitches[i] == null || !requiredSwitches[i].IsActive)
                    {
                        shouldOpen = false;
                        break;
                    }
                }
            }

            controlledDoor?.SetOpen(shouldOpen);
            if (shouldOpen != isOpen)
            {
                isOpen = shouldOpen;
                stateChanged?.Invoke(shouldOpen);
            }
        }
    }
}
