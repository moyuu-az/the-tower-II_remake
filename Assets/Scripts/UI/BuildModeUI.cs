using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TowerGame.Building;
using TowerGame.Economy;

namespace TowerGame.UI
{
    /// <summary>
    /// UI for building placement mode (The Tower II style)
    /// Supports all building types including new tenants and demolition
    /// </summary>
    public class BuildModeUI : MonoBehaviour
    {
        [Header("UI References - Foundation Buttons")]
        [SerializeField] private Button lobbyButton;
        [SerializeField] private Image lobbyButtonImage;

        [Header("UI References - Structure Buttons")]
        [SerializeField] private Button floorButton;
        [SerializeField] private Image floorButtonImage;

        [Header("UI References - Tenant Buttons")]
        [SerializeField] private Button officeButton;
        [SerializeField] private Image officeButtonImage;
        [SerializeField] private Button restaurantButton;
        [SerializeField] private Image restaurantButtonImage;
        [SerializeField] private Button shopButton;
        [SerializeField] private Image shopButtonImage;
        [SerializeField] private Button apartmentButton;
        [SerializeField] private Image apartmentButtonImage;

        [Header("UI References - Transportation Buttons")]
        [SerializeField] private Button elevatorButton;
        [SerializeField] private Image elevatorButtonImage;

        [Header("UI References - Special Buttons")]
        [SerializeField] private Button demolitionButton;
        [SerializeField] private Image demolitionButtonImage;

        [Header("Visual Settings")]
        [SerializeField] private Color normalColor = new Color(0.8f, 0.8f, 0.8f);
        [SerializeField] private Color selectedColor = new Color(0.4f, 0.7f, 1f);
        [SerializeField] private Color demolitionSelectedColor = new Color(0.9f, 0.3f, 0.3f);

        [Header("Status Display")]
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private Text legacyStatusText;

        private BuildingPlacer buildingPlacer;

        private void Start()
        {
            Debug.Log("[BuildModeUI] Start() - Initializing...");

            buildingPlacer = BuildingPlacer.Instance;
            if (buildingPlacer == null)
            {
                buildingPlacer = FindFirstObjectByType<BuildingPlacer>();
            }

            if (buildingPlacer != null)
            {
                buildingPlacer.OnBuildingTypeChanged += OnBuildingTypeChanged;
                buildingPlacer.OnBuildingPlaced += OnBuildingPlaced;
                buildingPlacer.OnTenantPlaced += OnTenantPlaced;
                Debug.Log("[BuildModeUI] BuildingPlacer found and events subscribed");
            }
            else
            {
                Debug.LogError("[BuildModeUI] BuildingPlacer not found!");
            }

            // Auto-find buttons
            FindAndSetupButton("LobbyButton", ref lobbyButton, ref lobbyButtonImage);
            FindAndSetupButton("FloorButton", ref floorButton, ref floorButtonImage);
            FindAndSetupButton("OfficeButton", ref officeButton, ref officeButtonImage);
            FindAndSetupButton("RestaurantButton", ref restaurantButton, ref restaurantButtonImage);
            FindAndSetupButton("ShopButton", ref shopButton, ref shopButtonImage);
            FindAndSetupButton("ApartmentButton", ref apartmentButton, ref apartmentButtonImage);
            FindAndSetupButton("ElevatorButton", ref elevatorButton, ref elevatorButtonImage);
            FindAndSetupButton("DemolitionButton", ref demolitionButton, ref demolitionButtonImage);

            // Auto-find status text
            if (statusText == null && legacyStatusText == null)
            {
                Canvas canvas = GetComponentInParent<Canvas>();
                if (canvas != null)
                {
                    Transform statusTransform = canvas.transform.Find("StatusText");
                    if (statusTransform != null)
                    {
                        statusText = statusTransform.GetComponent<TextMeshProUGUI>();
                        if (statusText == null)
                        {
                            legacyStatusText = statusTransform.GetComponent<Text>();
                        }
                    }
                }
            }

            // Setup button clicks
            SetupButtonClick(lobbyButton, () => buildingPlacer?.ToggleLobbyBuildMode(), "Lobby");
            SetupButtonClick(floorButton, () => buildingPlacer?.ToggleFloorBuildMode(), "Floor");
            SetupButtonClick(officeButton, () => buildingPlacer?.ToggleOfficeBuildMode(), "Office");
            SetupButtonClick(restaurantButton, () => buildingPlacer?.ToggleRestaurantBuildMode(), "Restaurant");
            SetupButtonClick(shopButton, () => buildingPlacer?.ToggleShopBuildMode(), "Shop");
            SetupButtonClick(apartmentButton, () => buildingPlacer?.ToggleApartmentBuildMode(), "Apartment");
            SetupButtonClick(elevatorButton, () => buildingPlacer?.ToggleElevatorBuildMode(), "Elevator");
            SetupButtonClick(demolitionButton, () => buildingPlacer?.ToggleDemolitionMode(), "Demolition");

            UpdateButtonVisual(BuildingType.None);
            UpdateStatus("");

            Debug.Log("[BuildModeUI] Initialization complete");
        }

        private void FindAndSetupButton(string buttonName, ref Button button, ref Image buttonImage)
        {
            if (button != null) return;

            Transform buttonTransform = transform.Find(buttonName);
            if (buttonTransform != null)
            {
                button = buttonTransform.GetComponent<Button>();
                buttonImage = buttonTransform.GetComponent<Image>();
                Debug.Log($"[BuildModeUI] {buttonName} found");
            }
        }

