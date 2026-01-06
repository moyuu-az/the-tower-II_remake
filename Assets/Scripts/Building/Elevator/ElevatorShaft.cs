using System.Collections.Generic;
using UnityEngine;
using TowerGame.Grid;

namespace TowerGame.Building
{
    /// <summary>
    /// Floor call request for elevator
    /// </summary>
    [System.Serializable]
    public class ElevatorCallRequest
    {
        public int floor;
        public ElevatorDirection direction;
        public float waitTime;
        public List<GameObject> waitingPassengers;

        public ElevatorCallRequest(int floor, ElevatorDirection direction)
        {
            this.floor = floor;
            this.direction = direction;
            this.waitTime = 0f;
            this.waitingPassengers = new List<GameObject>();
        }
    }

    /// <summary>
    /// ElevatorShaft - Vertical structure containing elevator cars
    /// Spans from lobby floor to top floor, handles call requests
    /// </summary>
    public class ElevatorShaft : MonoBehaviour
    {
        [Header("Shaft Settings")]
        [SerializeField] private int segmentX; // X position in grid
        [SerializeField] private int bottomFloor = 0; // Usually lobby floor (0)
        [SerializeField] private int topFloor = 1; // Highest floor this shaft reaches
        [SerializeField] private int towerId = 0;

        [Header("Car Settings")]
        [SerializeField] private int maxCars = 1; // Maximum cars in this shaft

        [Header("Visual Settings")]
        [SerializeField] private Color shaftColor = new Color(0.5f, 0.5f, 0.55f);
        [SerializeField] private Color machineRoomColor = new Color(0.3f, 0.3f, 0.35f);
        [SerializeField] private float shaftWidthUnits = 1.0f; // 1 segment width

        [Header("Runtime (Read Only)")]
        [SerializeField] private List<ElevatorCar> cars = new List<ElevatorCar>();
        [SerializeField] private List<ElevatorCallRequest> pendingCalls = new List<ElevatorCallRequest>();

        // Properties
        public int SegmentX => segmentX;
        public int BottomFloor => bottomFloor;
        public int TopFloor => topFloor;
        public int TowerId => towerId;
        public int FloorCount => topFloor - bottomFloor + 1;
        public List<ElevatorCar> Cars => cars;

        // Events
        public event System.Action<ElevatorShaft, int> OnCarArrived; // Shaft, floor

        private SpriteRenderer shaftRenderer;
        private GameObject topMachineRoom;
        private GameObject bottomMachineRoom;

        private void Awake()
        {
            CreateShaftVisual();
        }

        private void Update()
        {
            // Update wait times
            foreach (var call in pendingCalls)
            {
                call.waitTime += Time.deltaTime;
            }

            // Check for car arrivals and process calls
            ProcessPendingCalls();
        }

        /// <summary>
        /// Initialize the elevator shaft
        /// </summary>
        public void Initialize(int xPosition, int bottom, int top, int assignedTowerId)
        {
            segmentX = xPosition;
            bottomFloor = bottom;
            topFloor = top;
            towerId = assignedTowerId;

            UpdateShaftVisual();
            CreateInitialCar();

            Debug.Log($"[ElevatorShaft] Initialized at segment {segmentX}, floors {bottomFloor + 1}F to {topFloor + 1}F, Tower {towerId}");
        }

        /// <summary>
        /// Extend the shaft to reach a higher floor
        /// </summary>
        public void ExtendToFloor(int newTopFloor)
        {
            if (newTopFloor > topFloor)
            {
                topFloor = newTopFloor;
                UpdateShaftVisual();
                Debug.Log($"[ElevatorShaft] Extended to {topFloor + 1}F");
            }
        }

