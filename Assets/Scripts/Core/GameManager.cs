using UnityEngine;
using UnityEngine.InputSystem;
using TowerGame.Building;
using TowerGame.People;

namespace TowerGame.Core
{
    /// <summary>
    /// Main game controller that initializes and manages the simulation
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("References (Auto-found if not set)")]
        [SerializeField] private GameTimeManager timeManager;
        [SerializeField] private OfficeBuilding officeBuilding;
        [SerializeField] private PersonSpawner personSpawner;

        [Header("Game State")]
        [SerializeField] private bool isGameRunning;

        // Properties
        public bool IsRunning => isGameRunning;
        public GameTimeManager TimeManager => timeManager;
        public OfficeBuilding Office => officeBuilding;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            InitializeGame();
        }

        /// <summary>
        /// Initialize all game systems
        /// </summary>
        private void InitializeGame()
        {
            // Auto-find components if not assigned
            if (timeManager == null)
            {
                timeManager = GetComponentInChildren<GameTimeManager>();
                if (timeManager == null)
                {
                    timeManager = FindObjectOfType<GameTimeManager>();
                }
            }

            if (officeBuilding == null)
            {
                officeBuilding = FindObjectOfType<OfficeBuilding>();
            }

            if (personSpawner == null)
            {
                personSpawner = GetComponentInChildren<PersonSpawner>();
                if (personSpawner == null)
                {
                    personSpawner = FindObjectOfType<PersonSpawner>();
                }
            }

            // Validate setup
            if (timeManager == null)
            {
                Debug.LogError("[GameManager] GameTimeManager not found!");
            }
            if (officeBuilding == null)
            {
                Debug.LogError("[GameManager] OfficeBuilding not found!");
            }
            if (personSpawner == null)
            {
                Debug.LogError("[GameManager] PersonSpawner not found!");
            }

            isGameRunning = true;
            Debug.Log("[GameManager] Game initialized successfully");
        }

        /// <summary>
        /// Pause the game
        /// </summary>
        public void PauseGame()
        {
            if (timeManager != null)
            {
                timeManager.IsPaused = true;
            }
            isGameRunning = false;
            Debug.Log("[GameManager] Game paused");
        }

        /// <summary>
        /// Resume the game
        /// </summary>
        public void ResumeGame()
        {
            if (timeManager != null)
            {
                timeManager.IsPaused = false;
            }
            isGameRunning = true;
            Debug.Log("[GameManager] Game resumed");
        }

        /// <summary>
        /// Toggle pause state
        /// </summary>
        public void TogglePause()
        {
            if (isGameRunning)
            {
                PauseGame();
            }
            else
            {
                ResumeGame();
            }
        }

        /// <summary>
        /// Set time scale (1x, 2x, 4x speed)
        /// </summary>
        public void SetTimeScale(float multiplier)
        {
            if (timeManager != null)
            {
                // Base scale is 360 (10 seconds = 1 hour)
                timeManager.TimeScale = 360f * multiplier;
                Debug.Log($"[GameManager] Time scale set to {multiplier}x");
            }
        }

        /// <summary>
        /// Skip to a specific hour
        /// </summary>
        public void SkipToHour(int hour)
        {
            if (timeManager != null)
            {
                timeManager.SkipToHour(hour);
            }
        }

        private void Update()
        {
            // Debug controls using new Input System
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.spaceKey.wasPressedThisFrame)
            {
                TogglePause();
            }
            if (keyboard.digit1Key.wasPressedThisFrame)
            {
                SetTimeScale(1f);
            }
            if (keyboard.digit2Key.wasPressedThisFrame)
            {
                SetTimeScale(2f);
            }
            if (keyboard.digit4Key.wasPressedThisFrame)
            {
                SetTimeScale(4f);
            }
        }

        private void OnGUI()
        {
            // Simple debug info
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label($"Time: {(timeManager != null ? timeManager.GetFullTimeString() : "N/A")}");
            GUILayout.Label($"Game Running: {isGameRunning}");
            GUILayout.Label("Controls: Space=Pause, 1/2/4=Speed");

            if (personSpawner != null)
            {
                GUILayout.Label($"Employees: {personSpawner.EmployeeCount}");
            }
            GUILayout.EndArea();
        }
    }
}
