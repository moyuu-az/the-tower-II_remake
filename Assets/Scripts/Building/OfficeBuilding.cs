using System.Collections.Generic;
using UnityEngine;
using TowerGame.Core;
using TowerGame.Economy;

namespace TowerGame.Building
{
    /// <summary>
    /// Office tenant type - where employees work during business hours
    /// The Tower II style office building
    /// </summary>
    public class OfficeBuilding : Tenant
    {
        [Header("Office-Specific Visual Settings")]
        [SerializeField] private Color emptyWindowColor = new Color(0.8f, 0.9f, 1f);
        [SerializeField] private Color occupiedWindowColor = new Color(1f, 0.9f, 0.5f);

        // Office-specific occupant indicators
        private List<GameObject> occupantIndicators = new List<GameObject>();

        // Legacy property aliases for backward compatibility
        public List<Transform> workPositions => occupantPositions;

        #region Tenant Implementation

        public override TenantType TenantType => TenantType.Office;

        #endregion

        #region Initialization

        protected override void Awake()
        {
            // Set default office hours
            openHour = 8;
            closeHour = 18;

            // Set default colors
            emptyColor = emptyWindowColor;
            occupiedColor = occupiedWindowColor;

            base.Awake();
        }

        protected override void Start()
        {
            base.Start();
            CreateOccupantIndicatorsInternal();
        }

        protected override void CreateDefaultOccupantPositions(float groundLocalY)
        {
            // Office uses 3 work positions spread across the width
            float[] xOffsets = { -0.3f, 0f, 0.3f };

            for (int i = 0; i < xOffsets.Length; i++)
            {
                var posGO = new GameObject($"WorkPosition_{i}");
                posGO.transform.SetParent(transform);
                posGO.transform.localPosition = new Vector3(xOffsets[i], groundLocalY, 0);
                occupantPositions.Add(posGO.transform);
            }

            Debug.Log($"[OfficeBuilding] Created {occupantPositions.Count} work positions");
        }

        private void CreateOccupantIndicatorsInternal()
        {
            for (int i = 0; i < occupantPositions.Count && i < windowRenderers.Count; i++)
            {
                GameObject indicator = new GameObject($"OccupantIndicator_{i}");
                indicator.transform.SetParent(windowRenderers[i].transform);
                indicator.transform.localPosition = Vector3.zero;
                indicator.transform.localScale = new Vector3(0.6f, 0.6f, 1f);

                SpriteRenderer sr = indicator.AddComponent<SpriteRenderer>();
                sr.sprite = CreateIndicatorSprite();
                sr.color = Color.clear;
                sr.sortingOrder = 5;

                occupantIndicators.Add(indicator);
            }
        }

        private Sprite CreateIndicatorSprite()
        {
            Texture2D tex = new Texture2D(16, 16);
            Color[] colors = new Color[16 * 16];

            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(8, 8));
                    colors[y * 16 + x] = dist < 6 ? Color.white : Color.clear;
                }
            }

            tex.SetPixels(colors);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16);
        }

        #endregion

        #region Visual Updates

        protected override void UpdateOccupancyVisuals()
        {
            base.UpdateOccupancyVisuals();

            // Update occupant indicators
            for (int i = 0; i < occupantIndicators.Count; i++)
            {
                bool isOccupied = occupiedPositionIndices.Contains(i % occupantPositions.Count);

                SpriteRenderer indicator = occupantIndicators[i].GetComponent<SpriteRenderer>();
                if (indicator != null)
                {
                    indicator.color = isOccupied ? new Color(0.2f, 0.2f, 0.2f, 0.8f) : Color.clear;
                }
            }
        }

        #endregion

        #region Legacy API (Backward Compatibility)

        /// <summary>
        /// Get an available work position (legacy API)
        /// </summary>
        public Vector2? GetAvailableWorkPosition()
        {
            return GetAvailablePosition();
        }

        /// <summary>
        /// Release a work position (legacy API)
        /// </summary>
        public void ReleaseWorkPosition(Vector2 position)
        {
            ReleasePosition(position);
        }

        /// <summary>
        /// Get work position by index (legacy API)
        /// </summary>
        public Vector2 GetWorkPosition(int index)
        {
            return GetPosition(index);
        }

        #endregion

        #region Economy

        protected override long CalculateActualRent(long baseRent)
        {
            // Office has no time-based bonus, just base rent
            return baseRent;
        }

        #endregion

        #region Debug

        protected override void OnDrawGizmos()
        {
            // Draw entrance
            Gizmos.color = Color.green;
            Vector3 entrance = entrancePoint != null ? entrancePoint.position : transform.position + Vector3.down * 2f;
            Gizmos.DrawWireSphere(entrance, 0.3f);

            // Draw work positions
            Gizmos.color = Color.blue;
            foreach (var pos in occupantPositions)
            {
                if (pos != null)
                {
                    Gizmos.DrawWireCube(pos.position, Vector3.one * 0.4f);
                }
            }
        }

        #endregion
    }
}
