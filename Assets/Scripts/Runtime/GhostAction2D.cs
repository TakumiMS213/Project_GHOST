using UnityEngine;

namespace TelegGhost.Runtime
{
    public abstract class GhostAction2D : MonoBehaviour
    {
        public bool IsExecuting { get; protected set; }

        public abstract void BeginAction();
        public abstract void TickAction(float deltaTime);
        public abstract void ResetAction();
    }
}
