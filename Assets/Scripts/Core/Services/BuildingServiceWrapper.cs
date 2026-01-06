using System;
using System.Collections.Generic;
using UnityEngine;
using TowerGame.Building;

namespace TowerGame.Core.Services
{
    /// <summary>
    /// Wrapper class that adapts FloorSystemManager to IBuildingService interface.
    /// </summary>
    public class BuildingServiceWrapper : IBuildingService
    {
        private readonly FloorSystemManager manager;

        public BuildingServiceWrapper(FloorSystemManager manager)
        {
            this.manager = manager ?? throw new ArgumentNullException(nameof(manager));
        }

        #region IService Implementation

        public void Initialize()
        {
            Debug.Log("[BuildingServiceWrapper] Initialized");
        }

        public void Shutdown()
        {
            Debug.Log("[BuildingServiceWrapper] Shutdown");
        }

        #endregion

        #region IBuildingService Implementation

        public bool HasAnyLobby => manager.HasAnyLobby;

        public PlacementResult ValidateLobbyPlacement(int centerSegmentX, int widthSegments)
            => manager.ValidateLobbyPlacement(centerSegmentX, widthSegments);

        public PlacementResult ValidateFloorPlacement(int centerSegmentX, int floorNumber, int widthSegments)
            => manager.ValidateFloorPlacement(centerSegmentX, floorNumber, widthSegments);

        public PlacementResult ValidateElevatorPlacement(int segmentX)
            => manager.ValidateElevatorPlacement(segmentX);

        public PlacementResult ValidateTenantPlacement(int centerSegmentX, int floorNumber, int widthSegments)
            => manager.ValidateTenantPlacement(centerSegmentX, floorNumber, widthSegments);

        public Lobby PlaceLobby(Vector3 position, int centerSegmentX, int widthSegments)
            => manager.PlaceLobby(position, centerSegmentX, widthSegments);

        public FloorStructure PlaceFloor(Vector3 position, int centerSegmentX, int floorNumber, int widthSegments)
            => manager.PlaceFloor(position, centerSegmentX, floorNumber, widthSegments);

        public TowerData GetTowerAtPosition(int segmentX)
            => manager.GetTowerAtPosition(segmentX);

        public List<TowerData> GetAllTowers()
            => manager.GetAllTowers();

        public FloorStructure GetFloorAt(int segmentX, int floorNumber)
            => manager.GetFloorAt(segmentX, floorNumber);

        public BuildingCategory GetBuildingCategory(BuildingType type)
            => FloorSystemManager.GetCategory(type);

        #endregion
    }
}
