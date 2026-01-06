using System.Collections.Generic;
using UnityEngine;
using TowerGame.Grid;

namespace TowerGame.Building
{
    /// <summary>
    /// ElevatorManager - Central controller for all elevator shafts
    /// Handles shaft registration, dispatch logic, and floor access queries
    /// </summary>
    public class ElevatorManager : MonoBehaviour
    {
        public static ElevatorManager Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private int maxShaftsPerTower = 24;

        [Header("Runtime (Read Only)")]
        [SerializeField] private List<ElevatorShaft> allShafts = new List<ElevatorShaft>();

        // Tower-indexed shaft lists
        private Dictionary<int, List<ElevatorShaft>> shaftsByTower = new Dictionary<int, List<ElevatorShaft>>();

        // Events
        public event System.Action<ElevatorShaft> OnShaftCreated;
        public event System.Action<ElevatorShaft, int> OnCarArrived;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// Create a new elevator shaft
        /// </summary>
        public ElevatorShaft CreateShaft(int segmentX, int bottomFloor, int topFloor, int towerId)
        {
            // Validate tower shaft limit
            if (!shaftsByTower.ContainsKey(towerId))
            {
                shaftsByTower[towerId] = new List<ElevatorShaft>();
            }

            if (shaftsByTower[towerId].Count >= maxShaftsPerTower)
            {
                Debug.LogWarning($"[ElevatorManager] Tower {towerId} has reached max shafts ({maxShaftsPerTower})");
                return null;
            }

            // Create shaft GameObject
            GameObject shaftGO = new GameObject($"ElevatorShaft_Tower{towerId}_{shaftsByTower[towerId].Count}");
            shaftGO.transform.SetParent(transform);

            ElevatorShaft shaft = shaftGO.AddComponent<ElevatorShaft>();
            shaft.Initialize(segmentX, bottomFloor, topFloor, towerId);

            // Subscribe to events
            shaft.OnCarArrived += HandleCarArrived;

            // Register
            allShafts.Add(shaft);
            shaftsByTower[towerId].Add(shaft);

            OnShaftCreated?.Invoke(shaft);

            Debug.Log($"[ElevatorManager] Shaft created at segment {segmentX}, Tower {towerId}. Total: {allShafts.Count}");
            return shaft;
        }

        /// <summary>
        /// Get all shafts for a tower
        /// </summary>
        public List<ElevatorShaft> GetShaftsForTower(int towerId)
        {
            if (shaftsByTower.TryGetValue(towerId, out var shafts))
            {
                return new List<ElevatorShaft>(shafts);
            }
            return new List<ElevatorShaft>();
        }

        /// <summary>
        /// Get shaft at a specific segment position
        /// </summary>
        public ElevatorShaft GetShaftAtPosition(int segmentX, int towerId)
        {
            if (!shaftsByTower.ContainsKey(towerId)) return null;

            foreach (var shaft in shaftsByTower[towerId])
            {
                if (shaft.SegmentX == segmentX)
                {
                    return shaft;
                }
            }
            return null;
        }

        /// <summary>
        /// Find the best shaft for traveling from one floor to another
        /// </summary>
        public ElevatorShaft FindBestShaft(int fromFloor, int toFloor, int towerId)
        {
            if (!shaftsByTower.ContainsKey(towerId)) return null;

            ElevatorShaft bestShaft = null;
            float bestWaitTime = float.MaxValue;

            foreach (var shaft in shaftsByTower[towerId])
            {
                // Check if shaft serves both floors
                if (fromFloor >= shaft.BottomFloor && fromFloor <= shaft.TopFloor &&
                    toFloor >= shaft.BottomFloor && toFloor <= shaft.TopFloor)
                {
                    float waitTime = shaft.GetEstimatedWaitTime(fromFloor);
                    if (waitTime < bestWaitTime)
                    {
                        bestWaitTime = waitTime;
                        bestShaft = shaft;
                    }
                }
            }

            return bestShaft;
        }