        private void SetupButtonClick(Button button, System.Action action, string name)
        {
            if (button != null && action != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => {
                    Debug.Log($"[BuildModeUI] {name} button clicked!");
                    action();
                });
            }
        }

        private static T FindFirstObjectByType<T>() where T : Object
        {
            #if UNITY_2023_1_OR_NEWER
            return Object.FindFirstObjectByType<T>();
            #else
            return Object.FindObjectOfType<T>();
            #endif
        }

        private void OnDestroy()
        {
            if (buildingPlacer != null)
            {
                buildingPlacer.OnBuildingTypeChanged -= OnBuildingTypeChanged;
                buildingPlacer.OnBuildingPlaced -= OnBuildingPlaced;
                buildingPlacer.OnTenantPlaced -= OnTenantPlaced;
            }
        }

        private void Update()
        {
            if (buildingPlacer != null && buildingPlacer.IsInBuildMode)
            {
                int displayFloor = buildingPlacer.CurrentFloor + 1;
                string typeName = GetBuildingTypeName(buildingPlacer.SelectedBuildingType);
                string costStr = GetCostString(buildingPlacer.SelectedBuildingType);

                if (buildingPlacer.SelectedBuildingType == BuildingType.Demolition)
                {
                    UpdateStatus("Click on a building to demolish it. Right-click or ESC to cancel.");
                }
                else
                {
                    UpdateStatus($"[{displayFloor}F] {typeName} {costStr}. Click to place. Right-click/ESC to cancel.");
                }
            }
        }

        private void OnBuildingTypeChanged(BuildingType type)
        {
            UpdateButtonVisual(type);

            if (type != BuildingType.None)
            {
                int displayFloor = buildingPlacer != null ? buildingPlacer.CurrentFloor + 1 : 1;
                string typeName = GetBuildingTypeName(type);
                string hint = GetBuildingHint(type);
                string costStr = GetCostString(type);
                UpdateStatus($"[{displayFloor}F] {typeName} {costStr}. {hint}");
            }
            else
            {
                UpdateStatus("");
            }
        }

        private string GetBuildingTypeName(BuildingType type)
        {
            switch (type)
            {
                case BuildingType.Lobby: return "Lobby";
                case BuildingType.Floor: return "Floor";
                case BuildingType.Office: return "Office";
                case BuildingType.Restaurant: return "Restaurant";
                case BuildingType.Shop: return "Shop";
                case BuildingType.Apartment: return "Apartment";
                case BuildingType.Elevator: return "Elevator";
                case BuildingType.Demolition: return "Demolition";
                default: return "";
            }
        }

        private string GetBuildingHint(BuildingType type)
        {
            switch (type)
            {
                case BuildingType.Lobby: return "(1F only, required first)";
                case BuildingType.Floor: return "(2F+, needs support)";
                case BuildingType.Office: return "(needs Floor, employees work)";
                case BuildingType.Restaurant: return "(needs Floor, lunch/dinner bonus)";
                case BuildingType.Shop: return "(needs Floor, weekend bonus)";
                case BuildingType.Apartment: return "(needs Floor, residents live)";
                case BuildingType.Elevator: return "(in lobby, for 2F+ access)";
                case BuildingType.Demolition: return "(click building to remove)";
                default: return "";
            }
        }

        private string GetCostString(BuildingType type)
        {
            if (type == BuildingType.Demolition) return "";

            if (EconomyManager.Instance != null)
            {
                long cost = EconomyManager.Instance.GetBuildingCost(type);
                if (cost > 0)
                {
                    return $"(¥{cost:N0})";
                }
            }
            return "";
        }

        private void OnBuildingPlaced(OfficeBuilding office)
        {
            UpdateStatus($"Office placed on {office.DisplayFloor}F! Click to place another.");
        }

        private void OnTenantPlaced(Tenant tenant)
        {
            UpdateStatus($"{tenant.TenantType} placed on {tenant.DisplayFloor}F! Click to place another.");
        }

        private void UpdateButtonVisual(BuildingType selectedType)
        {
            SetButtonColor(lobbyButtonImage, selectedType == BuildingType.Lobby);
            SetButtonColor(floorButtonImage, selectedType == BuildingType.Floor);
            SetButtonColor(officeButtonImage, selectedType == BuildingType.Office);
            SetButtonColor(restaurantButtonImage, selectedType == BuildingType.Restaurant);
            SetButtonColor(shopButtonImage, selectedType == BuildingType.Shop);
            SetButtonColor(apartmentButtonImage, selectedType == BuildingType.Apartment);
            SetButtonColor(elevatorButtonImage, selectedType == BuildingType.Elevator);

            // Demolition uses different color
            if (demolitionButtonImage != null)
            {
                demolitionButtonImage.color = selectedType == BuildingType.Demolition ?
                    demolitionSelectedColor : normalColor;
            }
        }

        private void SetButtonColor(Image buttonImage, bool isSelected)
        {
            if (buttonImage != null)
            {
                buttonImage.color = isSelected ? selectedColor : normalColor;
            }
        }

        private void UpdateStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
            else if (legacyStatusText != null)
            {
                legacyStatusText.text = message;
            }
        }
    }
}
