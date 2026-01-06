using UnityEngine;

namespace TowerGame.People
{
    /// <summary>
    /// Configuration specific to employee behavior
    /// Work schedules, spawning, and behavior settings
    /// </summary>
    [CreateAssetMenu(fileName = "EmployeeConfig", menuName = "Tower Game/Employee Config")]
    public class EmployeeConfig : ScriptableObject
    {
        [Header("Work Schedule")]
        [Tooltip("Default hour employees start arriving (0-23)")]
        [Range(0, 23)]
        public int defaultArrivalHour = 8;

        [Tooltip("Default hour employees start leaving (0-23)")]
        [Range(0, 23)]
        public int defaultDepartureHour = 18;

        [Tooltip("Random variance in arrival time (minutes)")]
        [Range(0, 60)]
        public int arrivalVarianceMinutes = 15;

        [Tooltip("Random variance in departure time (minutes)")]
        [Range(0, 60)]
        public int departureVarianceMinutes = 30;

        [Header("Elevator Behavior")]
        [Tooltip("Maximum time to wait for elevator before giving up (seconds)")]
        public float maxElevatorWaitTime = 30f;

        [Tooltip("Time spent boarding/exiting elevator (seconds)")]
        public float elevatorBoardingTime = 0.5f;

        [Tooltip("Maximum number of employees per elevator car")]
        public int maxEmployeesPerElevator = 8;

        [Header("Work Behavior")]
        [Tooltip("Offset from tenant entrance to work position")]
        public float workPositionOffset = 0.5f;

        [Tooltip("Time between position changes while working (seconds)")]
        public float workPositionChangeInterval = 60f;

        [Tooltip("Chance of taking a break during work hours (0-1)")]
        [Range(0f, 1f)]
        public float breakChance = 0.1f;

        [Header("Spawn Settings")]
        [Tooltip("Default home position for spawning")]
        public Vector2 homePosition = new Vector2(-18f, -3f);

        [Tooltip("Horizontal spacing between employees at spawn")]
        public float employeeSpacing = 0.8f;

        [Tooltip("Random offset range for home positions")]
        public float homePositionVariance = 0.5f;

        [Header("Stress System")]
        [Tooltip("Enable stress accumulation from waiting")]
        public bool enableStressSystem = true;

        [Tooltip("Stress increase per second of elevator waiting")]
        public float stressPerSecondWaiting = 0.5f;

        [Tooltip("Stress decrease per second while working normally")]
        public float stressDecayRate = 0.1f;

        [Tooltip("Maximum stress level before complaints")]
        public float maxStressLevel = 100f;

        #region Utility Methods

        /// <summary>
        /// Get arrival hour with random variance
        /// </summary>
        public float GetRandomizedArrivalHour()
        {
            float variance = Random.Range(-arrivalVarianceMinutes, arrivalVarianceMinutes) / 60f;
            return Mathf.Clamp(defaultArrivalHour + variance, 0f, 23.99f);
        }

        /// <summary>
        /// Get departure hour with random variance
        /// </summary>
        public float GetRandomizedDepartureHour()
        {
            float variance = Random.Range(-departureVarianceMinutes, departureVarianceMinutes) / 60f;
            return Mathf.Clamp(defaultDepartureHour + variance, 0f, 23.99f);
        }

        /// <summary>
        /// Calculate home position for a specific employee index
        /// </summary>
        public Vector2 GetEmployeeHomePosition(int employeeIndex)
        {
            float xOffset = employeeIndex * employeeSpacing;
            float randomOffset = Random.Range(-homePositionVariance, homePositionVariance);
            return homePosition + new Vector2(xOffset + randomOffset, 0);
        }

        /// <summary>
        /// Check if current hour is within arrival window
        /// </summary>
        public bool IsArrivalTime(float currentHour)
        {
            float windowStart = defaultArrivalHour - (arrivalVarianceMinutes / 60f);
            float windowEnd = defaultArrivalHour + (arrivalVarianceMinutes / 60f) + 0.5f;
            return currentHour >= windowStart && currentHour <= windowEnd;
        }

        /// <summary>
        /// Check if current hour is within departure window
        /// </summary>
        public bool IsDepartureTime(float currentHour)
        {
            float windowStart = defaultDepartureHour - (departureVarianceMinutes / 60f);
            float windowEnd = defaultDepartureHour + (departureVarianceMinutes / 60f) + 0.5f;
            return currentHour >= windowStart && currentHour <= windowEnd;
        }

        /// <summary>
        /// Get work duration in hours
        /// </summary>
        public int GetWorkDuration()
        {
            return defaultDepartureHour - defaultArrivalHour;
        }

        /// <summary>
        /// Calculate stress from elevator wait time
        /// </summary>
        public float CalculateWaitStress(float waitTimeSeconds)
        {
            if (!enableStressSystem) return 0f;
            return waitTimeSeconds * stressPerSecondWaiting;
        }

        /// <summary>
        /// Validate configuration values
        /// </summary>
        private void OnValidate()
        {
            if (defaultDepartureHour <= defaultArrivalHour)
            {
                defaultDepartureHour = defaultArrivalHour + 1;
            }

            maxElevatorWaitTime = Mathf.Max(5f, maxElevatorWaitTime);
            elevatorBoardingTime = Mathf.Max(0.1f, elevatorBoardingTime);
            maxEmployeesPerElevator = Mathf.Max(1, maxEmployeesPerElevator);
            workPositionOffset = Mathf.Max(0f, workPositionOffset);
            workPositionChangeInterval = Mathf.Max(10f, workPositionChangeInterval);
            employeeSpacing = Mathf.Max(0.3f, employeeSpacing);
            homePositionVariance = Mathf.Max(0f, homePositionVariance);
            stressPerSecondWaiting = Mathf.Max(0f, stressPerSecondWaiting);
            stressDecayRate = Mathf.Max(0f, stressDecayRate);
            maxStressLevel = Mathf.Max(1f, maxStressLevel);
        }

        #endregion
    }
}
