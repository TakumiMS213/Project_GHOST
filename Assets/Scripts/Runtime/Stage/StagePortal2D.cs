using UnityEngine;
using UnityEngine.SceneManagement;

namespace TelegGhost.Runtime.Stage
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class StagePortal2D : MonoBehaviour
    {
        [SerializeField] private string sceneName;

        private bool isLoading;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (isLoading || other == null || other.GetComponentInParent<PlayerController2D>() == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(sceneName))
            {
                return;
            }

            isLoading = true;
            SceneManager.LoadScene(sceneName);
        }
    }
}
