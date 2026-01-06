using System.Collections.Generic;
using UnityEngine;
using TowerGame.Core;
using TowerGame.Economy;

namespace TowerGame.Building
{
    /// <summary>
    /// Apartment tenant type - residential units for Residents
    /// The Tower II style apartment with 24-hour availability
    /// </summary>
    public class Apartment : Tenant
    {
        [Header("Apartment-Specific Settings")]
        [SerializeField] private int unitCount = 4;
        [SerializeField] private int residentsPerUnit = 2;

        [Header("Apartment Visual Settings")]
        [SerializeField] private Color emptyUnitColor = new Color(0.3f, 0.3f, 0.35f);
        [SerializeField] private Color occupiedUnitColor = new Color(0.9f, 0.8f, 0.5f);
        [SerializeField] private Color sleepingColor = new Color(0.2f, 0.2f, 0.3f);

        // Unit tracking
        private List<GameObject> unitIndicators = new List<GameObject>();
        private List<int> unitsOccupancy = new List<int>(); // Residents per unit
        private int totalResidents = 0;

        #region Tenant Implementation

        public override TenantType TenantType => TenantType.Apartment;

        #endregion

        #region Properties

        /// <summary>
        /// Whether it's sleeping hours (22:00 - 6:00)
        /// </summary>
        public bool IsSleepingTime
        {
            get
            {
                if (GameTimeManager.Instance == null) return false;
                float hour = GameTimeManager.Instance.CurrentHour;
                return hour >= 22f || hour < 6f;
            }
        }

        /// <summary>
        /// Number of residential units
        /// </summary>
        public int UnitCount => unitCount;

        /// <summary>
        /// Total number of residents living here
        /// </summary>
        public int TotalResidents => totalResidents;

        /// <summary>
        /// Number of residents currently at home
        /// </summary>
        public int ResidentsAtHome => OccupantCount;

        /// <summary>
        /// Get an available unit index, or -1 if all full
        /// </summary>
        public int GetAvailableUnit()
        {
            for (int i = 0; i < unitCount; i++)
            {
                if (i < unitsOccupancy.Count && unitsOccupancy[i] < residentsPerUnit)
                {
                    return i;
                }
            }
            return -1;
        }

        #endregion

        #region Initialization

        protected override void Awake()
        {
            // Apartments are 24 hours (residents live there)
            openHour = 0;
            closeHour = 24;

            // Set capacity based on units
            capacity = unitCount * residentsPerUnit;

            // Initialize unit occupancy tracking
            for (int i = 0; i < unitCount; i++)
            {
                unitsOccupancy.Add(0);
            }

            // Set colors
            emptyColor = emptyUnitColor;
            occupiedColor = occupiedUnitColor;

            base.Awake();
        }

        protected override void Start()
        {
            base.Start();
            CreateUnitIndicators();
            UpdateVisualState();
        }

        protected override void CreateDefaultOccupantPositions(float groundLocalY)
        {
            // Create positions for each unit
            int unitsPerRow = (unitCount + 1) / 2;
            float xSpacing = 0.7f / unitsPerRow;

            for (int i = 0; i < unitCount; i++)
            {
                for (int r = 0; r < residentsPerUnit; r++)
                {
                    var posGO = new GameObject($"ResidentPosition_Unit{i}_{r}");
                    posGO.transform.SetParent(transform);

                    int row = i / unitsPerRow;
                    int col = i % unitsPerRow;
                    float xPos = -0.3f + col * xSpacing + r * 0.1f;
                    float yPos = groundLocalY + (row == 0 ? 0.15f : -0.15f);
                    posGO.transform.localPosition = new Vector3(xPos, yPos, 0);

                    occupantPositions.Add(posGO.transform);
                }
            }

            Debug.Log($"[Apartment] Created {occupantPositions.Count} resident positions across {unitCount} units");
        }

        private void CreateUnitIndicators()
        {
            // Create visual indicators for each unit (windows)
            int unitsPerRow = (unitCount + 1) / 2;
            float xSpacing = 0.7f / unitsPerRow;

            for (int i = 0; i < unitCount; i++)
            {
                GameObject indicator = new GameObject($"Unit_{i}");
                indicator.transform.SetParent(transform);

                int row = i / unitsPerRow;
                int col = i % unitsPerRow;
                float xPos = -0.3f + col * xSpacing;
                float yPos = (row == 0 ? 0.15f : -0.15f);
                indicator.transform.localPosition = new Vector3(xPos, yPos, 0);
                indicator.transform.localScale = new Vector3(0.12f, 0.2f, 1f);

                SpriteRenderer sr = indicator.AddComponent<SpriteRenderer>();
                sr.sprite = CreateWindowSprite();
                sr.color = emptyUnitColor;
                sr.sortingOrder = 3;

                unitIndicators.Add(indicator);
            }
        }

