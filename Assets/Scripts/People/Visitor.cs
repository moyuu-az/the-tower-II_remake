using UnityEngine;
using TowerGame.Core;
using TowerGame.Building;

namespace TowerGame.People
{
    /// <summary>
    /// Visitor states for shopping/dining behavior
    /// </summary>
    public enum VisitorState
    {
        Approaching,         // Walking to building entrance
        EnteringLobby,       // Walking into lobby
        WaitingForElevator,  // Waiting at elevator to go up
        RidingElevatorUp,    // Inside elevator going up
        ExitingElevator,     // Exiting elevator at destination floor
        WalkingToTenant,     // Walking to shop/restaurant
        Browsing,            // Shopping or dining inside tenant
        LeavingTenant,       // Walking from tenant to elevator
        WaitingForElevatorDown, // Waiting at elevator to go down
        RidingElevatorDown,  // Inside elevator going down
        ExitingBuilding,     // Walking out of building
        Leaving              // Walking away from building
    }

    /// <summary>
    /// Visitor type - determines what kind of tenant they visit
    /// </summary>
    public enum VisitorType
    {
        Shopper,    // Visits shops
        Diner       // Visits restaurants
    }

    /// <summary>
    /// Visitor who comes to shop or dine at tenants
    /// </summary>
    public class Visitor : Person
    {
        [Header("Visitor Settings")]
        [SerializeField] private int visitorId;
        [SerializeField] private VisitorType visitorType = VisitorType.Shopper;
        [SerializeField] private float browseTime = 60f; // Game seconds to spend in tenant

        [Header("References")]
        [SerializeField] private Tenant targetTenant;

        [Header("Positions")]
        [SerializeField] private Vector2 spawnPosition;
        [SerializeField] private Vector2 tenantPosition;

        [Header("Visitor State (Read Only)")]
        [SerializeField] private VisitorState visitorState = VisitorState.Approaching;

        // Elevator handling
        private ElevatorCar currentElevator;
        private ElevatorShaft targetShaft;
        private int targetFloor;
        private float elevatorWaitTimer;
        private const float MAX_ELEVATOR_WAIT = 30f;

        // Browsing
        private float browseTimer;

        // Properties
        public VisitorState State => visitorState;
        public int VisitorId => visitorId;
        public VisitorType Type => visitorType;
        public Tenant TargetTenant => targetTenant;
        public bool IsInTenant => visitorState == VisitorState.Browsing;

        protected override void Awake()
        {
            base.Awake();
        }

        /// <summary>
        /// Initialize the visitor with target tenant
        /// </summary>
        public void Initialize(int id, Tenant tenant, Vector2 spawnPoint, VisitorType type)
        {
            visitorId = id;
            targetTenant = tenant;
            spawnPosition = spawnPoint;
            visitorType = type;

            if (tenant != null)
            {
                targetFloor = tenant.Floor;

                // Set browse time based on tenant type
                if (tenant is Shop shop)
                {
                    browseTime = shop.GetRandomBrowseTime();
                }
                else if (tenant is Restaurant)
                {
                    browseTime = Random.Range(600f, 1800f); // 10-30 game minutes for dining
                }
                else
                {
                    browseTime = Random.Range(60f, 300f); // Default 1-5 game minutes
                }
            }

            Debug.Log($"[Visitor {id}] Initialized. Type: {type}, Target: {tenant?.name}, Browse time: {browseTime}s");
        }

        /// <summary>
        /// Start the visitor's journey to the target tenant
        /// </summary>
        public void StartVisit()
        {
            if (targetTenant == null)
            {
                Debug.LogWarning($"[Visitor {visitorId}] No target tenant assigned");
                Destroy(gameObject);
                return;
            }

            gameObject.SetActive(true);
            TeleportTo(spawnPosition);
            visitorState = VisitorState.Approaching;

            // Go to building entrance
            var lobby = GetTowerLobby();
            if (lobby != null)
            {
                MoveTo(lobby.EntrancePosition);
            }
            else
            {
                MoveTo(targetTenant.EntrancePosition);
            }

            Debug.Log($"[Visitor {visitorId}] Starting visit to {targetTenant.name}");
        }

        private Lobby GetTowerLobby()
        {
            if (FloorSystemManager.Instance == null) return null;
            if (targetTenant == null) return null;

            int targetTowerId = targetTenant.TowerId;
            var towers = FloorSystemManager.Instance.GetAllTowers();

            foreach (var tower in towers)
            {
                if (tower.towerId == targetTowerId && tower.lobby != null)
                {
                    return tower.lobby;
                }
            }

            foreach (var tower in towers)
            {
                if (tower.lobby != null)
                {
                    return tower.lobby;
                }
            }
            return null;
        }

