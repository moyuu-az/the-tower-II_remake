using UnityEngine;

namespace TowerGame.Core
{
    /// <summary>
    /// Game-wide configuration settings
    /// ScriptableObject for easy tuning in Unity Editor
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Tower Game/Game Config")]
    public class GameConfig : ScriptableObject
    {
        [Header("Time Settings")]
        [Tooltip("Base time scale: 360 = 10 real seconds equals 1 game hour")]
        public float baseTimeScale = 360f;

        [Tooltip("Starting hour of the day (0-23)")]
        [Range(0f, 23f)]
        public float startHour = 6f;

        [Tooltip("Work start hour")]
        [Range(0, 23)]
        public int workStartHour = 8;

        [Tooltip("Work end hour")]
        [Range(0, 23)]
        public int workEndHour = 18;

        [Header("Economy Settings")]
        [Tooltip("Starting money in yen")]
        public long startingMoney = 1000000;

        [Tooltip("Daily maintenance cost multiplier")]
        [Range(0f, 2f)]
        public float maintenanceCostMultiplier = 1f;

        [Tooltip("Daily rent income multiplier")]
        [Range(0f, 2f)]
        public float rentIncomeMultiplier = 1f;

        [Header("Simulation Settings")]
        [Tooltip("Maximum employees per office tenant")]
        public int maxEmployeesPerOffice = 10;

        [Tooltip("Initial number of employees to spawn")]
        public int initialEmployeeCount = 3;

        [Tooltip("Maximum population in the building")]
        public int maxPopulation = 500;

        [Header("Building Limits")]
        [Tooltip("Maximum number of floors")]
        public int maxFloors = 15;

        [Tooltip("Maximum width in segments")]
        public int maxWidthSegments = 40;

        [Tooltip("Maximum number of towers")]
        public int maxTowers = 2;

        [Header("Game Speed Presets")]
        [Tooltip("Speed multipliers for time controls")]
        public float[] speedMultipliers = new float[] { 1f, 2f, 4f };

        #region Utility Methods

        /// <summary>
        /// Get real seconds per game hour based on time scale
        /// </summary>
        public float GetSecondsPerGameHour()
        {
            return 3600f / baseTimeScale;
        }

        /// <summary>
        /// Convert real seconds to game hours
        /// </summary>
        public float RealSecondsToGameHours(float realSeconds)
        {
            return realSeconds * baseTimeScale / 3600f;
        }

        /// <summary>
        /// Convert game hours to real seconds
        /// </summary>
        public float GameHoursToRealSeconds(float gameHours)
        {
            return gameHours * 3600f / baseTimeScale;
        }

        /// <summary>
        /// Check if a given hour is within working hours
        /// </summary>
        public bool IsWithinWorkingHours(float hour)
        {
            return hour >= workStartHour && hour < workEndHour;
        }

        /// <summary>
        /// Get the duration of a work day in game hours
        /// </summary>
        public int GetWorkDayDuration()
        {
            return workEndHour - workStartHour;
        }

        /// <summary>
        /// Validate configuration values
        /// </summary>
        private void OnValidate()
        {
            baseTimeScale = Mathf.Max(1f, baseTimeScale);
            startHour = Mathf.Clamp(startHour, 0f, 23.99f);
            startingMoney = System.Math.Max(0, startingMoney);
            maxEmployeesPerOffice = Mathf.Max(1, maxEmployeesPerOffice);
            initialEmployeeCount = Mathf.Max(0, initialEmployeeCount);
            maxPopulation = Mathf.Max(1, maxPopulation);
            maxFloors = Mathf.Max(1, maxFloors);
            maxWidthSegments = Mathf.Max(1, maxWidthSegments);
            maxTowers = Mathf.Max(1, maxTowers);

            if (workEndHour <= workStartHour)
            {
                workEndHour = workStartHour + 1;
            }
        }

        #endregion
    }
}
