using System;
using UnityEngine;
using TowerGame.Grid;

namespace TowerGame.Core.Services
{
    /// <summary>
    /// Wrapper class that adapts GridManager to IGridService interface.
    /// </summary>
    public class GridServiceWrapper : IGridService
    {
        private readonly GridManager manager;

        public GridServiceWrapper(GridManager manager)
        {
            this.manager = manager ?? throw new ArgumentNullException(nameof(manager));
        }

        #region IService Implementation

        public void Initialize()
        {
            Debug.Log("[GridServiceWrapper] Initialized");
        }

        public void Shutdown()
        {
            Debug.Log("[GridServiceWrapper] Shutdown");
        }

        #endregion

        #region IGridService Implementation

        public GridConfig Config => manager.Config;

        public bool CanPlaceBuilding(int startSegmentX, int startFloor, int widthSegments, int heightFloors)
            => manager.CanPlaceBuilding(startSegmentX, startFloor, widthSegments, heightFloors);

        public bool HasBuildingsOnFloor(int floor) => manager.HasBuildingsOnFloor(floor);

        public int GetHighestFloor() => manager.GetHighestFloor();

        public void RegisterBuilding(GameObject building, int startSegmentX, int startFloor, int widthSegments, int heightFloors)
            => manager.RegisterBuilding(building, startSegmentX, startFloor, widthSegments, heightFloors);

        public void UnregisterBuilding(GameObject building) => manager.UnregisterBuilding(building);

        public int GetFloorAtWorldY(float worldY) => manager.GetFloorAtWorldY(worldY);

        public float GetWorldYForFloor(int floor) => manager.GetWorldYForFloor(floor);

        public Vector3 SnapToGrid(Vector3 worldPos, int widthSegments, int heightFloors)
            => manager.SnapToGrid(worldPos, widthSegments, heightFloors);

        public Vector2Int WorldToGrid(Vector3 worldPos) => manager.WorldToGrid(worldPos);

        #endregion
    }
}