        /// <summary>
        /// Call an elevator to a floor
        /// </summary>
        public bool CallElevator(int floor, ElevatorDirection direction, int towerId, GameObject passenger = null)
        {
            var shaft = FindShaftServingFloor(floor, towerId);
            if (shaft == null)
            {
                Debug.LogWarning($"[ElevatorManager] No shaft serves floor {floor + 1}F in Tower {towerId}");
                return false;
            }

            shaft.CallToFloor(floor, direction, passenger);
            return true;
        }

        /// <summary>
        /// Find any shaft that serves a specific floor
        /// </summary>
        public ElevatorShaft FindShaftServingFloor(int floor, int towerId)
        {
            if (!shaftsByTower.ContainsKey(towerId)) return null;

            foreach (var shaft in shaftsByTower[towerId])
            {
                if (floor >= shaft.BottomFloor && floor <= shaft.TopFloor)
                {
                    return shaft;
                }
            }
            return null;
        }

        /// <summary>
        /// Check if a floor is accessible via elevator
        /// </summary>
        public bool CanAccessFloor(int floor, int towerId)
        {
            return FindShaftServingFloor(floor, towerId) != null;
        }

        /// <summary>
        /// Get wait position for a passenger at a floor
        /// </summary>
        public Vector2? GetElevatorWaitPosition(int floor, int towerId)
        {
            var shaft = FindShaftServingFloor(floor, towerId);
            if (shaft != null)
            {
                return shaft.GetWaitPosition(floor);
            }
            return null;
        }

        /// <summary>
        /// Get an available elevator car at a floor
        /// </summary>
        public ElevatorCar GetAvailableCarAtFloor(int floor, int towerId)
        {
            if (!shaftsByTower.ContainsKey(towerId)) return null;

            foreach (var shaft in shaftsByTower[towerId])
            {
                var car = shaft.GetAvailableCarAtFloor(floor);
                if (car != null) return car;
            }
            return null;
        }

        /// <summary>
        /// Extend all shafts in a tower to reach a new floor
        /// </summary>
        public void ExtendShaftsToFloor(int newTopFloor, int towerId)
        {
            if (!shaftsByTower.ContainsKey(towerId)) return;

            foreach (var shaft in shaftsByTower[towerId])
            {
                shaft.ExtendToFloor(newTopFloor);
            }

            Debug.Log($"[ElevatorManager] Extended Tower {towerId} shafts to floor {newTopFloor + 1}F");
        }

        /// <summary>
        /// Get total number of shafts
        /// </summary>
        public int GetTotalShaftCount()
        {
            return allShafts.Count;
        }

        /// <summary>
        /// Get shaft count for a specific tower
        /// </summary>
        public int GetShaftCount(int towerId)
        {
            if (shaftsByTower.TryGetValue(towerId, out var shafts))
            {
                return shafts.Count;
            }
            return 0;
        }

        private void HandleCarArrived(ElevatorShaft shaft, int floor)
        {
            OnCarArrived?.Invoke(shaft, floor);
        }

        /// <summary>
        /// Remove a shaft (for demolition)
        /// </summary>
        public void RemoveShaft(ElevatorShaft shaft)
        {
            if (shaft == null) return;

            shaft.OnCarArrived -= HandleCarArrived;

            allShafts.Remove(shaft);

            if (shaftsByTower.TryGetValue(shaft.TowerId, out var shafts))
            {
                shafts.Remove(shaft);
            }

            Destroy(shaft.gameObject);
            Debug.Log($"[ElevatorManager] Shaft removed. Remaining: {allShafts.Count}");
        }

        private void OnDestroy()
        {
            // Clean up event subscriptions
            foreach (var shaft in allShafts)
            {
                if (shaft != null)
                {
                    shaft.OnCarArrived -= HandleCarArrived;
                }
            }
        }
    }
}
