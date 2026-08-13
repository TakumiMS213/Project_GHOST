using UnityEngine;

namespace TelegGhost.Runtime.Stage
{
    public sealed class SwitchLockedGoal2D : MonoBehaviour
    {
        [SerializeField] private GhostSwitch2D sourceSwitch;
        [SerializeField] private Collider2D goalCollider;
        [SerializeField] private SpriteRenderer goalRenderer;
        [SerializeField] private Color lockedColor = new Color(0.35f, 0.36f, 0.4f, 1f);
        [SerializeField] private Color unlockedColor = Color.white;

        public bool IsUnlocked { get; private set; }

        private void OnEnable()
        {
            if (sourceSwitch != null)
            {
                sourceSwitch.StateChanged += SetUnlocked;
                SetUnlocked(sourceSwitch.IsActive);
            }
            else
            {
                SetUnlocked(true);
            }
        }

        private void OnDisable()
        {
            if (sourceSwitch != null)
            {
                sourceSwitch.StateChanged -= SetUnlocked;
            }
        }

        private void SetUnlocked(bool unlocked)
        {
            IsUnlocked = unlocked;
            if (goalCollider != null)
            {
                goalCollider.enabled = unlocked;
            }
            if (goalRenderer != null)
            {
                goalRenderer.color = unlocked ? unlockedColor : lockedColor;
            }
        }
    }
}