        protected override void OnReachedDestination()
        {
            switch (visitorState)
            {
                case VisitorState.Approaching:
                    visitorState = VisitorState.EnteringLobby;
                    if (targetTenant != null && !targetTenant.IsGroundFloor)
                    {
                        GoToElevator();
                    }
                    else
                    {
                        // Ground floor tenant - go directly
                        visitorState = VisitorState.WalkingToTenant;
                        MoveTo(targetTenant.EntrancePosition);
                    }
                    break;

                case VisitorState.WaitingForElevator:
                    elevatorWaitTimer = 0f;
                    break;

                case VisitorState.ExitingElevator:
                    visitorState = VisitorState.WalkingToTenant;
                    if (targetTenant != null)
                    {
                        MoveTo(targetTenant.EntrancePosition);
                    }
                    break;

                case VisitorState.WalkingToTenant:
                    EnterTenant();
                    break;

                case VisitorState.LeavingTenant:
                    if (targetTenant != null && !targetTenant.IsGroundFloor)
                    {
                        GoToElevatorDown();
                    }
                    else
                    {
                        var lobby = GetTowerLobby();
                        visitorState = VisitorState.ExitingBuilding;
                        if (lobby != null)
                        {
                            MoveTo(lobby.EntrancePosition);
                        }
                        else
                        {
                            MoveTo(spawnPosition);
                        }
                    }
                    break;

                case VisitorState.WaitingForElevatorDown:
                    elevatorWaitTimer = 0f;
                    break;

                case VisitorState.ExitingBuilding:
                    visitorState = VisitorState.Leaving;
                    MoveTo(spawnPosition);
                    break;

                case VisitorState.Leaving:
                    FinishVisit();
                    break;

                default:
                    base.OnReachedDestination();
                    break;
            }
        }

        protected override void Update()
        {
            base.Update();

            switch (visitorState)
            {
                case VisitorState.WaitingForElevator:
                    UpdateElevatorWait(true);
                    break;

                case VisitorState.WaitingForElevatorDown:
                    UpdateElevatorWait(false);
                    break;

                case VisitorState.RidingElevatorUp:
                case VisitorState.RidingElevatorDown:
                    UpdateElevatorRide();
                    break;

                case VisitorState.Browsing:
                    UpdateBrowsing();
                    break;
            }
        }

        private void GoToElevator()
        {
            if (ElevatorManager.Instance == null || targetTenant == null)
            {
                visitorState = VisitorState.WalkingToTenant;
                MoveTo(targetTenant.EntrancePosition);
                return;
            }

            int towerId = targetTenant.TowerId;
            targetShaft = ElevatorManager.Instance.FindShaftServingFloor(0, towerId);

            if (targetShaft == null)
            {
                Debug.LogWarning($"[Visitor {visitorId}] No elevator found");
                LeaveBuilding();
                return;
            }

            visitorState = VisitorState.WaitingForElevator;
            Vector2 waitPos = targetShaft.GetWaitPosition(0);
            MoveTo(waitPos);

            targetShaft.CallToFloor(0, ElevatorDirection.Up, gameObject);
            Debug.Log($"[Visitor {visitorId}] Going to elevator, target floor {targetFloor + 1}F");
        }

        private void GoToElevatorDown()
        {
            if (ElevatorManager.Instance == null || targetShaft == null)
            {
                var lobby = GetTowerLobby();
                visitorState = VisitorState.ExitingBuilding;
                if (lobby != null)
                {
                    MoveTo(lobby.EntrancePosition);
                }
                else
                {
                    MoveTo(spawnPosition);
                }
                return;
            }

            int currentFloor = targetTenant != null ? targetTenant.Floor : 0;

            visitorState = VisitorState.WaitingForElevatorDown;
            Vector2 waitPos = targetShaft.GetWaitPosition(currentFloor);
            MoveTo(waitPos);

            targetShaft.CallToFloor(currentFloor, ElevatorDirection.Down, gameObject);
            Debug.Log($"[Visitor {visitorId}] Going to elevator to go down from {currentFloor + 1}F");
        }

