using System.Collections.Generic;
using UnityEngine;
using TowerGame.Grid;

namespace TowerGame.Building
{
    /// <summary>
    /// Placement validation result
    /// </summary>
    public class PlacementResult
    {
        public bool IsValid { get; private set; }
        public string ErrorMessage { get; private set; }
        public string ErrorCode { get; private set; }

        public static PlacementResult Success()
        {
            return new PlacementResult { IsValid = true, ErrorMessage = "", ErrorCode = "" };
        }

        public static PlacementResult Error(string code, string message)
        {
            return new PlacementResult { IsValid = false, ErrorCode = code, ErrorMessage = message };
        }
    }

    /// <summary>
    /// Tower data for managing multiple towers
    /// </summary>
    [System.Serializable]
    public class TowerData
    {
        public int towerId;
        public Lobby lobby;
        public List<FloorStructure> floors = new List<FloorStructure>();

        public FloorStructure GetFloor(int floorNumber)
        {
            return floors.Find(f => f.FloorNumber == floorNumber);
        }

        public bool HasFloor(int floorNumber)
        {
            return floors.Exists(f => f.FloorNumber == floorNumber);
        }
    }

    /// <summary>
    /// FloorSystemManager - Central manager for The Tower II style floor system
    /// Handles placement validation and building hierarchy
    /// </summary>
    public class FloorSystemManager : MonoBehaviour
    {
        public static FloorSystemManager Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private int minimumTowerGap = 9; // Segments between independent towers

        [Header("Runtime (Read Only)")]
        [SerializeField] private List<TowerData> towers = new List<TowerData>();

        private GridManager gridManager;

        // Properties
        public bool HasAnyLobby => towers.Count > 0 && towers.Exists(t => t.lobby != null);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            gridManager = GridManager.Instance;
        }

        #region Validation Methods

        /// <summary>
        /// Validate lobby placement
        /// </summary>
        public PlacementResult ValidateLobbyPlacement(int centerSegmentX, int widthSegments)
        {
            // Rule: Lobby must be on floor 0 (1F)
            // Rule: Cannot overlap with existing towers

            int halfWidth = widthSegments / 2;
            int leftBound = centerSegmentX - halfWidth;
            int rightBound = centerSegmentX + halfWidth;

            // Check if overlaps with existing tower (or too close)
            foreach (var tower in towers)
            {
                if (tower.lobby != null)
                {
                    int existingLeft = tower.lobby.LeftBoundary - minimumTowerGap;
                    int existingRight = tower.lobby.RightBoundary + minimumTowerGap;

                    if (!(rightBound < existingLeft || leftBound > existingRight))
                    {
                        return PlacementResult.Error(
                            "LOBBY_TOO_CLOSE",
                            $"ロビーは既存タワーから{minimumTowerGap}セグメント以上離す必要があります"
                        );
                    }
                }
            }

            // Check grid bounds
            if (gridManager != null && gridManager.Config != null)
            {
                int maxWidth = gridManager.Config.maxWidthSegments / 2;
                if (leftBound < -maxWidth || rightBound > maxWidth)
                {
                    return PlacementResult.Error("OUT_OF_BOUNDS", "配置範囲外です");
                }
            }

            return PlacementResult.Success();
        }

        /// <summary>
        /// Validate floor placement
        /// </summary>
        public PlacementResult ValidateFloorPlacement(int centerSegmentX, int floorNumber, int widthSegments)
        {
            // Rule: Floor cannot be on 1F (use Lobby instead)
            if (floorNumber == 0)
            {
                return PlacementResult.Error("USE_LOBBY", "1Fにはロビーを配置してください");
            }

            // Rule: Must have lobby first
            var tower = GetTowerAtPosition(centerSegmentX);
            if (tower == null || tower.lobby == null)
            {
                return PlacementResult.Error("LOBBY_REQUIRED", "先にロビーを配置してください");
            }

            int halfWidth = widthSegments / 2;
            int leftBound = centerSegmentX - halfWidth;
            int rightBound = centerSegmentX + halfWidth;

            // Rule: Cannot exceed lobby width
            if (!tower.lobby.CanFitRange(leftBound, rightBound))
            {
                return PlacementResult.Error(
                    "EXCEEDS_BOUNDARY",
                    "ロビーの幅を超えることはできません"
                );
            }

            // Rule: Must have full support from below (100% - no overhang)
            for (int x = leftBound; x <= rightBound; x++)
            {
                if (!HasSupportAt(tower, x, floorNumber - 1))
                {
                    return PlacementResult.Error(
                        "NO_SUPPORT",
                        "下階の支持構造が不足しています（オーバーハング不可）"
                    );
                }
            }

            // Rule: Cannot overlap with existing floor at same level
            var existingFloor = tower.GetFloor(floorNumber);
            if (existingFloor != null)
            {
                // Check for overlap
                if (!(rightBound < existingFloor.LeftBoundary || leftBound > existingFloor.RightBoundary))
                {
                    return PlacementResult.Error("FLOOR_EXISTS", "このフロアには既に構造体が存在します");
                }
            }

            return PlacementResult.Success();
        }