        private Sprite CreateWindowSprite()
        {
            Texture2D tex = new Texture2D(6, 10);
            Color[] colors = new Color[60];
            for (int i = 0; i < 60; i++)
            {
                colors[i] = Color.white;
            }
            tex.SetPixels(colors);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, 6, 10), new Vector2(0.5f, 0.5f), 6);
        }

        #endregion

        #region Visual Updates

        protected override void UpdateOccupancyVisuals()
        {
            UpdateVisualState();

            // Update unit windows based on occupancy and time
            for (int i = 0; i < unitIndicators.Count && i < unitsOccupancy.Count; i++)
            {
                SpriteRenderer sr = unitIndicators[i].GetComponent<SpriteRenderer>();
                if (sr == null) continue;

                if (unitsOccupancy[i] > 0)
                {
                    // Unit is occupied
                    if (IsSleepingTime)
                    {
                        sr.color = sleepingColor; // Dark at night
                    }
                    else
                    {
                        sr.color = occupiedUnitColor; // Lit during day
                    }
                }
                else
                {
                    sr.color = emptyUnitColor; // Always dark if empty
                }
            }
        }

        private void UpdateVisualState()
        {
            SpriteRenderer mainSR = GetComponent<SpriteRenderer>();
            if (mainSR == null) return;

            // Apartment building color based on time
            if (IsSleepingTime)
            {
                mainSR.color = new Color(0.4f, 0.4f, 0.5f); // Darker at night
            }
            else
            {
                mainSR.color = new Color(0.6f, 0.6f, 0.65f); // Normal during day
            }
        }

        #endregion

        #region Resident Management

        /// <summary>
        /// Register a new resident in a specific unit
        /// </summary>
        public bool RegisterResident(int unitIndex)
        {
            if (unitIndex < 0 || unitIndex >= unitCount) return false;
            if (unitsOccupancy[unitIndex] >= residentsPerUnit) return false;

            unitsOccupancy[unitIndex]++;
            totalResidents++;
            UpdateOccupancyVisuals();
            Debug.Log($"[Apartment] Resident registered to unit {unitIndex}. Total: {totalResidents}");
            return true;
        }

        /// <summary>
        /// Unregister a resident from a specific unit
        /// </summary>
        public void UnregisterResident(int unitIndex)
        {
            if (unitIndex < 0 || unitIndex >= unitCount) return;
            if (unitsOccupancy[unitIndex] <= 0) return;

            unitsOccupancy[unitIndex]--;
            totalResidents = Mathf.Max(0, totalResidents - 1);
            UpdateOccupancyVisuals();
            Debug.Log($"[Apartment] Resident unregistered from unit {unitIndex}. Total: {totalResidents}");
        }

        /// <summary>
        /// Resident returns home
        /// </summary>
        public override bool Enter(GameObject person)
        {
            if (base.Enter(person))
            {
                Debug.Log($"[Apartment] Resident returned home. At home: {OccupantCount}/{totalResidents}");
                return true;
            }
            return false;
        }

        /// <summary>
        /// Resident leaves home
        /// </summary>
        public override void Exit(GameObject person)
        {
            base.Exit(person);
            Debug.Log($"[Apartment] Resident left home. At home: {OccupantCount}/{totalResidents}");
        }

        /// <summary>
        /// Get the position for a resident in a specific unit
        /// </summary>
        public Vector2 GetUnitPosition(int unitIndex, int residentIndex = 0)
        {
            int positionIndex = unitIndex * residentsPerUnit + residentIndex;
            if (positionIndex >= 0 && positionIndex < occupantPositions.Count)
            {
                return occupantPositions[positionIndex].position;
            }
            return EntrancePosition;
        }

        #endregion

        #region Economy

        protected override long CalculateActualRent(long baseRent)
        {
            // Apartment rent is per occupied unit, not per resident
            int occupiedUnits = 0;
            foreach (int occupancy in unitsOccupancy)
            {
                if (occupancy > 0) occupiedUnits++;
            }

            // Calculate rent based on occupied units ratio
            float occupancyRatio = unitCount > 0 ? (float)occupiedUnits / unitCount : 0f;
            return (long)(baseRent * occupancyRatio);
        }

        /// <summary>
        /// Override IsOccupied to check for registered residents, not current presence
        /// </summary>
        public new bool IsOccupied => totalResidents > 0;

        #endregion

        #region Event Handlers

        protected override void OnHourChanged(int hour)
        {
            UpdateVisualState();
            UpdateOccupancyVisuals();

            // Log sleeping time changes
            if (hour == 22)
            {
                Debug.Log($"[Apartment] Night time - lights going off");
            }
            else if (hour == 6)
            {
                Debug.Log($"[Apartment] Morning - residents waking up");
            }
        }

        #endregion

        #region Debug

        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();

            // Draw apartment-specific info
            Gizmos.color = IsSleepingTime ? new Color(0.2f, 0.2f, 0.4f) : Color.white;
            Gizmos.DrawWireCube(transform.position + Vector3.up * 0.8f, new Vector3(0.5f, 0.2f, 0));
        }

        #endregion
    }
}