        private void UpdateElevatorWait(bool goingUp)
        {
            elevatorWaitTimer += Time.deltaTime;

            int waitFloor = goingUp ? 0 : (targetTenant != null ? targetTenant.Floor : 0);
            var car = targetShaft?.GetAvailableCarAtFloor(waitFloor);

            if (car != null && !car.IsFull)
            {
                int destination = goingUp ? targetFloor : 0;
                if (car.BoardPassenger(gameObject, destination))
                {
                    currentElevator = car;
                    visitorState = goingUp ? VisitorState.RidingElevatorUp : VisitorState.RidingElevatorDown;
                    Debug.Log($"[Visitor {visitorId}] Boarded elevator");
                }
            }
            else if (elevatorWaitTimer > MAX_ELEVATOR_WAIT)
            {
                Debug.LogWarning($"[Visitor {visitorId}] Elevator timeout, leaving");
                LeaveBuilding();
            }
        }

        private void UpdateElevatorRide()
        {
            if (currentElevator == null)
            {
                Debug.LogWarning($"[Visitor {visitorId}] Lost elevator reference");
                LeaveBuilding();
                return;
            }

            bool goingUp = visitorState == VisitorState.RidingElevatorUp;
            int destination = goingUp ? targetFloor : 0;

            if (currentElevator.CurrentFloor == destination &&
                (currentElevator.State == ElevatorCarState.DoorsOpen || currentElevator.State == ElevatorCarState.Idle))
            {
                if (currentElevator.ShouldPassengerExit(gameObject))
                {
                    Vector2 exitPos = currentElevator.GetExitPosition(destination);
                    currentElevator.ExitPassenger(gameObject);
                    TeleportTo(exitPos);

                    if (goingUp)
                    {
                        visitorState = VisitorState.ExitingElevator;
                    }
                    else
                    {
                        visitorState = VisitorState.ExitingBuilding;
                        var lobby = GetTowerLobby();
                        if (lobby != null)
                        {
                            MoveTo(lobby.EntrancePosition);
                        }
                        else
                        {
                            MoveTo(spawnPosition);
                        }
                    }

                    currentElevator = null;
                    Debug.Log($"[Visitor {visitorId}] Exited elevator at {destination + 1}F");

                    if (goingUp)
                    {
                        OnReachedDestination();
                    }
                }
            }
        }

        private void EnterTenant()
        {
            if (targetTenant == null || targetTenant.IsFull || !targetTenant.IsOpen())
            {
                Debug.Log($"[Visitor {visitorId}] Cannot enter tenant (full or closed), leaving");
                LeaveBuilding();
                return;
            }

            // Get a position inside the tenant
            Vector2? pos = targetTenant.GetAvailablePosition();
            if (pos == null)
            {
                Debug.Log($"[Visitor {visitorId}] No position available in tenant, leaving");
                LeaveBuilding();
                return;
            }

            tenantPosition = pos.Value;
            targetTenant.Enter(gameObject);
            visitorState = VisitorState.Browsing;
            browseTimer = 0f;

            Debug.Log($"[Visitor {visitorId}] Entered {targetTenant.name}, will browse for {browseTime}s");
        }

        private void UpdateBrowsing()
        {
            browseTimer += Time.deltaTime * (GameTimeManager.Instance?.TimeScale ?? 360f) / 360f;

            if (browseTimer >= browseTime)
            {
                LeaveTenant();
            }
        }

        private void LeaveTenant()
        {
            Debug.Log($"[Visitor {visitorId}] Finished browsing, leaving tenant");

            if (targetTenant != null)
            {
                targetTenant.Exit(gameObject);
                targetTenant.ReleasePosition(tenantPosition);
            }

            // Show sprite
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = true;
            }

            visitorState = VisitorState.LeavingTenant;
            OnReachedDestination();
        }

        private void LeaveBuilding()
        {
            // Show sprite if hidden
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = true;
            }

            // If in tenant, exit first
            if (targetTenant != null && visitorState == VisitorState.Browsing)
            {
                targetTenant.Exit(gameObject);
                targetTenant.ReleasePosition(tenantPosition);
            }

            visitorState = VisitorState.ExitingBuilding;
            var lobby = GetTowerLobby();
            if (lobby != null)
            {
                MoveTo(lobby.EntrancePosition);
            }
            else
            {
                MoveTo(spawnPosition);
            }
        }

        private void FinishVisit()
        {
            Debug.Log($"[Visitor {visitorId}] Finished visit, despawning");
            Destroy(gameObject);
        }

        public string GetStatusString()
        {
            return $"Visitor {visitorId} ({visitorType}): {visitorState}";
        }
    }
}
