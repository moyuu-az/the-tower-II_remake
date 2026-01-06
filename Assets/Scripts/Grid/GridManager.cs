using System.Collections.Generic;
using UnityEngine;
using TowerGame.Building;

namespace TowerGame.Grid
{
    /// <summary>
    /// Manages the grid system for building placement - The Tower II style
    /// </summary>
    public class GridManager : MonoBehaviour
    {
        public static GridManager Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private GridConfig config;

        [Header("Debug")]
        [SerializeField] private bool showGridGizmos = true;
        [SerializeField] private Color gridColor = new Color(1f, 1f, 1f, 0.2f);
        [SerializeField] private Color occupiedColor = new Color(1f, 0f, 0f, 0.3f);

        // Occupancy tracking: key = (segmentX, floor), value = building reference
        private Dictionary<Vector2Int, GameObject> occupancyGrid = new Dictionary<Vector2Int, GameObject>();

        // Properties
        public GridConfig Config => config;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Create default config if not assigned
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<GridConfig>();
                Debug.Log("[GridManager] Using default grid configuration");
            }
        }

        /// <summary>
        /// Check if a building can be placed at the given grid position
        /// </summary>
        public bool CanPlaceBuilding(int startSegmentX, int startFloor, int widthSegments, int heightFloors)
        {
            // Check bounds
            if (startFloor < 0 || startFloor + heightFloors > config.maxFloors)
            {
                return false;
            }

            int halfWidth = widthSegments / 2;
            int minX = startSegmentX - halfWidth;
            int maxX = startSegmentX + halfWidth;

            if (minX < -config.maxWidthSegments / 2 || maxX > config.maxWidthSegments / 2)
            {
                return false;
            }

            // Check for floor 0 - must be on ground
            if (startFloor == 0)
            {
                // Ground floor is always valid (for now)
            }
            else
            {
                // Upper floors need FULL support below (100% - no overhang allowed)
                // In The Tower II style, buildings must be placed on existing floor structures
                for (int x = minX; x <= maxX; x++)
                {
                    Vector2Int belowPos = new Vector2Int(x, startFloor - 1);
                    if (!occupancyGrid.ContainsKey(belowPos))
                    {
                        return false; // No support at this segment - overhang not allowed
                    }
                }
            }

            // Check for collision with existing buildings
            for (int x = minX; x <= maxX; x++)
            {
                for (int y = startFloor; y < startFloor + heightFloors; y++)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    if (occupancyGrid.ContainsKey(pos))
                    {
                        return false; // Collision
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Check if a floor has any buildings
        /// </summary>
        public bool HasBuildingsOnFloor(int floor)
        {
            foreach (var kvp in occupancyGrid)
            {
                if (kvp.Key.y == floor)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Get the highest occupied floor
        /// </summary>
        public int GetHighestFloor()
        {
            int highest = 0;
            foreach (var kvp in occupancyGrid)
            {
                if (kvp.Key.y > highest)
                {
                    highest = kvp.Key.y;
                }
            }
            return highest;
        }

        /// <summary>
        /// Register a building in the occupancy grid
        /// </summary>
        public void RegisterBuilding(GameObject building, int startSegmentX, int startFloor, int widthSegments, int heightFloors)
        {
            int halfWidth = widthSegments / 2;

            for (int x = startSegmentX - halfWidth; x <= startSegmentX + halfWidth; x++)
            {
                for (int y = startFloor; y < startFloor + heightFloors; y++)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    occupancyGrid[pos] = building;
                }
            }

            Debug.Log($"[GridManager] Registered building at segment {startSegmentX}, floor {startFloor}");
        }

        /// <summary>
        /// Unregister a building from the occupancy grid
        /// </summary>
        public void UnregisterBuilding(GameObject building)
        {
            List<Vector2Int> toRemove = new List<Vector2Int>();

            foreach (var kvp in occupancyGrid)
            {
                if (kvp.Value == building)
                {
                    toRemove.Add(kvp.Key);
                }
            }

            foreach (var pos in toRemove)
            {
                occupancyGrid.Remove(pos);
            }
        }

        /// <summary>
        /// Get the floor number at a given world Y position
        /// </summary>
        public int GetFloorAtWorldY(float worldY)
        {
            return Mathf.FloorToInt((worldY - config.groundLevel) / config.floorHeight);
        }

        /// <summary>
        /// Get the world Y position for a floor
        /// </summary>
        public float GetWorldYForFloor(int floor)
        {
            return config.groundLevel + (floor * config.floorHeight) + (config.floorHeight / 2f);
        }

        /// <summary>
        /// Snap a world position to the grid
        /// </summary>
        public Vector3 SnapToGrid(Vector3 worldPos, int widthSegments, int heightFloors)
        {
            return config.SnapToGrid(worldPos, widthSegments, heightFloors);
        }

        /// <summary>
        /// Convert world position to grid coordinates
        /// </summary>
        public Vector2Int WorldToGrid(Vector3 worldPos)
        {
            return config.WorldToGrid(worldPos);
        }

        private void OnDrawGizmos()
        {
            if (!showGridGizmos || config == null) return;

            // Draw grid lines
            Gizmos.color = gridColor;

            float startX = -config.maxWidthSegments / 2 * config.segmentWidth;
            float endX = config.maxWidthSegments / 2 * config.segmentWidth;

            // Horizontal lines (floor separators)
            for (int floor = 0; floor <= config.maxFloors; floor++)
            {
                float y = config.groundLevel + (floor * config.floorHeight);
                Gizmos.DrawLine(new Vector3(startX, y, 0), new Vector3(endX, y, 0));
            }

            // Vertical lines (segment separators) - only draw every 9 segments for clarity
            for (int seg = -config.maxWidthSegments / 2; seg <= config.maxWidthSegments / 2; seg += config.officeWidthSegments)
            {
                float x = seg * config.segmentWidth;
                float topY = config.groundLevel + (config.maxFloors * config.floorHeight);
                Gizmos.DrawLine(new Vector3(x, config.groundLevel, 0), new Vector3(x, topY, 0));
            }

            // Draw occupied cells
            Gizmos.color = occupiedColor;
            foreach (var kvp in occupancyGrid)
            {
                Vector3 center = config.GridToWorld(kvp.Key.x, kvp.Key.y);
                Gizmos.DrawCube(center, new Vector3(config.segmentWidth * 0.9f, config.floorHeight * 0.9f, 0.1f));
            }
        }
    }
}
