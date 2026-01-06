using System.Collections.Generic;
using UnityEngine;
using TowerGame.Core;
using TowerGame.Economy;

namespace TowerGame.Building
{
    /// <summary>
    /// Abstract base class for all tenant types (Office, Restaurant, Shop, Apartment)
    /// The Tower II style tenant system
    /// </summary>
    public abstract class Tenant : MonoBehaviour
    {
        [Header("Tenant Settings")]
        [SerializeField] protected int capacity = 10;
        [SerializeField] protected Transform entrancePoint;
        [SerializeField] protected List<Transform> occupantPositions = new List<Transform>();

        [Header("Business Hours")]
        [SerializeField] protected int openHour = 8;
        [SerializeField] protected int closeHour = 18;

        [Header("Floor Info")]
        [SerializeField] protected int floorNumber = 0;
        [SerializeField] protected int towerId = 0;
        [SerializeField] protected int segmentWidth = 9;

        [Header("Visual Settings")]
        [SerializeField] protected Color emptyColor = new Color(0.8f, 0.9f, 1f);
        [SerializeField] protected Color occupiedColor = new Color(1f, 0.9f, 0.5f);

        // Runtime data
        protected List<GameObject> currentOccupants = new List<GameObject>();
        protected HashSet<int> occupiedPositionIndices = new HashSet<int>();
        protected List<SpriteRenderer> windowRenderers = new List<SpriteRenderer>();
        protected long buildCost;
        protected bool isInitialized = false;

        #region Properties

        /// <summary>
        /// The type of this tenant
        /// </summary>
        public abstract TenantType TenantType { get; }

        /// <summary>
        /// Entrance position for people entering this tenant
        /// </summary>
        public Vector2 EntrancePosition => entrancePoint != null ?
            (Vector2)entrancePoint.position : (Vector2)transform.position + Vector2.down * 0.5f;

        /// <summary>
        /// Maximum capacity
        /// </summary>
        public int Capacity => capacity;

        /// <summary>
        /// Current number of occupants
        /// </summary>
        public int OccupantCount => currentOccupants.Count;

        /// <summary>
        /// Whether the tenant is at full capacity
        /// </summary>
        public bool IsFull => currentOccupants.Count >= capacity;

        /// <summary>
        /// Whether the tenant has any occupants
        /// </summary>
        public bool IsOccupied => currentOccupants.Count > 0;

        /// <summary>
        /// Floor number (0-indexed: 0 = 1F)
        /// </summary>
        public int Floor => floorNumber;

        /// <summary>
        /// Display floor number (1F, 2F, etc.)
        /// </summary>
        public int DisplayFloor => floorNumber + 1;

        /// <summary>
        /// Whether this tenant is on the ground floor
        /// </summary>
        public bool IsGroundFloor => floorNumber == 0;

        /// <summary>
        /// Tower ID this tenant belongs to
        /// </summary>
        public int TowerId => towerId;

        /// <summary>
        /// The original build cost
        /// </summary>
        public long BuildCost => buildCost;

        #endregion

        #region Unity Lifecycle

        protected virtual void Awake()
        {
            InitializePositions();
        }

        protected virtual void Start()
        {
            CollectWindowRenderers();
            CreateOccupantIndicators();
            SubscribeToEvents();
            isInitialized = true;
        }

        protected virtual void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize entrance and occupant positions
        /// </summary>
        protected virtual void InitializePositions()
        {
            float groundLocalY = -0.5f;

            if (entrancePoint == null)
            {
                var entranceGO = new GameObject("EntrancePoint");
                entranceGO.transform.SetParent(transform);
                entranceGO.transform.localPosition = new Vector3(0, groundLocalY, 0);
                entrancePoint = entranceGO.transform;
            }

            if (occupantPositions.Count == 0)
            {
                CreateDefaultOccupantPositions(groundLocalY);
            }
        }

