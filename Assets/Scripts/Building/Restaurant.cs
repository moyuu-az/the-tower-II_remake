using System.Collections.Generic;
using UnityEngine;
using TowerGame.Core;
using TowerGame.Economy;

namespace TowerGame.Building
{
    /// <summary>
    /// Restaurant tenant type - serves food to visitors during meal hours
    /// The Tower II style restaurant with lunch/dinner time bonuses
    /// </summary>
    public class Restaurant : Tenant
    {
        [Header("Restaurant-Specific Settings")]
        [SerializeField] private int tableCount = 6;
        [SerializeField] private float lunchStartHour = 11f;
        [SerializeField] private float lunchEndHour = 14f;
        [SerializeField] private float dinnerStartHour = 18f;
        [SerializeField] private float dinnerEndHour = 21f;

        [Header("Restaurant Visual Settings")]
        [SerializeField] private Color closedColor = new Color(0.5f, 0.4f, 0.3f);
        [SerializeField] private Color openColor = new Color(0.9f, 0.7f, 0.4f);
        [SerializeField] private Color busyColor = new Color(1f, 0.6f, 0.2f);

        // Table occupancy
        private List<GameObject> tableIndicators = new List<GameObject>();
        private int currentCustomers = 0;

        #region Tenant Implementation

        public override TenantType TenantType => TenantType.Restaurant;

        #endregion

        #region Properties

        /// <summary>
        /// Whether it's currently lunch time
        /// </summary>
        public bool IsLunchTime
        {
            get
            {
                if (GameTimeManager.Instance == null) return false;
                float hour = GameTimeManager.Instance.CurrentHour;
                return hour >= lunchStartHour && hour < lunchEndHour;
            }
        }

        /// <summary>
        /// Whether it's currently dinner time
        /// </summary>
        public bool IsDinnerTime
        {
            get
            {
                if (GameTimeManager.Instance == null) return false;
                float hour = GameTimeManager.Instance.CurrentHour;
                return hour >= dinnerStartHour && hour < dinnerEndHour;
            }
        }

        /// <summary>
        /// Whether it's a peak time (lunch or dinner)
        /// </summary>
        public bool IsPeakTime => IsLunchTime || IsDinnerTime;

        /// <summary>
        /// Number of tables in this restaurant
        /// </summary>
        public int TableCount => tableCount;

        /// <summary>
        /// Current number of customers
        /// </summary>
        public int CurrentCustomers => currentCustomers;

        #endregion

        #region Initialization

        protected override void Awake()
        {
            // Set restaurant hours (11:00 - 22:00)
            openHour = 11;
            closeHour = 22;

            // Set capacity based on tables (2 customers per table)
            capacity = tableCount * 2;

            // Set colors
            emptyColor = openColor;
            occupiedColor = busyColor;

            base.Awake();
        }

        protected override void Start()
        {
            base.Start();
            CreateTableIndicators();
            UpdateVisualState();
        }

        protected override void CreateDefaultOccupantPositions(float groundLocalY)
        {
            // Create table positions spread across the restaurant
            int tableRows = 2;
            int tablesPerRow = (tableCount + 1) / 2;
            float xSpacing = 0.8f / tablesPerRow;
            float ySpacing = 0.4f;

            for (int row = 0; row < tableRows && occupantPositions.Count < tableCount; row++)
            {
                for (int col = 0; col < tablesPerRow && occupantPositions.Count < tableCount; col++)
                {
                    var posGO = new GameObject($"Table_{occupantPositions.Count}");
                    posGO.transform.SetParent(transform);

                    float xPos = -0.35f + col * xSpacing;
                    float yPos = groundLocalY + (row - 0.5f) * ySpacing;
                    posGO.transform.localPosition = new Vector3(xPos, yPos, 0);

                    occupantPositions.Add(posGO.transform);
                }
            }

            Debug.Log($"[Restaurant] Created {occupantPositions.Count} table positions");
        }

