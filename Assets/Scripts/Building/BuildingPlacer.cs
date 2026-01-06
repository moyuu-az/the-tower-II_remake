using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TowerGame.Core;
using TowerGame.Grid;
using TowerGame.Economy;

namespace TowerGame.Building
{
    /// <summary>
    /// Building types that can be placed
    /// </summary>
    public enum BuildingType
    {
        None,
        Lobby,      // Required first on 1F - defines building boundary
        Floor,      // Horizontal space for tenants on 2F+
        Office,     // Tenant type - requires Floor or Lobby below
        Restaurant, // Tenant type - food service
        Shop,       // Tenant type - retail
        Apartment,  // Tenant type - residential
        Elevator,   // Vertical transportation - must be within lobby boundary
        Demolition  // Special mode for removing buildings
    }

    /// <summary>
    /// Building category for classification
    /// </summary>
    public enum BuildingCategory
    {
        None,
        Foundation,     // Lobby
        Structure,      // Floor
        Tenant,         // Office, Shop, Restaurant, Apartment
        Transportation, // Elevator, Stairs, Escalator
        Special         // Demolition mode
    }

    /// <summary>
    /// Handles building placement logic with grid snapping - The Tower II style
    /// </summary>
    public class BuildingPlacer : MonoBehaviour
    {
        public static BuildingPlacer Instance { get; private set; }

        [Header("Building Settings")]
        [SerializeField] private int officeWidthSegments = 9;
        [SerializeField] private int officeHeightFloors = 1;
        [SerializeField] private int elevatorWidthSegments = 1;
        [SerializeField] private int elevatorDefaultHeight = 2;

        [Header("Visual Settings")]
        [SerializeField] private Color validPlacementColor = new Color(0.3f, 0.8f, 0.3f, 0.5f);
        [SerializeField] private Color invalidPlacementColor = new Color(0.8f, 0.3f, 0.3f, 0.5f);
        [SerializeField] private Color demolitionColor = new Color(0.9f, 0.2f, 0.2f, 0.7f);

        [Header("State (Read Only)")]
        [SerializeField] private BuildingType selectedBuildingType = BuildingType.None;
        [SerializeField] private bool isInBuildMode;
        [SerializeField] private int currentFloor = 0;

        // Preview object
        private GameObject previewObject;
        private SpriteRenderer previewRenderer;

        // Demolition selection
        private GameObject selectedForDemolition;

        // Events
        public event System.Action<BuildingType> OnBuildingTypeChanged;
        public event System.Action<OfficeBuilding> OnBuildingPlaced;
        public event System.Action<Tenant> OnTenantPlaced;
        public event System.Action<GameObject> OnBuildingDemolished;

        // Properties
        public bool IsInBuildMode => isInBuildMode;
        public BuildingType SelectedBuildingType => selectedBuildingType;
        public int CurrentFloor => currentFloor;

        private Camera mainCamera;
        private GridManager gridManager;

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
            mainCamera = Camera.main;
            gridManager = GridManager.Instance;
            CreatePreviewObject();
        }

        private void Update()
        {
            if (!isInBuildMode || selectedBuildingType == BuildingType.None)
            {
                if (previewObject != null && previewObject.activeSelf)
                {
                    previewObject.SetActive(false);
                }
                return;
            }

            if (selectedBuildingType == BuildingType.Demolition)
            {
                UpdateDemolitionMode();
            }
            else
            {
                UpdatePreview();
            }

            HandleInput();
        }

        #region Preview

        private void CreatePreviewObject()
        {
            previewObject = new GameObject("BuildingPreview");
            previewObject.transform.SetParent(transform);

            previewRenderer = previewObject.AddComponent<SpriteRenderer>();
            previewRenderer.sprite = CreateBuildingSprite();
            previewRenderer.color = validPlacementColor;
            previewRenderer.sortingOrder = 100;

            UpdatePreviewSize();
            previewObject.SetActive(false);
        }