        /// <summary>
        /// Create default positions for occupants
        /// </summary>
        protected virtual void CreateDefaultOccupantPositions(float groundLocalY)
        {
            float[] xOffsets = { -0.3f, 0f, 0.3f };

            for (int i = 0; i < xOffsets.Length; i++)
            {
                var posGO = new GameObject($"OccupantPosition_{i}");
                posGO.transform.SetParent(transform);
                posGO.transform.localPosition = new Vector3(xOffsets[i], groundLocalY, 0);
                occupantPositions.Add(posGO.transform);
            }
        }

        /// <summary>
        /// Collect window renderers for visual updates
        /// </summary>
        protected virtual void CollectWindowRenderers()
        {
            foreach (Transform child in transform)
            {
                if (child.name.StartsWith("Window"))
                {
                    SpriteRenderer sr = child.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        windowRenderers.Add(sr);
                    }
                }
            }
        }

        /// <summary>
        /// Create visual indicators for occupancy
        /// </summary>
        protected virtual void CreateOccupantIndicators()
        {
            // Override in derived classes for custom indicators
        }

        /// <summary>
        /// Subscribe to game events
        /// </summary>
        protected virtual void SubscribeToEvents()
        {
            if (GameTimeManager.Instance != null)
            {
                GameTimeManager.Instance.OnHourChanged += OnHourChanged;
                GameTimeManager.Instance.OnDayChanged += OnDayChanged;
            }
        }

        /// <summary>
        /// Unsubscribe from game events
        /// </summary>
        protected virtual void UnsubscribeFromEvents()
        {
            if (GameTimeManager.Instance != null)
            {
                GameTimeManager.Instance.OnHourChanged -= OnHourChanged;
                GameTimeManager.Instance.OnDayChanged -= OnDayChanged;
            }
        }

        #endregion

        #region Business Hours

        /// <summary>
        /// Check if the tenant is open (during business hours)
        /// </summary>
        public virtual bool IsOpen()
        {
            if (GameTimeManager.Instance == null) return true;

            float currentHour = GameTimeManager.Instance.CurrentHour;
            return currentHour >= openHour && currentHour < closeHour;
        }

        /// <summary>
        /// Get the opening hour
        /// </summary>
        public int OpenHour => openHour;

        /// <summary>
        /// Get the closing hour
        /// </summary>
        public int CloseHour => closeHour;

        #endregion

        #region Occupant Management

