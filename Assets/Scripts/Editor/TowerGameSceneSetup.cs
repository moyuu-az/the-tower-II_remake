using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;
using TowerGame.Core;
using TowerGame.Building;
using TowerGame.Grid;
using TowerGame.People;
using TowerGame.UI;

namespace TowerGame.Editor
{
    /// <summary>
    /// Editor utility to set up the Tower Game scene automatically
    /// </summary>
    public class TowerGameSceneSetup : EditorWindow
    {
        [MenuItem("Tower Game/Setup Scene")]
        public static void SetupScene()
        {
            if (EditorUtility.DisplayDialog("Setup Tower Game Scene",
                "This will create all necessary GameObjects for the Tower Game prototype.\n\n" +
                "- Ground\n- Office Building\n- Game Manager\n- Time System\n- Employee Spawner\n- Build Mode UI\n\n" +
                "Continue?", "Setup", "Cancel"))
            {
                CreateScene();
            }
        }

        [MenuItem("Tower Game/Quick Setup (No Dialog)")]
        public static void QuickSetup()
        {
            CreateScene();
        }

        [MenuItem("Tower Game/Add Build Mode UI")]
        public static void AddBuildModeUI()
        {
            AddBuildUI();
            Debug.Log("[TowerGameSetup] Build Mode UI added!");
        }

        private static void CreateScene()
        {
            Debug.Log("[TowerGameSetup] Starting scene setup...");

            // Setup camera
            SetupCamera();

            // Create grid system
            CreateGridManager();

            // Create floor system manager (The Tower II style)
            CreateFloorSystemManager();

            // Create elevator manager
            CreateElevatorManager();

            // Create environment
            CreateGround();

            // Clean up any existing office buildings from previous setups
            OfficeBuilding[] existingBuildings = FindObjectsOfType<OfficeBuilding>();
            foreach (var building in existingBuildings)
            {
                DestroyImmediate(building.gameObject);
                Debug.Log("[TowerGameSetup] Removed existing OfficeBuilding");
            }
            // NOTE: No initial building - players must place Lobby first, then floors, then offices

            // Create game systems
            CreateGameManager();

            // Create UI (including build mode)
            CreateUI();

            Debug.Log("[TowerGameSetup] Scene setup complete!");
            Debug.Log("[TowerGameSetup] Press Play to start the simulation!");
            Debug.Log("[TowerGameSetup] Controls: Space=Pause, 1/2/4=Speed multiplier");
            Debug.Log("[TowerGameSetup] Build Order: 1. Lobby (1F) -> 2. Floor (2F+) -> 3. Office -> 4. Elevator");
            Debug.Log("[TowerGameSetup] Grid: 9 segments per office, 3 units per floor");
            Debug.Log("[TowerGameSetup] Elevator: Required for 2F+ access, place in lobby area");
        }

        private static void CreateGridManager()
        {
            // Check if GridManager already exists
            GridManager existingManager = FindFirstObjectByType<GridManager>();
            if (existingManager != null)
            {
                DestroyImmediate(existingManager.gameObject);
            }

            GameObject gridGO = new GameObject("GridManager");
            GridManager gridManager = gridGO.AddComponent<GridManager>();

            Debug.Log("[TowerGameSetup] GridManager created");
        }

        private static void CreateFloorSystemManager()
        {
            // Check if FloorSystemManager already exists
            FloorSystemManager existingManager = FindFirstObjectByType<FloorSystemManager>();
            if (existingManager != null)
            {
                DestroyImmediate(existingManager.gameObject);
            }

            GameObject floorSystemGO = new GameObject("FloorSystemManager");
            FloorSystemManager floorSystemManager = floorSystemGO.AddComponent<FloorSystemManager>();

            Debug.Log("[TowerGameSetup] FloorSystemManager created (The Tower II style)");
        }

        private static void CreateElevatorManager()
        {
            // Check if ElevatorManager already exists
            ElevatorManager existingManager = FindFirstObjectByType<ElevatorManager>();
            if (existingManager != null)
            {
                DestroyImmediate(existingManager.gameObject);
            }

            GameObject elevatorManagerGO = new GameObject("ElevatorManager");
            ElevatorManager elevatorManager = elevatorManagerGO.AddComponent<ElevatorManager>();

            Debug.Log("[TowerGameSetup] ElevatorManager created");
        }

        private static void SetupCamera()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                GameObject camGO = new GameObject("Main Camera");
                mainCamera = camGO.AddComponent<Camera>();
                camGO.AddComponent<AudioListener>();
                camGO.tag = "MainCamera";
            }

            mainCamera.orthographic = true;
            mainCamera.orthographicSize = 10f; // Wider view for larger ground
            mainCamera.transform.position = new Vector3(0, 2, -10); // Raised slightly
            mainCamera.backgroundColor = new Color(0.5f, 0.7f, 0.9f); // Sky blue