        private void UpdatePreviewSize()
        {
            if (gridManager != null && gridManager.Config != null)
            {
                Vector2 size = gridManager.Config.GetBuildingWorldSize(officeWidthSegments, officeHeightFloors);
                previewObject.transform.localScale = new Vector3(size.x, size.y, 1f);
            }
            else
            {
                previewObject.transform.localScale = new Vector3(9f, 3f, 1f);
            }
        }

        private void UpdatePreview()
        {
            if (previewObject == null) return;

            Mouse mouse = Mouse.current;
            if (mouse == null) return;

            Vector3 mouseScreenPos = mouse.position.ReadValue();
            mouseScreenPos.z = mainCamera.nearClipPlane;
            Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(mouseScreenPos);
            mouseWorldPos.z = 0;

            Vector3 snappedPos = GetSnappedPosition(mouseWorldPos);
            previewObject.transform.position = snappedPos;

            Vector2Int gridPos = gridManager != null ? gridManager.WorldToGrid(snappedPos) : Vector2Int.zero;
            currentFloor = gridPos.y;

            bool isValid = IsValidPlacement(gridPos.x, gridPos.y);
            previewRenderer.color = isValid ? validPlacementColor : invalidPlacementColor;

            if (!previewObject.activeSelf)
            {
                previewObject.SetActive(true);
            }
        }