        /// <summary>
        /// Person enters the tenant
        /// </summary>
        public virtual bool Enter(GameObject person)
        {
            if (IsFull)
            {
                Debug.LogWarning($"[{GetType().Name}] Tenant is full, cannot enter");
                return false;
            }

            if (!currentOccupants.Contains(person))
            {
                currentOccupants.Add(person);

                // Hide the person sprite
                SpriteRenderer sr = person.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.enabled = false;
                }

                UpdateOccupancyVisuals();
                OnOccupantEntered(person);

                Debug.Log($"[{GetType().Name}] {person.name} entered. Occupants: {OccupantCount}");
                return true;
            }
            return false;
        }

        /// <summary>
        /// Person exits the tenant
        /// </summary>
        public virtual void Exit(GameObject person)
        {
            if (currentOccupants.Remove(person))
            {
                // Show the person sprite
                SpriteRenderer sr = person.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.enabled = true;
                }

                UpdateOccupancyVisuals();
                OnOccupantExited(person);

                Debug.Log($"[{GetType().Name}] {person.name} exited. Occupants: {OccupantCount}");
            }
        }

        /// <summary>
        /// Get an available position for an occupant
        /// </summary>
        public virtual Vector2? GetAvailablePosition()
        {
            for (int i = 0; i < occupantPositions.Count; i++)
            {
                if (!occupiedPositionIndices.Contains(i))
                {
                    occupiedPositionIndices.Add(i);
                    return occupantPositions[i].position;
                }
            }
            return null;
        }

        /// <summary>
        /// Release a position
        /// </summary>
        public virtual void ReleasePosition(Vector2 position)
        {
            for (int i = 0; i < occupantPositions.Count; i++)
            {
                if ((Vector2)occupantPositions[i].position == position)
                {
                    occupiedPositionIndices.Remove(i);
                    break;
                }
            }
        }

        /// <summary>
        /// Get position by index
        /// </summary>
        public Vector2 GetPosition(int index)
        {
            if (index >= 0 && index < occupantPositions.Count)
            {
                occupiedPositionIndices.Add(index);
                return occupantPositions[index].position;
            }
            return EntrancePosition;
        }

        /// <summary>
        /// Called when an occupant enters (override for custom behavior)
        /// </summary>
        protected virtual void OnOccupantEntered(GameObject person) { }

        /// <summary>
        /// Called when an occupant exits (override for custom behavior)
        /// </summary>
        protected virtual void OnOccupantExited(GameObject person) { }

        #endregion

        #region Visual Updates

        /// <summary>
        /// Update visual representation based on occupancy
        /// </summary>
        protected virtual void UpdateOccupancyVisuals()
        {
            for (int i = 0; i < windowRenderers.Count; i++)
            {
                bool isOccupied = occupiedPositionIndices.Contains(i % occupantPositions.Count);
                windowRenderers[i].color = isOccupied ? occupiedColor : emptyColor;
            }
        }

        #endregion

        #region Floor Info

        /// <summary>
        /// Set the floor number
        /// </summary>
        public void SetFloor(int floor)
        {
            floorNumber = floor;
            Debug.Log($"[{GetType().Name}] {gameObject.name} set to floor {DisplayFloor}F");
        }

        /// <summary>
        /// Set the tower ID
        /// </summary>
        public void SetTowerId(int id)
        {
            towerId = id;
            Debug.Log($"[{GetType().Name}] {gameObject.name} assigned to Tower {towerId}");
        }

        /// <summary>
        /// Set floor and tower ID together
        /// </summary>
        public void SetFloorInfo(int floor, int tower)
        {
            floorNumber = floor;
            towerId = tower;
            Debug.Log($"[{GetType().Name}] {gameObject.name} set to {DisplayFloor}F, Tower {towerId}");
        }

        /// <summary>
        /// Set the build cost for this tenant
        /// </summary>
        public void SetBuildCost(long cost)
        {
            buildCost = cost;
        }

        #endregion

        #region Economy

        /// <summary>
        /// Collect rent from this tenant
        /// </summary>
        public virtual long CollectRent()
        {
            if (!IsOccupied) return 0;

            long baseRent = EconomyManager.Instance?.GetDailyRent(TenantType) ?? 0;
            long actualRent = CalculateActualRent(baseRent);

            Debug.Log($"[{GetType().Name}] {gameObject.name} collected rent: ¥{actualRent:N0}");
            return actualRent;
        }

        /// <summary>
        /// Calculate actual rent with bonuses (override for custom calculations)
        /// </summary>
        protected virtual long CalculateActualRent(long baseRent)
        {
            // Base implementation returns base rent
            // Override in derived classes for time-based bonuses
            return baseRent;
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Called when the hour changes
        /// </summary>
        protected virtual void OnHourChanged(int hour) { }

        /// <summary>
        /// Called when the day changes
        /// </summary>
        protected virtual void OnDayChanged(int day) { }

        #endregion

        #region Debug

        protected virtual void OnDrawGizmos()
        {
            // Draw entrance
            Gizmos.color = Color.green;
            Vector3 entrance = entrancePoint != null ? entrancePoint.position : transform.position + Vector3.down * 0.5f;
            Gizmos.DrawWireSphere(entrance, 0.3f);

            // Draw occupant positions
            Gizmos.color = Color.blue;
            foreach (var pos in occupantPositions)
            {
                if (pos != null)
                {
                    Gizmos.DrawWireCube(pos.position, Vector3.one * 0.4f);
                }
            }
        }

        #endregion
    }
}