        /// <summary>
        /// Call elevator to a floor
        /// </summary>
        public void CallToFloor(int floor, ElevatorDirection direction, GameObject passenger = null)
        {
            if (floor < bottomFloor || floor > topFloor)
            {
                Debug.LogWarning($"[ElevatorShaft] Floor {floor + 1}F is outside shaft range");
                return;
            }

            // Check if call already exists
            var existingCall = pendingCalls.Find(c => c.floor == floor && c.direction == direction);
            if (existingCall != null)
            {
                if (passenger != null && !existingCall.waitingPassengers.Contains(passenger))
                {
                    existingCall.waitingPassengers.Add(passenger);
                }
                return;
            }

            // Create new call
            var call = new ElevatorCallRequest(floor, direction);
            if (passenger != null)
            {
                call.waitingPassengers.Add(passenger);
            }
            pendingCalls.Add(call);

            // Dispatch a car
            DispatchCarToFloor(floor);

            Debug.Log($"[ElevatorShaft] Call registered: {floor + 1}F going {direction}");
        }

        /// <summary>
        /// Get a car that's at the specified floor with doors open
        /// </summary>
        public ElevatorCar GetAvailableCarAtFloor(int floor)
        {
            foreach (var car in cars)
            {
                if (car.CurrentFloor == floor &&
                    (car.State == ElevatorCarState.DoorsOpen || car.State == ElevatorCarState.Idle))
                {
                    return car;
                }
            }
            return null;
        }

        /// <summary>
        /// Check if any car is at the specified floor
        /// </summary>
        public bool HasCarAtFloor(int floor)
        {
            return GetAvailableCarAtFloor(floor) != null;
        }

        /// <summary>
        /// Get estimated wait time for a floor
        /// </summary>
        public float GetEstimatedWaitTime(int floor)
        {
            // Find the closest idle or moving-toward car
            float minTime = float.MaxValue;

            foreach (var car in cars)
            {
                if (car.CurrentFloor == floor && car.State == ElevatorCarState.DoorsOpen)
                {
                    return 0f; // Car already here
                }

                int distance = Mathf.Abs(car.CurrentFloor - floor);
                float estimatedTime = distance * 1f; // 1 second per floor estimate

                if (estimatedTime < minTime)
                {
                    minTime = estimatedTime;
                }
            }

            return minTime == float.MaxValue ? 10f : minTime;
        }

        private void CreateInitialCar()
        {
            if (cars.Count < maxCars)
            {
                AddCar();
            }
        }

        /// <summary>
        /// Add a new elevator car to this shaft
        /// </summary>
        public ElevatorCar AddCar()
        {
            if (cars.Count >= maxCars)
            {
                Debug.LogWarning($"[ElevatorShaft] Maximum cars ({maxCars}) reached");
                return null;
            }

            GameObject carGO = new GameObject($"ElevatorCar_{cars.Count}");
            // Don't parent to shaft (which is scaled) - parent to ElevatorManager instead
            if (ElevatorManager.Instance != null)
            {
                carGO.transform.SetParent(ElevatorManager.Instance.transform);
            }

            // Car size: 0.8 units wide, 2 units tall (fits within 1-unit shaft)
            float carHeight = 2f;
            carGO.transform.localScale = new Vector3(0.8f, carHeight, 1f);

            // Position at shaft center - car bottom should be at floor walk level
            float x = GetShaftWorldX();
            float walkLevel = GetFloorWalkLevel(bottomFloor);
            float y = walkLevel + (carHeight / 2f); // Center of car above walk level
            carGO.transform.position = new Vector3(x, y, 0);

            ElevatorCar car = carGO.AddComponent<ElevatorCar>();
            car.Initialize(this, bottomFloor);

            cars.Add(car);

            Debug.Log($"[ElevatorShaft] Car added. Total cars: {cars.Count}");
            return car;
        }

        private void DispatchCarToFloor(int floor)
        {
            if (cars.Count == 0) return;

            // Find the best car to dispatch
            ElevatorCar bestCar = null;
            int minDistance = int.MaxValue;

            foreach (var car in cars)
            {
                // Prefer idle cars
                if (car.State == ElevatorCarState.Idle)
                {
                    int distance = Mathf.Abs(car.CurrentFloor - floor);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        bestCar = car;
                    }
                }
            }

