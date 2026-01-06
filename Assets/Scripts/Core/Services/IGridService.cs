using UnityEngine;

namespace TowerGame.Core.Services
{
    /// <summary>
    /// Service interface for grid management.
    /// Handles building placement validation, grid snapping, and occupancy tracking.
    /// </summary>
    public interface IGridService : IService
    {
        /// <summary>
        /// Grid configuration
        /// </summary>
        Grid.GridConfig Config { get; }

        /// <summary>
        /// Check if a building can be placed at the given position
        /// </summary>
        /// <param name="startSegmentX">Center segment X position</param>
        /// <param name="startFloor">Starting floor number</param>
        /// <param name="widthSegments">Width in segments</param>
        /// <param name="heightFloors">Height in floors</param>
        /// <returns>True if placement is valid</returns>
        bool CanPlaceBuilding(int startSegmentX, int startFloor, int widthSegments, int heightFloors);

        /// <summary>
        /// Check if a floor has any buildings
        /// </summary>
        bool HasBuildingsOnFloor(int floor);

        /// <summary>
        /// Get the highest occupied floor
        /// </summary>
        int GetHighestFloor();

        /// <summary>
        /// Register a building in the occupancy grid
        /// </summary>
        void RegisterBuilding(GameObject building, int startSegmentX, int startFloor, int widthSegments, int heightFloors);

        /// <summary>
        /// Unregister a building from the occupancy grid
        /// </summary>
        void UnregisterBuilding(GameObject building);

        /// <summary>
        /// Get the floor number at a world Y position
        /// </summary>
        int GetFloorAtWorldY(float worldY);

        /// <summary>
        /// Get the world Y position for a floor
        /// </summary>
        float GetWorldYForFloor(int floor);

        /// <summary>
        /// Snap a world position to the grid
        /// </summary>
        Vector3 SnapToGrid(Vector3 worldPos, int widthSegments, int heightFloors);

        /// <summary>
        /// Convert world position to grid coordinates
        /// </summary>
        Vector2Int WorldToGrid(Vector3 worldPos);
    }
}
