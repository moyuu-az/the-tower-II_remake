using UnityEngine;

namespace TowerGame.Grid
{
    /// <summary>
    /// Configuration for the grid system - The Tower II style
    /// </summary>
    [CreateAssetMenu(fileName = "GridConfig", menuName = "Tower Game/Grid Config")]
    public class GridConfig : ScriptableObject
    {
        [Header("Grid Dimensions")]
        [Tooltip("Width of one segment in Unity units")]
        public float segmentWidth = 1f;

        [Tooltip("Height of one floor in Unity units")]
        public float floorHeight = 3f;

        [Header("World Settings")]
        [Tooltip("Ground level Y position")]
        public float groundLevel = -3f;

        [Tooltip("Maximum number of floors")]
        public int maxFloors = 15;

        [Tooltip("Maximum width in segments")]
        public int maxWidthSegments = 40;

        [Header("Building Sizes (in segments/floors)")]
        [Tooltip("Office width in segments")]
        public int officeWidthSegments = 9;

        [Tooltip("Office height in floors")]
        public int officeHeightFloors = 1;

        /// <summary>
        /// Convert grid position to world position
        /// </summary>
        public Vector3 GridToWorld(int segmentX, int floor)
        {
            float worldX = segmentX * segmentWidth;
            float worldY = groundLevel + (floor * floorHeight) + (floorHeight / 2f);
            return new Vector3(worldX, worldY, 0);
        }

        /// <summary>
        /// Convert world position to grid position
        /// </summary>
        public Vector2Int WorldToGrid(Vector3 worldPos)
        {
            int segmentX = Mathf.RoundToInt(worldPos.x / segmentWidth);
            int floor = Mathf.FloorToInt((worldPos.y - groundLevel) / floorHeight);
            return new Vector2Int(segmentX, Mathf.Max(0, floor));
        }

        /// <summary>
        /// Snap world position to grid
        /// </summary>
        public Vector3 SnapToGrid(Vector3 worldPos, int buildingWidthSegments, int buildingHeightFloors)
        {
            // Calculate grid position
            Vector2Int gridPos = WorldToGrid(worldPos);

            // Snap to valid position (center of building)
            float worldX = gridPos.x * segmentWidth;
            float worldY = groundLevel + (gridPos.y * floorHeight) + (buildingHeightFloors * floorHeight / 2f);

            return new Vector3(worldX, worldY, 0);
        }

        /// <summary>
        /// Get world size for a building
        /// </summary>
        public Vector2 GetBuildingWorldSize(int widthSegments, int heightFloors)
        {
            return new Vector2(widthSegments * segmentWidth, heightFloors * floorHeight);
        }

        /// <summary>
        /// Get the Y position where people walk on a floor (floor level, not center)
        /// Floor 0 (1F) = groundLevel, Floor 1 (2F) = groundLevel + floorHeight, etc.
        /// </summary>
        public float GetFloorWalkLevel(int floor)
        {
            return groundLevel + (floor * floorHeight);
        }

        /// <summary>
        /// Get the Y position of floor center (for building placement)
        /// </summary>
        public float GetFloorCenterY(int floor)
        {
            return groundLevel + (floor * floorHeight) + (floorHeight / 2f);
        }
    }
}