            Debug.Log("[TowerGameSetup] Camera configured (wider view)");
        }

        private static void CreateGround()
        {
            // Check if ground already exists
            GameObject existingGround = GameObject.Find("Ground");
            if (existingGround != null)
            {
                DestroyImmediate(existingGround);
            }

            GameObject ground = new GameObject("Ground");
            // Note: Ground tag not used - identification by name instead
            SpriteRenderer sr = ground.AddComponent<SpriteRenderer>();

            // Create sprite
            sr.sprite = CreateSquareSprite();
            sr.color = new Color(0.4f, 0.3f, 0.2f); // Brown
            sr.sortingOrder = 0;

            // Ground is 40 units wide to allow multiple buildings
            ground.transform.position = new Vector3(0, -3.5f, 0);
            ground.transform.localScale = new Vector3(40, 1, 1);

            // Add collider
            BoxCollider2D collider = ground.AddComponent<BoxCollider2D>();

            Debug.Log("[TowerGameSetup] Ground created (40 units wide)");
        }

        private static void CreateBuilding()
        {
            // Check if building already exists
            GameObject existingBuilding = GameObject.Find("OfficeBuilding");
            if (existingBuilding != null)
            {
                DestroyImmediate(existingBuilding);
            }

            GameObject building = new GameObject("OfficeBuilding");
            SpriteRenderer sr = building.AddComponent<SpriteRenderer>();

            // Create sprite
            sr.sprite = CreateSquareSprite();
            sr.color = new Color(0.6f, 0.6f, 0.7f); // Gray-blue
            sr.sortingOrder = 1;

            // Position on the left side to leave room for new buildings
            building.transform.position = new Vector3(-12, 0, 0);
            building.transform.localScale = new Vector3(8, 5, 1);

            // Add building component
            OfficeBuilding office = building.AddComponent<OfficeBuilding>();

            // Create entrance point
            GameObject entrance = new GameObject("EntrancePoint");
            entrance.transform.SetParent(building.transform);
            entrance.transform.localPosition = new Vector3(0, -2.5f, 0);

            // Create windows decoration
            CreateBuildingWindows(building.transform);

            Debug.Log("[TowerGameSetup] Office Building created");
        }

        private static void CreateBuildingWindows(Transform building)
        {
            // Create simple window decorations
            float[] xPositions = { -2.5f, -0.8f, 0.8f, 2.5f };
            float[] yPositions = { 0.5f, 1.5f };

            foreach (float y in yPositions)
            {
                foreach (float x in xPositions)
                {
                    GameObject window = new GameObject("Window");
                    window.transform.SetParent(building);
                    window.transform.localPosition = new Vector3(x / 8f, y / 5f, 0);
                    window.transform.localScale = new Vector3(0.08f, 0.12f, 1);

                    SpriteRenderer sr = window.AddComponent<SpriteRenderer>();
                    sr.sprite = CreateSquareSprite();
                    sr.color = new Color(0.8f, 0.9f, 1f); // Light blue (window)
                    sr.sortingOrder = 2;
                }
            }
        }

        private static void CreateGameManager()
        {
            // Check if GameManager already exists
            GameObject existingGM = GameObject.Find("GameManager");
            if (existingGM != null)
            {
                DestroyImmediate(existingGM);
            }

            GameObject gmGO = new GameObject("GameManager");

            // Add GameManager
            GameManager gm = gmGO.AddComponent<GameManager>();

            // Add GameTimeManager as child
            GameObject timeGO = new GameObject("TimeManager");
            timeGO.transform.SetParent(gmGO.transform);
            GameTimeManager tm = timeGO.AddComponent<GameTimeManager>();

            // Add PersonSpawner as child
            GameObject spawnerGO = new GameObject("PersonSpawner");
            spawnerGO.transform.SetParent(gmGO.transform);
            PersonSpawner spawner = spawnerGO.AddComponent<PersonSpawner>();

            // Add BuildingPlacer as child
            GameObject placerGO = new GameObject("BuildingPlacer");
            placerGO.transform.SetParent(gmGO.transform);
            BuildingPlacer placer = placerGO.AddComponent<BuildingPlacer>();

            Debug.Log("[TowerGameSetup] GameManager created with TimeManager, PersonSpawner, and BuildingPlacer");
        }

        private static void CreateUI()
        {
            // Check if Canvas already exists
            Canvas existingCanvas = FindFirstObjectByType<Canvas>();
            if (existingCanvas != null && existingCanvas.gameObject.name == "GameUI")
            {
                DestroyImmediate(existingCanvas.gameObject);
            }

            // Ensure EventSystem exists (required for UI interaction)
            EventSystem existingEventSystem = FindFirstObjectByType<EventSystem>();
            if (existingEventSystem == null)
            {
                GameObject eventSystemGO = new GameObject("EventSystem");
                eventSystemGO.AddComponent<EventSystem>();
                eventSystemGO.AddComponent<InputSystemUIInputModule>(); // Use new Input System
                Debug.Log("[TowerGameSetup] EventSystem created with InputSystemUIInputModule");
            }
            else
            {
                // Check if it has the correct input module
                if (existingEventSystem.GetComponent<InputSystemUIInputModule>() == null)
                {
                    // Remove old input module if exists
                    StandaloneInputModule oldModule = existingEventSystem.GetComponent<StandaloneInputModule>();
                    if (oldModule != null)
                    {
                        DestroyImmediate(oldModule);
                    }
                    existingEventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
                    Debug.Log("[TowerGameSetup] Updated EventSystem to use InputSystemUIInputModule");
                }
            }

            // Create Canvas
            GameObject canvasGO = new GameObject("GameUI");
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGO.AddComponent<GraphicRaycaster>();

            // Create Time Display (top center)
            CreateTimeDisplay(canvasGO.transform);

            // Create Build Panel (bottom left)
            CreateBuildPanel(canvasGO.transform);

            // Create Status Text (bottom center)
            CreateStatusText(canvasGO.transform);

            Debug.Log("[TowerGameSetup] UI created with Build Mode panel");
        }

        private static void CreateTimeDisplay(Transform parent)
        {
            GameObject timeDisplayGO = new GameObject("TimeDisplay");
            timeDisplayGO.transform.SetParent(parent);

            RectTransform rt = timeDisplayGO.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0, -20);
            rt.sizeDelta = new Vector2(300, 50);

            if (IsTMPAvailable())
            {
                TextMeshProUGUI tmp = timeDisplayGO.AddComponent<TextMeshProUGUI>();
                tmp.text = "Day 1 - 06:00";
                tmp.fontSize = 36;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = Color.white;
                tmp.outlineWidth = 0.2f;
                tmp.outlineColor = Color.black;
            }
            else
            {
                Text text = timeDisplayGO.AddComponent<Text>();
                text.text = "Day 1 - 06:00";
                text.fontSize = 24;
                text.alignment = TextAnchor.MiddleCenter;
                text.color = Color.white;
            }

            TimeDisplayUI timeUI = timeDisplayGO.AddComponent<TimeDisplayUI>();
        }

        private static void CreateBuildPanel(Transform parent)
        {
            // Create Build Panel container
            GameObject buildPanelGO = new GameObject("BuildPanel");
            buildPanelGO.transform.SetParent(parent);

            RectTransform panelRT = buildPanelGO.AddComponent<RectTransform>();
            panelRT.anchorMin = new Vector2(0, 0);
            panelRT.anchorMax = new Vector2(0, 0);
            panelRT.pivot = new Vector2(0, 0);
            panelRT.anchoredPosition = new Vector2(20, 20);
            panelRT.sizeDelta = new Vector2(200, 280); // Height for 4 buttons

            // Add background
            Image panelBG = buildPanelGO.AddComponent<Image>();
            panelBG.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            // Add vertical layout
            VerticalLayoutGroup layout = buildPanelGO.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.spacing = 10;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = false;

            // Create "Build" label
            GameObject labelGO = new GameObject("BuildLabel");
            labelGO.transform.SetParent(buildPanelGO.transform);
            RectTransform labelRT = labelGO.AddComponent<RectTransform>();
            labelRT.sizeDelta = new Vector2(180, 30);

            if (IsTMPAvailable())
            {
                TextMeshProUGUI labelTMP = labelGO.AddComponent<TextMeshProUGUI>();
                labelTMP.text = "Build";
                labelTMP.fontSize = 24;
                labelTMP.fontStyle = FontStyles.Bold;
                labelTMP.color = Color.white;
            }
            else
            {
                Text labelText = labelGO.AddComponent<Text>();
                labelText.text = "Build";
                labelText.fontSize = 18;
                labelText.fontStyle = FontStyle.Bold;
                labelText.color = Color.white;
            }

            // Create building buttons (The Tower II style: Lobby -> Floor -> Office -> Elevator)
            GameObject lobbyButtonGO = CreateBuildButton(buildPanelGO.transform, "LobbyButton", "1. Lobby");
            GameObject floorButtonGO = CreateBuildButton(buildPanelGO.transform, "FloorButton", "2. Floor");
            GameObject officeButtonGO = CreateBuildButton(buildPanelGO.transform, "OfficeButton", "3. Office");
            GameObject elevatorButtonGO = CreateBuildButton(buildPanelGO.transform, "ElevatorButton", "4. Elevator");

            // Add BuildModeUI component
            BuildModeUI buildModeUI = buildPanelGO.AddComponent<BuildModeUI>();
        }

        private static GameObject CreateBuildButton(Transform parent, string name, string label)
        {
            GameObject buttonGO = new GameObject(name);
            buttonGO.transform.SetParent(parent);

            RectTransform buttonRT = buttonGO.AddComponent<RectTransform>();
            buttonRT.sizeDelta = new Vector2(180, 40);

            Image buttonImage = buttonGO.AddComponent<Image>();
            buttonImage.color = new Color(0.8f, 0.8f, 0.8f);

            Button button = buttonGO.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.8f, 0.8f, 0.8f);
            colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f);
            colors.pressedColor = new Color(0.6f, 0.6f, 0.6f);
            colors.selectedColor = new Color(0.4f, 0.7f, 1f);
            button.colors = colors;

            // Create button text
            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(buttonGO.transform);

            RectTransform textRT = textGO.AddComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.sizeDelta = Vector2.zero;
            textRT.anchoredPosition = Vector2.zero;

            if (IsTMPAvailable())
            {
                TextMeshProUGUI buttonTMP = textGO.AddComponent<TextMeshProUGUI>();
                buttonTMP.text = label;
                buttonTMP.fontSize = 20;
                buttonTMP.alignment = TextAlignmentOptions.Center;
                buttonTMP.color = Color.black;
            }
            else
            {
                Text buttonText = textGO.AddComponent<Text>();
                buttonText.text = label;
                buttonText.fontSize = 16;
                buttonText.alignment = TextAnchor.MiddleCenter;
                buttonText.color = Color.black;
            }

            return buttonGO;
        }

        private static void CreateStatusText(Transform parent)
        {
            GameObject statusGO = new GameObject("StatusText");
            statusGO.transform.SetParent(parent);

            RectTransform rt = statusGO.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0);
            rt.anchorMax = new Vector2(0.5f, 0);
            rt.pivot = new Vector2(0.5f, 0);
            rt.anchoredPosition = new Vector2(0, 20);
            rt.sizeDelta = new Vector2(500, 40);

            if (IsTMPAvailable())
            {
                TextMeshProUGUI tmp = statusGO.AddComponent<TextMeshProUGUI>();
                tmp.text = "";
                tmp.fontSize = 20;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = Color.yellow;
            }
            else
            {
                Text text = statusGO.AddComponent<Text>();
                text.text = "";
                text.fontSize = 16;
                text.alignment = TextAnchor.MiddleCenter;
                text.color = Color.yellow;
            }
        }

        private static void AddBuildUI()
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[TowerGameSetup] No Canvas found. Run Setup Scene first.");
                return;
            }

            // Check if BuildPanel already exists
            Transform existingPanel = canvas.transform.Find("BuildPanel");
            if (existingPanel != null)
            {
                DestroyImmediate(existingPanel.gameObject);
            }

            CreateBuildPanel(canvas.transform);

            // Add BuildingPlacer if not exists
            if (FindFirstObjectByType<BuildingPlacer>() == null)
            {
                GameObject gmGO = GameObject.Find("GameManager");
                if (gmGO != null)
                {
                    GameObject placerGO = new GameObject("BuildingPlacer");
                    placerGO.transform.SetParent(gmGO.transform);
                    placerGO.AddComponent<BuildingPlacer>();
                }
            }
        }

        private static bool IsTMPAvailable()
        {
            return System.Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro") != null;
        }

        private static Sprite CreateSquareSprite()
        {
            string spritePath = "Assets/Sprites/WhiteSquare.png";
            Sprite existingSprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (existingSprite != null)
            {
                return existingSprite;
            }

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

        private static T FindFirstObjectByType<T>() where T : Object
        {
            #if UNITY_2023_1_OR_NEWER
            return Object.FindFirstObjectByType<T>();
            #else
            return Object.FindObjectOfType<T>();
            #endif
        }

        [MenuItem("Tower Game/Debug/Skip to 8:00 (Work Start)")]
        public static void SkipTo8AM()
        {
            if (Application.isPlaying && GameTimeManager.Instance != null)
            {
                GameTimeManager.Instance.SkipToHour(8);
            }
            else
            {
                Debug.LogWarning("Game must be running to skip time");
            }
        }

        [MenuItem("Tower Game/Debug/Skip to 18:00 (Work End)")]
        public static void SkipTo6PM()
        {
            if (Application.isPlaying && GameTimeManager.Instance != null)
            {
                GameTimeManager.Instance.SkipToHour(18);
            }
            else
            {
                Debug.LogWarning("Game must be running to skip time");
            }
        }
    }
}
