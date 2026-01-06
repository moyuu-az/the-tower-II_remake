using UnityEngine;
using UnityEngine.SceneManagement;

namespace TowerGame.Core.Events
{
    /// <summary>
    /// Automatically clears the EventBus when scenes are unloaded
    /// Prevents memory leaks from lingering subscriptions
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class EventBusCleaner : MonoBehaviour
    {
        private static EventBusCleaner instance;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
        }

        private void OnSceneUnloaded(Scene scene)
        {
            Debug.Log($"[EventBusCleaner] Scene '{scene.name}' unloaded, clearing EventBus");
            EventBus.Clear();
        }

        /// <summary>
        /// Ensure the cleaner exists in the scene
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (instance == null)
            {
                var go = new GameObject("[EventBusCleaner]");
                go.AddComponent<EventBusCleaner>();
            }
        }
    }
}
