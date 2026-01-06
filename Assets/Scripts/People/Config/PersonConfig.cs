using UnityEngine;

namespace TowerGame.People
{
    /// <summary>
    /// Common configuration for all person types
    /// The Tower II style discrete movement settings
    /// </summary>
    [CreateAssetMenu(fileName = "PersonConfig", menuName = "Tower Game/Person Config")]
    public class PersonConfig : ScriptableObject
    {
        [Header("Movement (The Tower II Style)")]
        [Tooltip("Distance moved per step in Unity units")]
        [Range(0.1f, 2f)]
        public float stepSize = 0.5f;

        [Tooltip("Time interval between steps in seconds")]
        [Range(0.05f, 0.5f)]
        public float stepInterval = 0.1f;

        [Tooltip("Distance threshold to consider arrival at destination")]
        [Range(0.01f, 0.5f)]
        public float arrivalThreshold = 0.1f;

        [Header("Vertical Movement")]
        [Tooltip("Speed when using stairs (steps per second)")]
        public float stairClimbSpeed = 2f;

        [Tooltip("Elevator boarding/exit time in seconds")]
        public float elevatorTransitionTime = 0.5f;

        [Header("Visual Settings")]
        [Tooltip("Base size of person sprite")]
        public Vector2 personSize = new Vector2(0.5f, 0.8f);

        [Tooltip("Sorting order for person sprites")]
        public int baseSortingOrder = 10;

        [Tooltip("Available colors for employees")]
        public Color[] employeeColors = new Color[]
        {
            new Color(0.2f, 0.4f, 0.8f),  // Blue
            new Color(0.8f, 0.3f, 0.3f),  // Red
            new Color(0.3f, 0.7f, 0.3f),  // Green
            new Color(0.8f, 0.6f, 0.2f),  // Orange
            new Color(0.6f, 0.3f, 0.7f)   // Purple
        };

        [Tooltip("Available colors for visitors")]
        public Color[] visitorColors = new Color[]
        {
            new Color(0.5f, 0.5f, 0.5f),  // Gray
            new Color(0.4f, 0.6f, 0.8f),  // Light Blue
            new Color(0.8f, 0.5f, 0.6f),  // Pink
            new Color(0.6f, 0.7f, 0.5f),  // Light Green
            new Color(0.7f, 0.6f, 0.4f)   // Tan
        };

        [Tooltip("Available colors for residents")]
        public Color[] residentColors = new Color[]
        {
            new Color(0.9f, 0.8f, 0.6f),  // Cream
            new Color(0.6f, 0.5f, 0.4f),  // Brown
            new Color(0.5f, 0.6f, 0.7f),  // Steel Blue
            new Color(0.7f, 0.5f, 0.5f),  // Dusty Rose
            new Color(0.5f, 0.7f, 0.6f)   // Sage
        };

        #region Utility Methods

        /// <summary>
        /// Calculate walking speed in units per second
        /// </summary>
        public float GetWalkingSpeed()
        {
            return stepSize / stepInterval;
        }

        /// <summary>
        /// Get color for employee by index (cycles through available colors)
        /// </summary>
        public Color GetEmployeeColor(int index)
        {
            if (employeeColors == null || employeeColors.Length == 0)
            {
                return Color.white;
            }
            return employeeColors[index % employeeColors.Length];
        }

        /// <summary>
        /// Get color for visitor by index
        /// </summary>
        public Color GetVisitorColor(int index)
        {
            if (visitorColors == null || visitorColors.Length == 0)
            {
                return Color.white;
            }
            return visitorColors[index % visitorColors.Length];
        }

        /// <summary>
        /// Get color for resident by index
        /// </summary>
        public Color GetResidentColor(int index)
        {
            if (residentColors == null || residentColors.Length == 0)
            {
                return Color.white;
            }
            return residentColors[index % residentColors.Length];
        }

        /// <summary>
        /// Calculate time to walk a certain distance
        /// </summary>
        public float CalculateWalkTime(float distance)
        {
            float speed = GetWalkingSpeed();
            return speed > 0 ? distance / speed : 0f;
        }

        /// <summary>
        /// Calculate number of steps to reach a distance
        /// </summary>
        public int CalculateStepCount(float distance)
        {
            return Mathf.CeilToInt(distance / stepSize);
        }

        /// <summary>
        /// Validate configuration values
        /// </summary>
        private void OnValidate()
        {
            stepSize = Mathf.Max(0.1f, stepSize);
            stepInterval = Mathf.Max(0.05f, stepInterval);
            arrivalThreshold = Mathf.Max(0.01f, arrivalThreshold);
            stairClimbSpeed = Mathf.Max(0.5f, stairClimbSpeed);
            elevatorTransitionTime = Mathf.Max(0.1f, elevatorTransitionTime);
            personSize = new Vector2(Mathf.Max(0.1f, personSize.x), Mathf.Max(0.1f, personSize.y));
        }

        #endregion
    }
}
