using System.Collections.Generic;
using UnityEngine;
using TowerGame.Grid;
using TowerGame.Core;

namespace TowerGame.Building
{
    /// <summary>
    /// Elevator car state
    /// </summary>
    public enum ElevatorCarState
    {
        Idle,           // Waiting at a floor
        MovingUp,       // Moving upward
        MovingDown,     // Moving downward
        DoorsOpen,      // Doors open, loading/unloading
        DoorsClosing    // Doors closing
    }

    /// <summary>
    /// Direction request for elevator
    /// </summary>
    public enum ElevatorDirection
    {
        None,
        Up,
        Down
    }

    /// <summary>
    /// ElevatorCar - The moving cabin within an elevator shaft
    /// Handles passenger boarding, movement, and destination management
    /// </summary>
    public class ElevatorCar : MonoBehaviour
    {
        [Header("Elevator Settings")]
        [SerializeField] private int capacity = 21;
        [SerializeField] private float moveSpeed = 3f; // Units per second
        [SerializeField] private float doorOpenTime = 1f; // Seconds doors stay open

        [Header("Visual Settings")]
        [SerializeField] private Color emptyColor = new Color(0.6f, 0.6f, 0.7f);
        [SerializeField] private Color occupiedColor = new Color(0.5f, 0.7f, 0.9f);

        [Header("Runtime State (Read Only)")]
        [SerializeField] private int currentFloor = 0;
        [SerializeField] private ElevatorCarState state = ElevatorCarState.Idle;
        [SerializeField] private ElevatorDirection currentDirection = ElevatorDirection.None;

        // References
        private ElevatorShaft parentShaft;
        private SpriteRenderer spriteRenderer;

        // Passenger management
        private List<GameObject> passengers = new List<GameObject>();
        private Dictionary<int, List<GameObject>> passengerDestinations = new Dictionary<int, List<GameObject>>();

        // Movement
        private int targetFloor = 0;
        private float doorTimer = 0f;
        private HashSet<int> floorStops = new HashSet<int>();