            // If no idle car, use any car
            if (bestCar == null)
            {
                foreach (var car in cars)
                {
                    int distance = Mathf.Abs(car.CurrentFloor - floor);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        bestCar = car;
                    }
                }
            }

            if (bestCar != null)
            {
                bestCar.AddFloorStop(floor);
            }
        }

        private void ProcessPendingCalls()
        {
            // Check if any car has arrived at a call floor
            for (int i = pendingCalls.Count - 1; i >= 0; i--)
            {
                var call = pendingCalls[i];
                var car = GetAvailableCarAtFloor(call.floor);

                if (car != null)
                {
                    // Car has arrived - notify passengers
                    foreach (var passenger in call.waitingPassengers)
                    {
                        // Passengers will handle boarding themselves
                    }

                    OnCarArrived?.Invoke(this, call.floor);
                    pendingCalls.RemoveAt(i);
                }
            }
        }

        private void CreateShaftVisual()
        {
            shaftRenderer = GetComponent<SpriteRenderer>();
            if (shaftRenderer == null)
            {
                shaftRenderer = gameObject.AddComponent<SpriteRenderer>();
                shaftRenderer.sprite = CreateShaftSprite();
                shaftRenderer.color = shaftColor;
                shaftRenderer.sortingOrder = 0;
            }

            // Create machine rooms (The Tower II style)
            CreateMachineRooms();
        }

        private void CreateMachineRooms()
        {
            // Destroy existing machine rooms if any (prevent duplicates)
            if (topMachineRoom != null)
            {
                Destroy(topMachineRoom);
            }
            if (bottomMachineRoom != null)
            {
                Destroy(bottomMachineRoom);
            }

            // Create machine rooms as independent objects (not affected by shaft scaling)
            // Top machine room
            topMachineRoom = new GameObject("TopMachineRoom");
            if (ElevatorManager.Instance != null)
            {
                topMachineRoom.transform.SetParent(ElevatorManager.Instance.transform);
            }
            SpriteRenderer topSR = topMachineRoom.AddComponent<SpriteRenderer>();
            topSR.sprite = CreateShaftSprite();
            topSR.color = machineRoomColor;
            topSR.sortingOrder = 2;

            // Bottom machine room
            bottomMachineRoom = new GameObject("BottomMachineRoom");
            if (ElevatorManager.Instance != null)
            {
                bottomMachineRoom.transform.SetParent(ElevatorManager.Instance.transform);
            }
            SpriteRenderer bottomSR = bottomMachineRoom.AddComponent<SpriteRenderer>();
            bottomSR.sprite = CreateShaftSprite();
            bottomSR.color = machineRoomColor;
            bottomSR.sortingOrder = 2;
        }

        private void UpdateShaftVisual()
        {
            if (shaftRenderer == null) return;

            float x = GetShaftWorldX();
            float floorHeight = GetFloorHeight();
            float machineRoomHeight = floorHeight * 0.4f; // Machine rooms are 40% of floor height

            // Calculate full shaft span
            float fullBottomY = GetFloorCenterY(bottomFloor) - floorHeight / 2f;
            float fullTopY = GetFloorCenterY(topFloor) + floorHeight / 2f;
            float fullHeight = fullTopY - fullBottomY;
            float centerY = (fullBottomY + fullTopY) / 2f;

            // Position main shaft body
            transform.position = new Vector3(x, centerY, 0);
            transform.localScale = new Vector3(shaftWidthUnits, fullHeight, 1f);

            // Position machine rooms (world coordinates, not affected by shaft scale)
            if (topMachineRoom != null)
            {
                float topRoomCenterY = fullTopY + machineRoomHeight / 2f;
                topMachineRoom.transform.position = new Vector3(x, topRoomCenterY, 0);
                topMachineRoom.transform.localScale = new Vector3(shaftWidthUnits * 1.2f, machineRoomHeight, 1f);
            }

            if (bottomMachineRoom != null)
            {
                float bottomRoomCenterY = fullBottomY - machineRoomHeight / 2f;
                bottomMachineRoom.transform.position = new Vector3(x, bottomRoomCenterY, 0);
                bottomMachineRoom.transform.localScale = new Vector3(shaftWidthUnits * 1.2f, machineRoomHeight, 1f);
            }

            Debug.Log($"[ElevatorShaft] Visual updated: floors {bottomFloor + 1}F to {topFloor + 1}F");
        }

        private float GetShaftWorldX()
        {
            if (GridManager.Instance != null && GridManager.Instance.Config != null)
            {
                return segmentX * GridManager.Instance.Config.segmentWidth;
            }
            return segmentX;
        }

        /// <summary>
        /// Get Y position for floor center (for building/shaft placement)
        /// </summary>
        private float GetFloorCenterY(int floor)
        {
            if (GridManager.Instance != null && GridManager.Instance.Config != null)
            {
                return GridManager.Instance.Config.GetFloorCenterY(floor);
            }
            return -3f + (floor * 3f) + 1.5f;
        }

        /// <summary>
        /// Get Y position where people walk (floor level)
        /// </summary>
        private float GetFloorWalkLevel(int floor)
        {
            if (GridManager.Instance != null && GridManager.Instance.Config != null)
            {
                return GridManager.Instance.Config.GetFloorWalkLevel(floor);
            }
            return -3f + (floor * 3f);
        }

        private float GetFloorHeight()
        {
            if (GridManager.Instance != null && GridManager.Instance.Config != null)
            {
                return GridManager.Instance.Config.floorHeight;
            }
            return 3f;
        }

        private Sprite CreateShaftSprite()
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

        /// <summary>
        /// Get wait position for passengers at a floor (at floor walk level)
        /// </summary>
        public Vector2 GetWaitPosition(int floor)
        {
            float shaftX = GetShaftWorldX();
            float x = shaftX - 1.0f; // Wait 1 unit to the left of shaft
            float y = GetFloorWalkLevel(floor);
            Debug.Log($"[ElevatorShaft] GetWaitPosition: segment={segmentX}, shaftX={shaftX}, waitX={x}, floor={floor}, y={y}");
            return new Vector2(x, y);
        }

        /// <summary>
        /// Get exit position for passengers at a floor (at floor walk level)
        /// </summary>
        public Vector2 GetExitPosition(int floor)
        {
            float shaftX = GetShaftWorldX();
            float x = shaftX + 1.0f; // Exit 1 unit to the right of shaft
            float y = GetFloorWalkLevel(floor);
            Debug.Log($"[ElevatorShaft] GetExitPosition: segment={segmentX}, shaftX={shaftX}, exitX={x}, floor={floor}, y={y}");
            return new Vector2(x, y);
        }

        private void OnDestroy()
        {
            // Clean up machine rooms
            if (topMachineRoom != null)
            {
                Destroy(topMachineRoom);
            }
            if (bottomMachineRoom != null)
            {
                Destroy(bottomMachineRoom);
            }

            // Clean up elevator cars
            foreach (var car in cars)
            {
                if (car != null)
                {
                    Destroy(car.gameObject);
                }
            }
            cars.Clear();
        }

        private void OnDrawGizmos()
        {
            if (Application.isPlaying) return;

            // Draw shaft outline
            Gizmos.color = Color.cyan;
            float x = segmentX;
            float bottomY = -3f + (bottomFloor * 3f);
            float topY = -3f + ((topFloor + 1) * 3f);

            Gizmos.DrawLine(new Vector3(x - 0.5f, bottomY, 0), new Vector3(x - 0.5f, topY, 0));
            Gizmos.DrawLine(new Vector3(x + 0.5f, bottomY, 0), new Vector3(x + 0.5f, topY, 0));
            Gizmos.DrawLine(new Vector3(x - 0.5f, bottomY, 0), new Vector3(x + 0.5f, bottomY, 0));
            Gizmos.DrawLine(new Vector3(x - 0.5f, topY, 0), new Vector3(x + 0.5f, topY, 0));
        }
    }
}
