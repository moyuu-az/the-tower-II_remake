using System.Collections.Generic;
using UnityEngine;
using TowerGame.Core;
using TowerGame.Economy;

namespace TowerGame.Building
{
    /// <summary>
    /// Shop tenant type - retail store for visitors to browse and purchase
    /// The Tower II style shop with weekend bonuses
    /// </summary>
    public class Shop : Tenant
    {
        [Header("Shop-Specific Settings")]
        [SerializeField] private int displayCount = 4;
        [SerializeField] private float browseTimeMin = 30f; // Seconds in game time
        [SerializeField] private float browseTimeMax = 120f;

        [Header("Shop Visual Settings")]
        [SerializeField] private Color closedColor = new Color(0.4f, 0.4f, 0.5f);
        [SerializeField] private Color openColor = new Color(0.5f, 0.7f, 0.9f);
        [SerializeField] private Color busyColor = new Color(0.3f, 0.5f, 0.8f);

        // Display indicators
        private List<GameObject> displayIndicators = new List<GameObject>();
        private int currentShoppers = 0;

        #region Tenant Implementation

        public override TenantType TenantType => TenantType.Shop;

        #endregion

        #region Properties

        /// <summary>
        /// Whether it's a weekend (Saturday or Sunday)
        /// </summary>
        public bool IsWeekend
        {
            get
            {
                if (GameTimeManager.Instance == null) return false;
                int dayOfWeek = GameTimeManager.Instance.CurrentDay % 7;
                return dayOfWeek == 5 || dayOfWeek == 6; // Saturday = 5, Sunday = 6
            }
        }

        /// <summary>
        /// Number of display areas
        /// </summary>
        public int DisplayCount => displayCount;

        /// <summary>
        /// Current number of shoppers
        /// </summary>
        public int CurrentShoppers => currentShoppers;

        /// <summary>
        /// Get random browse time for a visitor
        /// </summary>
        public float GetRandomBrowseTime()
        {
            return Random.Range(browseTimeMin, browseTimeMax);
        }

        #endregion

        #region Initialization

        protected override void Awake()
        {
            // Set shop hours (10:00 - 21:00)
            openHour = 10;
            closeHour = 21;

            // Set capacity based on displays (3 customers per display)
            capacity = displayCount * 3;

            // Set colors
            emptyColor = openColor;
            occupiedColor = busyColor;

            base.Awake();
        }

        protected override void Start()
        {
            base.Start();
            CreateDisplayIndicators();
            UpdateVisualState();
        }

        protected override void CreateDefaultOccupantPositions(float groundLocalY)
        {
            // Create shopper positions spread around displays
            float xSpacing = 0.6f / displayCount;

            for (int i = 0; i < displayCount * 2 && occupantPositions.Count < capacity; i++)
            {
                var posGO = new GameObject($"ShopperPosition_{occupantPositions.Count}");
                posGO.transform.SetParent(transform);

                float xPos = -0.25f + (i % displayCount) * xSpacing;
                float yPos = groundLocalY + (i < displayCount ? 0.1f : -0.1f);
                posGO.transform.localPosition = new Vector3(xPos, yPos, 0);

                occupantPositions.Add(posGO.transform);
            }

            Debug.Log($"[Shop] Created {occupantPositions.Count} shopper positions");
        }

        private void CreateDisplayIndicators()
        {
            // Create visual indicators for displays
            float xSpacing = 0.6f / displayCount;

            for (int i = 0; i < displayCount; i++)
            {
                GameObject indicator = new GameObject($"Display_{i}");
                indicator.transform.SetParent(transform);
                indicator.transform.localPosition = new Vector3(-0.25f + i * xSpacing, 0.2f, 0);
                indicator.transform.localScale = new Vector3(0.08f, 0.15f, 1f);

                SpriteRenderer sr = indicator.AddComponent<SpriteRenderer>();
                sr.sprite = CreateDisplaySprite();
                sr.color = new Color(0.9f, 0.9f, 0.95f); // Light gray display
                sr.sortingOrder = 3;

                displayIndicators.Add(indicator);
            }
        }

        private Sprite CreateDisplaySprite()
        {
            Texture2D tex = new Texture2D(4, 8);
            Color[] colors = new Color[32];
            for (int i = 0; i < 32; i++)
            {
                colors[i] = Color.white;
            }
            tex.SetPixels(colors);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, 4, 8), new Vector2(0.5f, 0.5f), 4);
        }

        #endregion

        #region Visual Updates

        protected override void UpdateOccupancyVisuals()
        {
            UpdateVisualState();

            // Update display brightness based on activity
            float activityLevel = (float)OccupantCount / capacity;
            foreach (var display in displayIndicators)
            {
                SpriteRenderer sr = display.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    float brightness = 0.8f + activityLevel * 0.2f;
                    sr.color = new Color(brightness, brightness, brightness + 0.05f);
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
            else if (OccupantCount > capacity / 2)
            {
                mainSR.color = busyColor;
            }
            else
            {
                mainSR.color = openColor;
            }
        }

        #endregion

        #region Shopper Management

        /// <summary>
        /// Shopper enters the shop
        /// </summary>
        public override bool Enter(GameObject person)
        {
            if (base.Enter(person))
            {
                currentShoppers++;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Shopper exits the shop
        /// </summary>
        public override void Exit(GameObject person)
        {
            base.Exit(person);
            currentShoppers = Mathf.Max(0, currentShoppers - 1);
        }

        protected override void OnOccupantEntered(GameObject person)
        {
            Debug.Log($"[Shop] Shopper entered. Total: {currentShoppers}/{capacity}");
        }

        protected override void OnOccupantExited(GameObject person)
        {
            Debug.Log($"[Shop] Shopper left. Remaining: {currentShoppers}/{capacity}");
        }

        #endregion

        #region Economy

        protected override long CalculateActualRent(long baseRent)
        {
            float multiplier = 1f;

            // Apply weekend bonus from BuildingCosts
            if (IsWeekend && EconomyManager.Instance?.Costs != null)
            {
                multiplier = EconomyManager.Instance.Costs.shopWeekendBonus;
            }

            return (long)(baseRent * multiplier);
        }

        #endregion

        #region Event Handlers

        protected override void OnHourChanged(int hour)
        {
            UpdateVisualState();
        }

        protected override void OnDayChanged(int day)
        {
            if (IsWeekend)
            {
                Debug.Log($"[Shop] Weekend day! Bonus income active.");
            }
        }

        #endregion

        #region Debug

        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();

            // Draw shop-specific info
            Gizmos.color = IsWeekend ? Color.cyan : Color.white;
            Gizmos.DrawWireCube(transform.position + Vector3.up * 0.8f, new Vector3(0.5f, 0.2f, 0));
        }

        #endregion
    }
}