        private void UpdateDemolitionMode()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null) return;

            Vector3 mouseScreenPos = mouse.position.ReadValue();
            mouseScreenPos.z = mainCamera.nearClipPlane;
            Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(mouseScreenPos);
            mouseWorldPos.z = 0;

            // Raycast to find buildings
            RaycastHit2D hit = Physics2D.Raycast(mouseWorldPos, Vector2.zero);

            if (selectedForDemolition != null)
            {
                // Reset previous selection color
                var prevSR = selectedForDemolition.GetComponent<SpriteRenderer>();
                if (prevSR != null)
                {
                    prevSR.color = Color.white; // Reset to default
                }
            }

            if (hit.collider != null)
            {
                // Check if it's a demolishable building
                Tenant tenant = hit.collider.GetComponent<Tenant>();
                if (tenant != null)
                {
                    selectedForDemolition = hit.collider.gameObject;
                    var sr = selectedForDemolition.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        sr.color = demolitionColor;
                    }
                }
            }
            else
            {
                selectedForDemolition = null;
            }

            // Hide preview in demolition mode
            if (previewObject != null && previewObject.activeSelf)
            {
                previewObject.SetActive(false);
            }
        }

        #endregion

        #region Input

        private void HandleInput()
        {
            Mouse mouse = Mouse.current;
            Keyboard keyboard = Keyboard.current;
            if (mouse == null) return;

            bool rightClick = mouse.rightButton.wasPressedThisFrame;
            bool escapeKey = keyboard != null && keyboard.escapeKey.wasPressedThisFrame;

            if (rightClick || escapeKey)
            {
                CancelBuildMode();
                return;
            }

            if (mouse.leftButton.wasPressedThisFrame)
            {
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                {
                    return;
                }

                if (selectedBuildingType == BuildingType.Demolition)
                {
                    TryDemolish();
                }
                else
                {
                    Vector3 mouseScreenPos = mouse.position.ReadValue();
                    mouseScreenPos.z = mainCamera.nearClipPlane;
                    Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(mouseScreenPos);
                    mouseWorldPos.z = 0;

                    Vector3 snappedPos = GetSnappedPosition(mouseWorldPos);
                    Vector2Int gridPos = gridManager != null ? gridManager.WorldToGrid(snappedPos) : Vector2Int.zero;

                    if (IsValidPlacement(gridPos.x, gridPos.y))
                    {
                        PlaceBuilding(snappedPos, gridPos.x, gridPos.y);
                    }
                    else
                    {
                        Debug.Log("[BuildingPlacer] Invalid placement position!");
                    }
                }
            }
        }

        #endregion

        #region Placement

        private Vector3 GetSnappedPosition(Vector3 worldPos)
        {
            if (gridManager != null)
            {
                return gridManager.SnapToGrid(worldPos, officeWidthSegments, officeHeightFloors);
            }

            float floorHeight = 3f;
            float groundLevel = -3f;

            int floor = Mathf.Max(0, Mathf.FloorToInt((worldPos.y - groundLevel) / floorHeight));
            float snappedX = Mathf.Round(worldPos.x);
            float snappedY = groundLevel + (floor * floorHeight) + (floorHeight / 2f);

            return new Vector3(snappedX, snappedY, 0);
        }

        private bool IsValidPlacement(int segmentX, int floor)
        {
            // Check if can afford
            if (EconomyManager.Instance != null)
            {
                long cost = EconomyManager.Instance.GetBuildingCost(selectedBuildingType);
                if (cost > 0 && !EconomyManager.Instance.CanAfford(cost))
                {
                    return false;
                }
            }

            if (FloorSystemManager.Instance != null)
            {
                PlacementResult result;

                switch (selectedBuildingType)
                {
                    case BuildingType.Lobby:
                        if (floor != 0) return false;
                        result = FloorSystemManager.Instance.ValidateLobbyPlacement(segmentX, officeWidthSegments);
                        return result.IsValid;

                    case BuildingType.Floor:
                        if (floor == 0) return false;
                        result = FloorSystemManager.Instance.ValidateFloorPlacement(segmentX, floor, officeWidthSegments);
                        return result.IsValid;

                    case BuildingType.Office:
                    case BuildingType.Restaurant:
                    case BuildingType.Shop:
                    case BuildingType.Apartment:
                        if (floor == 0) return false;
                        result = FloorSystemManager.Instance.ValidateTenantPlacement(segmentX, floor, officeWidthSegments);
                        return result.IsValid;

                    case BuildingType.Elevator:
                        if (floor != 0) return false;
                        result = FloorSystemManager.Instance.ValidateElevatorPlacement(segmentX);
                        return result.IsValid;

                    default:
                        return false;
                }
            }

            if (gridManager != null)
            {
                return gridManager.CanPlaceBuilding(segmentX, floor, officeWidthSegments, officeHeightFloors);
            }

            return floor >= 0;
        }

        private void PlaceBuilding(Vector3 position, int segmentX, int floor)
        {
            // Try to purchase
            if (EconomyManager.Instance != null)
            {
                long cost = EconomyManager.Instance.GetBuildingCost(selectedBuildingType);
                if (cost > 0 && !EconomyManager.Instance.TryPurchaseBuilding(selectedBuildingType))
                {
                    Debug.LogWarning("[BuildingPlacer] Cannot afford this building!");
                    return;
                }
            }

            switch (selectedBuildingType)
            {
                case BuildingType.Lobby:
                    PlaceLobby(position, segmentX);
                    break;

                case BuildingType.Floor:
                    PlaceFloorStructure(position, segmentX, floor);
                    break;

                case BuildingType.Office:
                    PlaceOffice(position, segmentX, floor);
                    break;

                case BuildingType.Restaurant:
                    PlaceRestaurant(position, segmentX, floor);
                    break;

                case BuildingType.Shop:
                    PlaceShop(position, segmentX, floor);
                    break;

                case BuildingType.Apartment:
                    PlaceApartment(position, segmentX, floor);
                    break;

                case BuildingType.Elevator:
                    PlaceElevator(position, segmentX);
                    break;
            }
        }

        private void PlaceLobby(Vector3 position, int segmentX)
        {
            if (FloorSystemManager.Instance == null) return;

            Lobby lobby = FloorSystemManager.Instance.PlaceLobby(position, segmentX, officeWidthSegments);
            if (lobby != null)
            {
                Debug.Log($"[BuildingPlacer] Lobby placed at segment {segmentX}");
            }
        }

        private void PlaceFloorStructure(Vector3 position, int segmentX, int floor)
        {
            if (FloorSystemManager.Instance == null) return;

            FloorStructure floorStruct = FloorSystemManager.Instance.PlaceFloor(position, segmentX, floor, officeWidthSegments);
            if (floorStruct != null)
            {
                Debug.Log($"[BuildingPlacer] Floor placed at segment {segmentX}, floor {floor + 1}F");
            }
        }

        private void PlaceOffice(Vector3 position, int segmentX, int floor)
        {
            var floorStruct = FloorSystemManager.Instance?.GetFloorAt(segmentX, floor);
            if (floorStruct == null)
            {
                Debug.LogWarning("[BuildingPlacer] No floor structure found for office placement");
                return;
            }

            GameObject building = CreateTenantBuilding<OfficeBuilding>(position, floor, "Office", new Color(0.6f, 0.6f, 0.7f));
            OfficeBuilding office = building.GetComponent<OfficeBuilding>();
            office.SetFloorInfo(floor, floorStruct.TowerId);
            office.SetBuildCost(EconomyManager.Instance?.GetBuildingCost(BuildingType.Office) ?? 0);

            int halfWidth = officeWidthSegments / 2;
            floorStruct.OccupySegments(segmentX - halfWidth, segmentX + halfWidth, building);

            if (gridManager != null)
            {
                gridManager.RegisterBuilding(building, segmentX, floor, officeWidthSegments, officeHeightFloors);
            }

            Debug.Log($"[BuildingPlacer] Office placed at segment {segmentX}, floor {floor + 1}F");
            OnBuildingPlaced?.Invoke(office);
            OnTenantPlaced?.Invoke(office);
        }

        private void PlaceRestaurant(Vector3 position, int segmentX, int floor)
        {
            var floorStruct = FloorSystemManager.Instance?.GetFloorAt(segmentX, floor);
            if (floorStruct == null) return;

            GameObject building = CreateTenantBuilding<Restaurant>(position, floor, "Restaurant", new Color(0.9f, 0.7f, 0.4f));
            Restaurant restaurant = building.GetComponent<Restaurant>();
            restaurant.SetFloorInfo(floor, floorStruct.TowerId);
            restaurant.SetBuildCost(EconomyManager.Instance?.GetTenantCost(TenantType.Restaurant) ?? 0);

            int halfWidth = officeWidthSegments / 2;
            floorStruct.OccupySegments(segmentX - halfWidth, segmentX + halfWidth, building);

            if (gridManager != null)
            {
                gridManager.RegisterBuilding(building, segmentX, floor, officeWidthSegments, officeHeightFloors);
            }

            Debug.Log($"[BuildingPlacer] Restaurant placed at segment {segmentX}, floor {floor + 1}F");
            OnTenantPlaced?.Invoke(restaurant);
        }

        private void PlaceShop(Vector3 position, int segmentX, int floor)
        {
            var floorStruct = FloorSystemManager.Instance?.GetFloorAt(segmentX, floor);
            if (floorStruct == null) return;

            GameObject building = CreateTenantBuilding<Shop>(position, floor, "Shop", new Color(0.5f, 0.7f, 0.9f));
            Shop shop = building.GetComponent<Shop>();
            shop.SetFloorInfo(floor, floorStruct.TowerId);
            shop.SetBuildCost(EconomyManager.Instance?.GetTenantCost(TenantType.Shop) ?? 0);

            int halfWidth = officeWidthSegments / 2;
            floorStruct.OccupySegments(segmentX - halfWidth, segmentX + halfWidth, building);

            if (gridManager != null)
            {
                gridManager.RegisterBuilding(building, segmentX, floor, officeWidthSegments, officeHeightFloors);
            }

            Debug.Log($"[BuildingPlacer] Shop placed at segment {segmentX}, floor {floor + 1}F");
            OnTenantPlaced?.Invoke(shop);
        }

        private void PlaceApartment(Vector3 position, int segmentX, int floor)
        {
            var floorStruct = FloorSystemManager.Instance?.GetFloorAt(segmentX, floor);
            if (floorStruct == null) return;

            GameObject building = CreateTenantBuilding<Apartment>(position, floor, "Apartment", new Color(0.7f, 0.6f, 0.5f));
            Apartment apartment = building.GetComponent<Apartment>();
            apartment.SetFloorInfo(floor, floorStruct.TowerId);
            apartment.SetBuildCost(EconomyManager.Instance?.GetTenantCost(TenantType.Apartment) ?? 0);

            int halfWidth = officeWidthSegments / 2;
            floorStruct.OccupySegments(segmentX - halfWidth, segmentX + halfWidth, building);

            if (gridManager != null)
            {
                gridManager.RegisterBuilding(building, segmentX, floor, officeWidthSegments, officeHeightFloors);
            }

            Debug.Log($"[BuildingPlacer] Apartment placed at segment {segmentX}, floor {floor + 1}F");
            OnTenantPlaced?.Invoke(apartment);
        }

        private void PlaceElevator(Vector3 position, int segmentX)
        {
            if (ElevatorManager.Instance == null)
            {
                Debug.LogWarning("[BuildingPlacer] ElevatorManager not found");
                return;
            }

            var tower = FloorSystemManager.Instance?.GetTowerAtPosition(segmentX);
            if (tower == null)
            {
                Debug.LogWarning("[BuildingPlacer] No tower found at this position");
                return;
            }

            int topFloor = 1;
            foreach (var floor in tower.floors)
            {
                if (floor.FloorNumber > topFloor)
                {
                    topFloor = floor.FloorNumber;
                }
            }

            ElevatorShaft shaft = ElevatorManager.Instance.CreateShaft(segmentX, 0, topFloor, tower.towerId);
            if (shaft != null)
            {
                if (tower.lobby != null)
                {
                    tower.lobby.RegisterTransportation(shaft.transform);
                }
                Debug.Log($"[BuildingPlacer] Elevator placed at segment {segmentX}, floors 1F to {topFloor + 1}F");
            }
        }

        private GameObject CreateTenantBuilding<T>(Vector3 position, int floor, string typeName, Color color) where T : Tenant
        {
            int displayFloor = floor + 1;
            GameObject building = new GameObject($"{typeName}_{displayFloor}F_{System.DateTime.Now.Ticks % 10000}");

            Vector2 size;
            if (gridManager != null && gridManager.Config != null)
            {
                size = gridManager.Config.GetBuildingWorldSize(officeWidthSegments, officeHeightFloors);
            }
            else
            {
                size = new Vector2(9f, 3f);
            }

            SpriteRenderer sr = building.AddComponent<SpriteRenderer>();
            sr.sprite = CreateBuildingSprite();
            sr.color = color;
            sr.sortingOrder = 1;

            building.transform.position = position;
            building.transform.localScale = new Vector3(size.x, size.y, 1f);

            // Add collider for demolition
            BoxCollider2D collider = building.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;

            building.AddComponent<T>();
            CreateBuildingWindows(building.transform, size);

            return building;
        }

        private void CreateBuildingWindows(Transform building, Vector2 size)
        {
            int windowCount = 6;
            float spacing = 1f / (windowCount + 1);

            for (int i = 0; i < windowCount; i++)
            {
                GameObject window = new GameObject($"Window_{i}");
                window.transform.SetParent(building);

                float xPos = -0.5f + spacing + (i * spacing);
                window.transform.localPosition = new Vector3(xPos, 0.1f, 0);
                window.transform.localScale = new Vector3(0.08f, 0.5f, 1);

                SpriteRenderer sr = window.AddComponent<SpriteRenderer>();
                sr.sprite = CreateBuildingSprite();
                sr.color = new Color(0.8f, 0.9f, 1f);
                sr.sortingOrder = 2;
            }
        }

        private Sprite CreateBuildingSprite()
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

        #region Demolition

        private void TryDemolish()
        {
            if (selectedForDemolition == null) return;

            Tenant tenant = selectedForDemolition.GetComponent<Tenant>();
            if (tenant == null) return;

            // Calculate refund
            long refund = 0;
            if (EconomyManager.Instance != null)
            {
                refund = EconomyManager.Instance.GetDemolitionRefund(tenant.BuildCost);
            }

            // Unregister from grid
            if (gridManager != null)
            {
                gridManager.UnregisterBuilding(selectedForDemolition);
            }

            // Unregister from floor structure
            var floorStruct = FloorSystemManager.Instance?.GetFloorAt(
                gridManager?.WorldToGrid(selectedForDemolition.transform.position).x ?? 0,
                tenant.Floor
            );
            if (floorStruct != null)
            {
                int centerX = gridManager?.WorldToGrid(selectedForDemolition.transform.position).x ?? 0;
                int halfWidth = officeWidthSegments / 2;
                floorStruct.ReleaseSegments(centerX - halfWidth, centerX + halfWidth);
            }

            // Give refund
            if (refund > 0 && EconomyManager.Instance != null)
            {
                EconomyManager.Instance.Earn(refund, $"{tenant.TenantType} 解体払い戻し");
            }

            Debug.Log($"[BuildingPlacer] Demolished {tenant.TenantType}. Refund: ¥{refund:N0}");
            OnBuildingDemolished?.Invoke(selectedForDemolition);

            Destroy(selectedForDemolition);
            selectedForDemolition = null;
        }

        #endregion

        #region Build Mode

        public void EnterBuildMode(BuildingType type)
        {
            selectedBuildingType = type;
            isInBuildMode = true;

            UpdatePreviewForBuildingType(type);

            OnBuildingTypeChanged?.Invoke(type);
            Debug.Log($"[BuildingPlacer] Entered build mode: {type}");
        }

        private void UpdatePreviewForBuildingType(BuildingType type)
        {
            if (previewObject == null || gridManager == null || gridManager.Config == null) return;

            Vector2 size;
            Color color = validPlacementColor;

            switch (type)
            {
                case BuildingType.Elevator:
                    float elevatorWidth = 1.0f;
                    float elevatorHeight = elevatorDefaultHeight * gridManager.Config.floorHeight;
                    size = new Vector2(elevatorWidth, elevatorHeight);
                    break;

                case BuildingType.Demolition:
                    size = Vector2.one;
                    color = demolitionColor;
                    break;

                default:
                    size = gridManager.Config.GetBuildingWorldSize(officeWidthSegments, officeHeightFloors);
                    break;
            }

            previewObject.transform.localScale = new Vector3(size.x, size.y, 1f);
            previewRenderer.color = color;
        }

        public void CancelBuildMode()
        {
            selectedBuildingType = BuildingType.None;
            isInBuildMode = false;

            if (previewObject != null)
            {
                previewObject.SetActive(false);
            }

            // Reset demolition selection
            if (selectedForDemolition != null)
            {
                var sr = selectedForDemolition.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.color = Color.white;
                }
                selectedForDemolition = null;
            }

            OnBuildingTypeChanged?.Invoke(BuildingType.None);
            Debug.Log("[BuildingPlacer] Exited build mode");
        }

        // Toggle methods for each building type
        public void ToggleOfficeBuildMode() => ToggleBuildMode(BuildingType.Office);
        public void ToggleLobbyBuildMode() => ToggleBuildMode(BuildingType.Lobby);
        public void ToggleFloorBuildMode() => ToggleBuildMode(BuildingType.Floor);
        public void ToggleElevatorBuildMode() => ToggleBuildMode(BuildingType.Elevator);
        public void ToggleRestaurantBuildMode() => ToggleBuildMode(BuildingType.Restaurant);
        public void ToggleShopBuildMode() => ToggleBuildMode(BuildingType.Shop);
        public void ToggleApartmentBuildMode() => ToggleBuildMode(BuildingType.Apartment);
        public void ToggleDemolitionMode() => ToggleBuildMode(BuildingType.Demolition);

        private void ToggleBuildMode(BuildingType type)
        {
            if (isInBuildMode && selectedBuildingType == type)
            {
                CancelBuildMode();
            }
            else
            {
                EnterBuildMode(type);
            }
        }

        public BuildingCategory GetSelectedCategory()
        {
            return FloorSystemManager.GetCategory(selectedBuildingType);
        }

        #endregion
    }
}