        /// <summary>
        /// Validate elevator placement
        /// </summary>
        public PlacementResult ValidateElevatorPlacement(int segmentX)
        {
            // Rule: Must have lobby first
            var tower = GetTowerAtPosition(segmentX);
            if (tower == null || tower.lobby == null)
            {
                return PlacementResult.Error("LOBBY_REQUIRED", "先にロビーを配置してください");
            }

            // Rule: Must be within lobby boundary
            if (!tower.lobby.IsWithinBoundary(segmentX))
            {
                return PlacementResult.Error("OUTSIDE_LOBBY", "エレベーターはロビーの範囲内に配置してください");
            }

            // Rule: Check for existing elevator at this position
            if (ElevatorManager.Instance != null)
            {
                var existingShaft = ElevatorManager.Instance.GetShaftAtPosition(segmentX, tower.towerId);
                if (existingShaft != null)
                {
                    return PlacementResult.Error("ELEVATOR_EXISTS", "この位置には既にエレベーターがあります");
                }
            }

            return PlacementResult.Success();
        }

        /// <summary>
        /// Validate tenant (Office, etc.) placement
        /// </summary>
        public PlacementResult ValidateTenantPlacement(int centerSegmentX, int floorNumber, int widthSegments)
        {
            // Rule: Cannot place tenant on 1F (lobby floor)
            if (floorNumber == 0)
            {
                return PlacementResult.Error(
                    "LOBBY_FLOOR",
                    "ロビー階にテナントは配置できません"
                );
            }

            // Rule: Must have lobby first
            var tower = GetTowerAtPosition(centerSegmentX);
            if (tower == null || tower.lobby == null)
            {
                return PlacementResult.Error("LOBBY_REQUIRED", "先にロビーを配置してください");
            }

            // Rule: Floor structure must exist
            var floor = tower.GetFloor(floorNumber);
            if (floor == null)
            {
                return PlacementResult.Error(
                    "FLOOR_REQUIRED",
                    "先にフロア構造体を配置してください"
                );
            }

            int halfWidth = widthSegments / 2;
            int leftBound = centerSegmentX - halfWidth;
            int rightBound = centerSegmentX + halfWidth;

            // Rule: Must fit within floor's available space
            if (!floor.CanOccupyRange(leftBound, rightBound))
            {
                return PlacementResult.Error(
                    "NO_SPACE",
                    "フロア上に十分な空きスペースがありません"
                );
            }

            return PlacementResult.Success();
        }

        #endregion

        #region Placement Methods

        /// <summary>
        /// Place a lobby
        /// </summary>
        public Lobby PlaceLobby(Vector3 position, int centerSegmentX, int widthSegments)
        {
            var result = ValidateLobbyPlacement(centerSegmentX, widthSegments);
            if (!result.IsValid)
            {
                Debug.LogWarning($"[FloorSystemManager] Cannot place lobby: {result.ErrorMessage}");
                return null;
            }

            // Create new tower
            int newTowerId = towers.Count;
            var towerData = new TowerData { towerId = newTowerId };

            // Create lobby GameObject
            GameObject lobbyGO = new GameObject($"Lobby_Tower{newTowerId}");
            lobbyGO.transform.position = position;

            // Set size
            if (gridManager != null && gridManager.Config != null)
            {
                Vector2 size = gridManager.Config.GetBuildingWorldSize(widthSegments, 1);
                lobbyGO.transform.localScale = new Vector3(size.x, size.y, 1f);
            }

            // Add visual
            SpriteRenderer sr = lobbyGO.AddComponent<SpriteRenderer>();
            sr.sprite = CreateSimpleSprite();
            sr.color = new Color(0.9f, 0.85f, 0.7f); // Warm beige
            sr.sortingOrder = 1;

            // Add Lobby component
            Lobby lobby = lobbyGO.AddComponent<Lobby>();
            lobby.Initialize(centerSegmentX, widthSegments, newTowerId);

            towerData.lobby = lobby;
            towers.Add(towerData);

            // Register with grid manager
            if (gridManager != null)
            {
                gridManager.RegisterBuilding(lobbyGO, centerSegmentX, 0, widthSegments, 1);
            }

            Debug.Log($"[FloorSystemManager] Lobby placed for Tower {newTowerId}");
            return lobby;
        }