        // Properties
        public int CurrentFloor => currentFloor;
        public int Capacity => capacity;
        public int PassengerCount => passengers.Count;
        public bool IsFull => passengers.Count >= capacity;
        public bool IsEmpty => passengers.Count == 0;
        public ElevatorCarState State => state;
        public ElevatorDirection Direction => currentDirection;
        public ElevatorShaft Shaft => parentShaft;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
                spriteRenderer.sprite = CreateCarSprite();
                spriteRenderer.sortingOrder = 10;
            }
            // Set initial scale (will be overridden by AddCar but good default)
            transform.localScale = new Vector3(0.4f, 2f, 1f);
            UpdateVisuals();
        }

        /// <summary>
        /// Initialize the elevator car
        /// </summary>
        public void Initialize(ElevatorShaft shaft, int startFloor)
        {
            parentShaft = shaft;
            currentFloor = startFloor;
            targetFloor = startFloor;
            state = ElevatorCarState.Idle;
            UpdatePosition();
        }

        private void Update()
        {
            switch (state)
            {
                case ElevatorCarState.Idle:
                    // Check for pending stops
                    ProcessNextStop();
                    break;

                case ElevatorCarState.MovingUp:
                case ElevatorCarState.MovingDown:
                    MoveTowardTarget();
                    break;

                case ElevatorCarState.DoorsOpen:
                    doorTimer -= Time.deltaTime;
                    if (doorTimer <= 0)
                    {
                        CloseDoors();
                    }
                    break;

                case ElevatorCarState.DoorsClosing:
                    // Transition to next state
                    ProcessNextStop();
                    break;
            }

            UpdateVisuals();
        }

        /// <summary>
        /// Add a floor stop request
        /// </summary>
        public void AddFloorStop(int floor)
        {
            if (floor >= 0 && floor <= parentShaft.TopFloor)
            {
                floorStops.Add(floor);
                Debug.Log($"[ElevatorCar] Stop added for floor {floor + 1}F");
            }
        }

        /// <summary>
        /// Board a passenger going to a specific floor
        /// </summary>
        public bool BoardPassenger(GameObject passenger, int destinationFloor)
        {
            if (IsFull)
            {
                Debug.Log("[ElevatorCar] Cannot board - car is full");
                return false;
            }

            if (state != ElevatorCarState.DoorsOpen && state != ElevatorCarState.Idle)
            {
                Debug.Log("[ElevatorCar] Cannot board - doors not open");
                return false;
            }

            passengers.Add(passenger);
            AddFloorStop(destinationFloor);

            // Track passenger's destination
            if (!passengerDestinations.ContainsKey(destinationFloor))
            {
                passengerDestinations[destinationFloor] = new List<GameObject>();
            }
            passengerDestinations[destinationFloor].Add(passenger);

            // Hide passenger (they're inside the elevator)
            SpriteRenderer sr = passenger.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.enabled = false;
            }

            Debug.Log($"[ElevatorCar] Passenger boarded. Count: {PassengerCount}/{Capacity}. Destination: {destinationFloor + 1}F");
            return true;
        }

        /// <summary>
        /// Check if a specific passenger should exit at the current floor
        /// </summary>
        public bool ShouldPassengerExit(GameObject passenger)
        {
            if (passengerDestinations.TryGetValue(currentFloor, out var exitingPassengers))
            {
                return exitingPassengers.Contains(passenger);
            }
            return false;
        }

        /// <summary>
        /// Handle a single passenger exiting the elevator
        /// </summary>
        public void ExitPassenger(GameObject passenger)
        {
            // Remove from passengers list
            passengers.Remove(passenger);

            // Remove from destination tracking
            if (passengerDestinations.TryGetValue(currentFloor, out var exitingPassengers))
            {
                exitingPassengers.Remove(passenger);

                // Clean up if no more passengers for this floor
                if (exitingPassengers.Count == 0)
                {
                    passengerDestinations.Remove(currentFloor);
                    floorStops.Remove(currentFloor);
                }
            }

            // Show passenger sprite again
            SpriteRenderer sr = passenger.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.enabled = true;
            }

            Debug.Log($"[ElevatorCar] Passenger exited at {currentFloor + 1}F. Remaining: {passengers.Count}");
        }

        /// <summary>
        /// Get passengers exiting at the current floor (for reference only, does not modify state)
        /// </summary>
        public List<GameObject> GetExitingPassengers()
        {
            if (passengerDestinations.TryGetValue(currentFloor, out var exitingPassengers))
            {
                return new List<GameObject>(exitingPassengers);
            }
            return new List<GameObject>();
        }

        /// <summary>
        /// Open doors at current floor
        /// </summary>
        public void OpenDoors()
        {
            state = ElevatorCarState.DoorsOpen;
            doorTimer = doorOpenTime;
            Debug.Log($"[ElevatorCar] Doors opened at {currentFloor + 1}F");
        }

        private void CloseDoors()
        {
            state = ElevatorCarState.DoorsClosing;
            Debug.Log($"[ElevatorCar] Doors closing at {currentFloor + 1}F");
        }

        private void ProcessNextStop()
        {
            if (floorStops.Count == 0)
            {
                state = ElevatorCarState.Idle;
                currentDirection = ElevatorDirection.None;
                return;
            }

            // Find next stop based on current direction
            int nextStop = FindNextStop();

            if (nextStop == currentFloor)
            {
                // Already at destination
                floorStops.Remove(currentFloor);
                OpenDoors();
                return;
            }

            // Start moving
            targetFloor = nextStop;
            if (targetFloor > currentFloor)
            {
                state = ElevatorCarState.MovingUp;
                currentDirection = ElevatorDirection.Up;
            }
            else
            {
                state = ElevatorCarState.MovingDown;
                currentDirection = ElevatorDirection.Down;
            }

            Debug.Log($"[ElevatorCar] Moving from {currentFloor + 1}F to {targetFloor + 1}F");
        }

        private int FindNextStop()
        {
            if (floorStops.Count == 0) return currentFloor;

            // Continue in current direction if possible
            if (currentDirection == ElevatorDirection.Up)
            {
                int nextUp = int.MaxValue;
                foreach (int floor in floorStops)
                {
                    if (floor >= currentFloor && floor < nextUp)
                    {
                        nextUp = floor;
                    }
                }
                if (nextUp != int.MaxValue) return nextUp;
            }
            else if (currentDirection == ElevatorDirection.Down)
            {
                int nextDown = int.MinValue;
                foreach (int floor in floorStops)
                {
                    if (floor <= currentFloor && floor > nextDown)
                    {
                        nextDown = floor;
                    }
                }
                if (nextDown != int.MinValue) return nextDown;
            }

            // No stops in current direction, find closest stop
            int closest = currentFloor;
            int minDistance = int.MaxValue;
            foreach (int floor in floorStops)
            {
                int distance = Mathf.Abs(floor - currentFloor);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closest = floor;
                }
            }

            return closest;
        }

        private void MoveTowardTarget()
        {
            // Target Y is the car CENTER position (walk level + half car height)
            float carHeight = transform.localScale.y;
            float targetWalkLevel = GetFloorWalkLevel(targetFloor);
            float targetY = targetWalkLevel + (carHeight / 2f);
            float currentY = transform.position.y;
            float step = moveSpeed * Time.deltaTime;

            if (Mathf.Abs(targetY - currentY) < step)
            {
                // Arrived at floor
                currentFloor = targetFloor;
                UpdatePosition();

                if (floorStops.Contains(currentFloor))
                {
                    floorStops.Remove(currentFloor);
                    OpenDoors();
                }
                else
                {
                    ProcessNextStop();
                }
            }
            else
            {
                // Move toward target
                float direction = targetY > currentY ? 1f : -1f;
                transform.position += new Vector3(0, direction * step, 0);

                // Check if passing a floor with a stop
                // Calculate floor from car bottom position
                float carBottom = transform.position.y - (carHeight / 2f);
                int passingFloor = GetFloorFromY(carBottom);
                if (passingFloor != currentFloor && floorStops.Contains(passingFloor))
                {
                    // Stop at this floor
                    currentFloor = passingFloor;
                    UpdatePosition();
                    floorStops.Remove(currentFloor);
                    OpenDoors();
                }
            }
        }

        /// <summary>
        /// Get Y position where people walk on a floor (for car positioning)
        /// </summary>
        private float GetFloorWalkLevel(int floor)
        {
            if (GridManager.Instance != null && GridManager.Instance.Config != null)
            {
                return GridManager.Instance.Config.GetFloorWalkLevel(floor);
            }
            return -3f + (floor * 3f); // Fallback
        }

        private int GetFloorFromY(float y)
        {
            if (GridManager.Instance != null && GridManager.Instance.Config != null)
            {
                var config = GridManager.Instance.Config;
                return Mathf.FloorToInt((y - config.groundLevel) / config.floorHeight);
            }
            return Mathf.FloorToInt((y + 3f) / 3f);
        }

        private void UpdatePosition()
        {
            // Position car so its BOTTOM is at floor walk level
            // Car height is determined by localScale.y
            float carHeight = transform.localScale.y;
            float walkLevel = GetFloorWalkLevel(currentFloor);
            float y = walkLevel + (carHeight / 2f);
            transform.position = new Vector3(transform.position.x, y, transform.position.z);
        }

        private void UpdateVisuals()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = IsEmpty ? emptyColor : occupiedColor;
            }
        }

        private Sprite CreateCarSprite()
        {
            Texture2D tex = new Texture2D(8, 12);
            Color[] colors = new Color[8 * 12];

            for (int y = 0; y < 12; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    // Create elevator car shape
                    if (y == 0 || y == 11 || x == 0 || x == 7)
                    {
                        colors[y * 8 + x] = Color.gray; // Frame
                    }
                    else
                    {
                        colors[y * 8 + x] = Color.white; // Interior
                    }
                }
            }

            tex.SetPixels(colors);
            tex.Apply();
            tex.filterMode = FilterMode.Point;

            return Sprite.Create(tex, new Rect(0, 0, 8, 12), new Vector2(0.5f, 0.5f), 8);
        }

        /// <summary>
        /// Get the world position for passengers to wait at this floor
        /// Delegates to parent shaft for consistency
        /// </summary>
        public Vector2 GetWaitPosition(int floor)
        {
            if (parentShaft != null)
            {
                return parentShaft.GetWaitPosition(floor);
            }
            // Fallback
            float y = GetFloorWalkLevel(floor);
            return new Vector2(transform.position.x - 1f, y);
        }

        /// <summary>
        /// Get the world position for passengers exiting at this floor
        /// Delegates to parent shaft for consistency
        /// </summary>
        public Vector2 GetExitPosition(int floor)
        {
            if (parentShaft != null)
            {
                return parentShaft.GetExitPosition(floor);
            }
            // Fallback
            float y = GetFloorWalkLevel(floor);
            return new Vector2(transform.position.x + 1f, y);
        }
    }
}
