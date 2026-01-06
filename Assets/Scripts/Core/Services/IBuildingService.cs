using System.Collections.Generic;
using UnityEngine;
using TowerGame.Building;

namespace TowerGame.Core.Services
{
    /// <summary>
    /// Service interface for building placement management.
    /// Handles floor system, lobbies, floors, and tenant placement validation.
    /// </summary>
    public interface IBuildingService : IService
    {
        /// <summary>
        /// Whether any lobby exists
        /// </summary>
        bool HasAnyLobby { get; }

        /// <summary>
        /// Validate lobby placement
        /// </summary>
        /// <param name="centerSegmentX">Center segment X position</param>
        /// <param name="widthSegments">Width in segments</param>
        /// <returns>Placement validation result</returns>
        PlacementResult ValidateLobbyPlacement(int centerSegmentX, int widthSegments);

        /// <summary>
        /// Validate floor structure placement
        /// </summary>
        /// <param name="centerSegmentX">Center segment X position</param>
        /// <param name="floorNumber">Floor number (0-indexed)</param>
        /// <param name="widthSegments">Width in segments</param>
        /// <returns>Placement validation result</returns>
        PlacementResult ValidateFloorPlacement(int centerSegmentX, int floorNumber, int widthSegments);

        /// <summary>
        /// Validate elevator placement
        /// </summary>
        /// <param name="segmentX">Segment X position</param>
        /// <returns>Placement validation result</returns>
        PlacementResult ValidateElevatorPlacement(int segmentX);

        /// <summary>
        /// Validate tenant placement (Office, Restaurant, etc.)
        /// </summary>
        /// <param name="centerSegmentX">Center segment X position</param>
        /// <param name="floorNumber">Floor number (0-indexed)</param>
        /// <param name="widthSegments">Width in segments</param>
        /// <returns>Placement validation result</returns>
        PlacementResult ValidateTenantPlacement(int centerSegmentX, int floorNumber, int widthSegments);

        /// <summary>
        /// Place a lobby
        /// </summary>
        /// <param name="position">World position</param>
        /// <param name="centerSegmentX">Center segment X position</param>
        /// <param name="widthSegments">Width in segments</param>
        /// <returns>Created lobby, or null if failed</returns>
        Lobby PlaceLobby(Vector3 position, int centerSegmentX, int widthSegments);

        /// <summary>
        /// Place a floor structure
        /// </summary>
        /// <param name="position">World position</param>
        /// <param name="centerSegmentX">Center segment X position</param>
        /// <param name="floorNumber">Floor number (0-indexed)</param>
        /// <param name="widthSegments">Width in segments</param>
        /// <returns>Created floor structure, or null if failed</returns>
        FloorStructure PlaceFloor(Vector3 position, int centerSegmentX, int floorNumber, int widthSegments);

        /// <summary>
        /// Get tower data at a given segment position
        /// </summary>
        TowerData GetTowerAtPosition(int segmentX);

        /// <summary>
        /// Get all tower data
        /// </summary>
        List<TowerData> GetAllTowers();

        /// <summary>
        /// Get floor structure at a position
        /// </summary>
        FloorStructure GetFloorAt(int segmentX, int floorNumber);

        /// <summary>
        /// Get building category for a type
        /// </summary>
        BuildingCategory GetBuildingCategory(BuildingType type);
    }
}