        /// <summary>
        /// Place a floor structure
        /// </summary>
        public FloorStructure PlaceFloor(Vector3 position, int centerSegmentX, int floorNumber, int widthSegments)
        {
            var result = ValidateFloorPlacement(centerSegmentX, floorNumber, widthSegments);
            if (!result.IsValid)
            {
                Debug.LogWarning($"[FloorSystemManager] Cannot place floor: {result.ErrorMessage}");
                return null;
            }

            var tower = GetTowerAtPosition(centerSegmentX);
            if (tower == null) return null;

            // Create floor GameObject
            GameObject floorGO = new GameObject($"Floor_{floorNumber + 1}F_Tower{tower.towerId}");
            floorGO.transform.position = position;

            // Set size
            if (gridManager != null && gridManager.Config != null)
            {
                Vector2 size = gridManager.Config.GetBuildingWorldSize(widthSegments, 1);
                floorGO.transform.localScale = new Vector3(size.x, size.y, 1f);
            }

            // Add visual
            SpriteRenderer sr = floorGO.AddComponent<SpriteRenderer>();
            sr.sprite = CreateSimpleSprite();
            sr.color = new Color(0.7f, 0.7f, 0.75f); // Gray
            sr.sortingOrder = 1;

            // Add FloorStructure component
            FloorStructure floor = floorGO.AddComponent<FloorStructure>();
            floor.Initialize(centerSegmentX, floorNumber, widthSegments, tower.towerId);

            tower.floors.Add(floor);

            // Register with grid manager
            if (gridManager != null)
            {
                gridManager.RegisterBuilding(floorGO, centerSegmentX, floorNumber, widthSegments, 1);
            }

            Debug.Log($"[FloorSystemManager] Floor {floorNumber + 1}F placed for Tower {tower.towerId}");
            return floor;
        }

        #endregion

        #region Query Methods

        /// <summary>
        /// Get tower at a given segment position
        /// </summary>
        public TowerData GetTowerAtPosition(int segmentX)
        {
            foreach (var tower in towers)
            {
                if (tower.lobby != null && tower.lobby.IsWithinBoundary(segmentX))
                {
                    return tower;
                }
            }
            return null;
        }

        /// <summary>
        /// Get all towers
        /// </summary>
        public List<TowerData> GetAllTowers()
        {
            return new List<TowerData>(towers);
        }

        /// <summary>
        /// Check if there's support at a specific position
        /// </summary>
        private bool HasSupportAt(TowerData tower, int segmentX, int floorNumber)
        {
            if (floorNumber < 0) return false;

            if (floorNumber == 0)
            {
                // 1F is supported by lobby
                return tower.lobby != null && tower.lobby.IsWithinBoundary(segmentX);
            }
            else
            {
                // Check floor below
                var floorBelow = tower.GetFloor(floorNumber);
                return floorBelow != null && floorBelow.IsWithinBoundary(segmentX);
            }
        }

        /// <summary>
        /// Get floor structure at a position
        /// </summary>
        public FloorStructure GetFloorAt(int segmentX, int floorNumber)
        {
            var tower = GetTowerAtPosition(segmentX);
            if (tower == null) return null;
            return tower.GetFloor(floorNumber);
        }

        /// <summary>
        /// Get building category for a type
        /// </summary>
        public static BuildingCategory GetCategory(BuildingType type)
        {
            switch (type)
            {
                case BuildingType.Lobby:
                    return BuildingCategory.Foundation;
                case BuildingType.Floor:
                    return BuildingCategory.Structure;
                case BuildingType.Office:
                case BuildingType.Restaurant:
                case BuildingType.Shop:
                case BuildingType.Apartment:
                    return BuildingCategory.Tenant;
                case BuildingType.Elevator:
                    return BuildingCategory.Transportation;
                case BuildingType.Demolition:
                    return BuildingCategory.Special;
                default:
                    return BuildingCategory.None;
            }
        }

        #endregion

        #region Helper Methods

        private Sprite CreateSimpleSprite()
        {
            Texture2D tex = new Texture2D(4, 4);
            Color[] colors = new Color[16];
            for (int i = 0; i < 16; i++)
            {
                colors[i] = Color.white;
            }
            tex.SetPixels(colors);
            tex.Apply();
            tex.filterMode = FilterMode.Point;

            return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);
        }

        #endregion
    }
}