        private void CreateTableIndicators()
        {
            // Create visual indicators for tables
            foreach (var tablePos in occupantPositions)
            {
                GameObject indicator = new GameObject("TableIndicator");
                indicator.transform.SetParent(tablePos);
                indicator.transform.localPosition = Vector3.zero;
                indicator.transform.localScale = new Vector3(0.15f, 0.1f, 1f);

                SpriteRenderer sr = indicator.AddComponent<SpriteRenderer>();
                sr.sprite = CreateTableSprite();
                sr.color = new Color(0.6f, 0.4f, 0.2f); // Brown table
                sr.sortingOrder = 3;

                tableIndicators.Add(indicator);
            }
        }

        private Sprite CreateTableSprite()
        {
            Texture2D tex = new Texture2D(8, 4);
            Color[] colors = new Color[32];
            for (int i = 0; i < 32; i++)
            {
                colors[i] = Color.white;
            }
            tex.SetPixels(colors);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, 8, 4), new Vector2(0.5f, 0.5f), 8);
        }

        #endregion

        #region Visual Updates

        protected override void UpdateOccupancyVisuals()
        {
            // Update main building color based on state
            UpdateVisualState();

            // Update table indicators
            for (int i = 0; i < tableIndicators.Count; i++)
            {
                bool tableOccupied = occupiedPositionIndices.Contains(i);
                SpriteRenderer sr = tableIndicators[i].GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.color = tableOccupied ?
                        new Color(0.4f, 0.3f, 0.15f) : // Darker when occupied
                        new Color(0.6f, 0.4f, 0.2f);   // Normal brown
                }
            }
        }

        private void UpdateVisualState()
        {
            SpriteRenderer mainSR = GetComponent<SpriteRenderer>();
            if (mainSR == null) return;

            if (!IsOpen())
            {
                mainSR.color = closedColor;
            }
            else if (IsPeakTime && OccupantCount > capacity / 2)
            {
                mainSR.color = busyColor;
            }
            else
            {
                mainSR.color = openColor;
            }
        }

        #endregion

        #region Customer Management

        /// <summary>
        /// Customer enters the restaurant
        /// </summary>
        public override bool Enter(GameObject person)
        {
            if (base.Enter(person))
            {
                currentCustomers++;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Customer exits the restaurant
        /// </summary>
        public override void Exit(GameObject person)
        {
            base.Exit(person);
            currentCustomers = Mathf.Max(0, currentCustomers - 1);
        }

        protected override void OnOccupantEntered(GameObject person)
        {
            Debug.Log($"[Restaurant] Customer seated. Total: {currentCustomers}/{capacity}");
        }

        protected override void OnOccupantExited(GameObject person)
        {
            Debug.Log($"[Restaurant] Customer left. Remaining: {currentCustomers}/{capacity}");
        }

        #endregion

        #region Economy

        protected override long CalculateActualRent(long baseRent)
        {
            float multiplier = 1f;

            // Apply lunch/dinner bonuses from BuildingCosts
            if (EconomyManager.Instance?.Costs != null)
            {
                if (IsLunchTime)
                {
                    multiplier = EconomyManager.Instance.Costs.restaurantLunchBonus;
                }
                else if (IsDinnerTime)
                {
                    multiplier = EconomyManager.Instance.Costs.restaurantDinnerBonus;
                }
            }

            return (long)(baseRent * multiplier);
        }

        #endregion

        #region Event Handlers

        protected override void OnHourChanged(int hour)
        {
            UpdateVisualState();

            // Log peak time changes
            if (hour == (int)lunchStartHour)
            {
                Debug.Log($"[Restaurant] Lunch time started!");
            }
            else if (hour == (int)dinnerStartHour)
            {
                Debug.Log($"[Restaurant] Dinner time started!");
            }
        }

        #endregion

        #region Debug

        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();

            // Draw restaurant-specific info
            Gizmos.color = IsPeakTime ? Color.yellow : Color.white;
            Gizmos.DrawWireCube(transform.position + Vector3.up * 0.8f, new Vector3(0.5f, 0.2f, 0));
        }

        #endregion
    }
}
