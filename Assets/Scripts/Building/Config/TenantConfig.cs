using UnityEngine;
using TowerGame.Economy;

namespace TowerGame.Building
{
    /// <summary>
    /// Base configuration for tenant types (Office, Restaurant, Shop, Apartment)
    /// The Tower II style building tenant settings
    /// </summary>
    [CreateAssetMenu(fileName = "TenantConfig", menuName = "Tower Game/Tenant Config")]
    public class TenantConfig : ScriptableObject
    {
        [Header("Base Settings")]
        [Tooltip("Type of tenant this config applies to")]
        public TenantType tenantType;

        [Tooltip("Display name for this tenant type")]
        public string displayName = "Office";

        [Tooltip("Maximum capacity of occupants")]
        public int defaultCapacity = 10;

        [Tooltip("Width in grid segments")]
        public int widthSegments = 9;

        [Tooltip("Height in floors (typically 1)")]
        public int heightFloors = 1;

        [Header("Operating Hours")]
        [Tooltip("Hour the tenant opens (0-23)")]
        [Range(0, 23)]
        public int openHour = 8;

        [Tooltip("Hour the tenant closes (0-23)")]
        [Range(0, 23)]
        public int closeHour = 18;

        [Tooltip("Whether this tenant operates on weekends")]
        public bool operatesOnWeekends = false;

        [Header("Placement Rules")]
        [Tooltip("Whether this tenant can be placed on ground floor")]
        public bool allowGroundFloor = true;

        [Tooltip("Whether this tenant can be placed on upper floors")]
        public bool allowUpperFloors = true;

        [Tooltip("Minimum floor this tenant can be placed on (0-indexed)")]
        public int minimumFloor = 0;

        [Tooltip("Whether this tenant requires elevator access for upper floors")]
        public bool requiresElevator = true;

        [Tooltip("Minimum support percentage from floor below (0-1)")]
        [Range(0f, 1f)]
        public float minimumSupportPercentage = 0.7f;

        [Header("Visual Settings")]
        [Tooltip("Base color of the building exterior")]
        public Color buildingColor = new Color(0.85f, 0.85f, 0.9f);

        [Tooltip("Window color when occupied")]
        public Color occupiedWindowColor = new Color(1f, 0.95f, 0.7f);

        [Tooltip("Window color when empty")]
        public Color emptyWindowColor = new Color(0.5f, 0.6f, 0.7f);

        [Tooltip("Night time window color (lit)")]
        public Color nightWindowColor = new Color(1f, 0.9f, 0.6f);

        [Tooltip("Closed/dark window color")]
        public Color closedWindowColor = new Color(0.3f, 0.35f, 0.4f);

        [Header("Economy")]
        [Tooltip("Base construction cost in yen")]
        public long baseBuildCost = 500000;

        [Tooltip("Daily rent income per occupant")]
        public long rentPerOccupant = 10000;

        [Tooltip("Daily maintenance cost")]
        public long dailyMaintenanceCost = 5000;

        [Tooltip("Cost multiplier per floor above ground (1.0 = no increase)")]
        [Range(1f, 2f)]
        public float floorCostMultiplier = 1.1f;

        #region Utility Methods

        /// <summary>
        /// Check if the tenant is open at a given hour
        /// </summary>
        public bool IsOpenAtHour(float hour)
        {
            return hour >= openHour && hour < closeHour;
        }

        /// <summary>
        /// Get operating hours as formatted string
        /// </summary>
        public string GetOperatingHoursString()
        {
            return $"{openHour:D2}:00 - {closeHour:D2}:00";
        }

        /// <summary>
        /// Get operating duration in hours
        /// </summary>
        public int GetOperatingDuration()
        {
            return closeHour - openHour;
        }

        /// <summary>
        /// Calculate build cost for a specific floor
        /// </summary>
        public long CalculateBuildCost(int floor)
        {
            if (floor <= 0) return baseBuildCost;
            float multiplier = Mathf.Pow(floorCostMultiplier, floor);
            return (long)(baseBuildCost * multiplier);
        }

        /// <summary>
        /// Calculate expected daily income at full capacity
        /// </summary>
        public long CalculateMaxDailyIncome()
        {
            return rentPerOccupant * defaultCapacity;
        }

        /// <summary>
        /// Calculate net daily income at full capacity
        /// </summary>
        public long CalculateNetDailyIncome()
        {
            return CalculateMaxDailyIncome() - dailyMaintenanceCost;
        }

        /// <summary>
        /// Calculate days to recover build cost at full occupancy
        /// </summary>
        public int CalculatePaybackDays(int floor = 0)
        {
            long netIncome = CalculateNetDailyIncome();
            if (netIncome <= 0) return -1;
            return Mathf.CeilToInt(CalculateBuildCost(floor) / (float)netIncome);
        }

        /// <summary>
        /// Check if placement is valid for a given floor
        /// </summary>
        public bool IsValidPlacement(int floor, bool hasElevatorAccess = true)
        {
            if (floor < minimumFloor) return false;

            if (floor == 0)
            {
                return allowGroundFloor;
            }
            else
            {
                if (!allowUpperFloors) return false;
                if (requiresElevator && !hasElevatorAccess) return false;
                return true;
            }
        }

        /// <summary>
        /// Get window color based on occupancy and time
        /// </summary>
        public Color GetWindowColor(bool isOccupied, bool isOpen, bool isNight)
        {
            if (!isOpen)
            {
                return closedWindowColor;
            }

            if (isNight)
            {
                return isOccupied ? nightWindowColor : closedWindowColor;
            }

            return isOccupied ? occupiedWindowColor : emptyWindowColor;
        }

        /// <summary>
        /// Get the world width of this tenant
        /// </summary>
        public float GetWorldWidth(float segmentWidth = 1f)
        {
            return widthSegments * segmentWidth;
        }

        /// <summary>
        /// Get the world height of this tenant
        /// </summary>
        public float GetWorldHeight(float floorHeight = 3f)
        {
            return heightFloors * floorHeight;
        }

        /// <summary>
        /// Validate configuration values
        /// </summary>
        private void OnValidate()
        {
            if (closeHour <= openHour)
            {
                closeHour = openHour + 1;
            }

            defaultCapacity = Mathf.Max(1, defaultCapacity);
            widthSegments = Mathf.Max(1, widthSegments);
            heightFloors = Mathf.Max(1, heightFloors);
            minimumFloor = Mathf.Max(0, minimumFloor);
            baseBuildCost = System.Math.Max(0, baseBuildCost);
            rentPerOccupant = System.Math.Max(0, rentPerOccupant);
            dailyMaintenanceCost = System.Math.Max(0, dailyMaintenanceCost);

            if (string.IsNullOrEmpty(displayName))
            {
                displayName = tenantType.ToString();
            }
        }

        #endregion
    }
}
