using System;
using UnityEngine;

namespace TowerGame.Core
{
    /// <summary>
    /// Manages game time where 10 real seconds = 1 game hour
    /// </summary>
    public class GameTimeManager : MonoBehaviour
    {
        public static GameTimeManager Instance { get; private set; }

        [Header("Time Settings")]
        [SerializeField] private float timeScale = 360f; // 10 seconds = 1 hour (3600/10 = 360)
        [SerializeField] private float startHour = 6f; // Game starts at 6:00 AM

        [Header("Current Time (Read Only)")]
        [SerializeField] private float currentHour;
        [SerializeField] private int currentDay = 1;
        [SerializeField] private bool isPaused;

        // Events
        public event Action<int> OnHourChanged;
        public event Action<int> OnDayChanged;
        public event Action<float> OnTimeUpdated;

        // Properties
        public float CurrentHour => currentHour;
        public int CurrentDay => currentDay;
        public bool IsPaused
        {
            get => isPaused;
            set => isPaused = value;
        }
        public float TimeScale
        {
            get => timeScale;
            set => timeScale = Mathf.Max(0, value);
        }

        private int lastHour;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            currentHour = startHour;
            lastHour = Mathf.FloorToInt(currentHour);
        }

        private void Update()
        {
            if (isPaused) return;

            float previousHour = currentHour;

            // Calculate time progression
            // timeScale = 360 means 1 real second = 360 game seconds = 6 game minutes = 0.1 game hours
            currentHour += Time.deltaTime * timeScale / 3600f;

            // Handle day rollover
            if (currentHour >= 24f)
            {
                currentHour -= 24f;
                currentDay++;
                OnDayChanged?.Invoke(currentDay);
                Debug.Log($"[GameTime] New day: Day {currentDay}");
            }

            // Check for hour change
            int newHour = Mathf.FloorToInt(currentHour);
            if (newHour != lastHour)
            {
                lastHour = newHour;
                OnHourChanged?.Invoke(newHour);
                Debug.Log($"[GameTime] Hour changed: {GetFormattedTime()}");
            }

            OnTimeUpdated?.Invoke(currentHour);
        }

        /// <summary>
        /// Returns time in "HH:MM" format
        /// </summary>
        public string GetFormattedTime()
        {
            int hours = Mathf.FloorToInt(currentHour);
            int minutes = Mathf.FloorToInt((currentHour - hours) * 60);
            return $"{hours:D2}:{minutes:D2}";
        }

        /// <summary>
        /// Returns full date/time string
        /// </summary>
        public string GetFullTimeString()
        {
            return $"Day {currentDay} - {GetFormattedTime()}";
        }

        /// <summary>
        /// Check if current time is within working hours (8:00 - 18:00)
        /// </summary>
        public bool IsWorkingHours()
        {
            return currentHour >= 8f && currentHour < 18f;
        }

        /// <summary>
        /// Check if it's time to go to work (around 8:00)
        /// </summary>
        public bool IsCommutingTime()
        {
            return currentHour >= 7f && currentHour < 8.5f;
        }

        /// <summary>
        /// Check if it's time to leave work (around 18:00)
        /// </summary>
        public bool IsLeavingTime()
        {
            return currentHour >= 17.5f && currentHour < 19f;
        }

        /// <summary>
        /// Set the current time (for debugging)
        /// </summary>
        public void SetTime(float hour)
        {
            currentHour = Mathf.Clamp(hour, 0f, 23.99f);
            lastHour = Mathf.FloorToInt(currentHour);
        }

        /// <summary>
        /// Skip to a specific hour
        /// </summary>
        public void SkipToHour(int targetHour)
        {
            if (targetHour < 0 || targetHour > 23) return;

            if (targetHour <= currentHour)
            {
                // Skip to next day
                currentDay++;
                OnDayChanged?.Invoke(currentDay);
            }

            currentHour = targetHour;
            lastHour = targetHour;
            OnHourChanged?.Invoke(targetHour);
        }
    }
}
