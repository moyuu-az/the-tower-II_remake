using UnityEngine;
using TowerGame.Core;
using TowerGame.Building;

namespace TowerGame.People
{
    /// <summary>
    /// Resident states for daily life behavior
    /// </summary>
    public enum ResidentState
    {
        AtHome,              // Inside apartment, resting
        WakingUp,            // Preparing to leave
        LeavingHome,         // Walking to elevator/entrance
        WaitingForElevatorDown, // Waiting at elevator to go down
        RidingElevatorDown,  // Inside elevator going down
        ExitingElevator,     // Exiting elevator on ground floor
        LeavingBuilding,     // Walking out of building
        OutsideBuilding,     // Away from building (off-screen)
        ReturningToBuilding, // Walking to building entrance
        EnteringBuilding,    // Walking into lobby
        WaitingForElevatorUp, // Waiting at elevator to go up
        RidingElevatorUp,    // Inside elevator going up
        EnteringHome,        // Walking from elevator to apartment
        Sleeping             // Night time rest
    }

    /// <summary>
    /// Resident who lives in an Apartment and goes out during the day
    /// </summary>
    public class Resident : Person
    {
        [Header("Resident Settings")]
        [SerializeField] private int residentId;
        [SerializeField] private int wakeUpHour = 7;
        [SerializeField] private int leaveHour = 8;
        [SerializeField] private int returnHour = 19;
        [SerializeField] private int sleepHour = 22;

        [Header("References")]
        [SerializeField] private Apartment assignedApartment;
        [SerializeField] private int unitIndex;

        [Header("Positions")]
        [SerializeField] private Vector2 homePosition;
        [SerializeField] private Vector2 outsidePosition;

        [Header("Resident State (Read Only)")]
        [SerializeField] private ResidentState residentState = ResidentState.AtHome;

        // Elevator handling
        private ElevatorCar currentElevator;
        private ElevatorShaft targetShaft;
        private float elevatorWaitTimer;
        private const float MAX_ELEVATOR_WAIT = 30f;

        // Properties
        public ResidentState State => residentState;
        public int ResidentId => residentId;
        public bool IsAtHome => residentState == ResidentState.AtHome || residentState == ResidentState.Sleeping;
        public Apartment AssignedApartment => assignedApartment;

        protected override void Awake()
        {
            base.Awake();
        }

        private void Start()
        {
            if (GameTimeManager.Instance != null)
            {
                GameTimeManager.Instance.OnHourChanged += OnHourChanged;
            }

            // Start at home position (inside apartment)
            TeleportTo(homePosition);
            residentState = ResidentState.AtHome;

            // Hide sprite (inside apartment)
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = false;
            }
        }

        private void OnDestroy()
        {
            if (GameTimeManager.Instance != null)
            {
                GameTimeManager.Instance.OnHourChanged -= OnHourChanged;
            }
        }

        /// <summary>
        /// Initialize the resident with apartment assignment
        /// </summary>
        public void Initialize(int id, Apartment apartment, int unit, Vector2 spawnPoint)
        {
            residentId = id;
            assignedApartment = apartment;
            unitIndex = unit;
            outsidePosition = spawnPoint;

            if (apartment != null)
            {
                homePosition = apartment.GetUnitPosition(unit);
                apartment.RegisterResident(unit);
            }

            // Add randomness to schedule
            wakeUpHour = Random.Range(6, 9);
            leaveHour = wakeUpHour + 1;
            returnHour = Random.Range(18, 21);
            sleepHour = Random.Range(22, 24);

            Debug.Log($"[Resident {id}] Initialized. Apartment unit {unit}, wake: {wakeUpHour}, leave: {leaveHour}, return: {returnHour}");
        }

        private void OnHourChanged(int hour)
        {
            switch (residentState)
            {
                case ResidentState.AtHome:
                case ResidentState.Sleeping:
                    if (hour == wakeUpHour)
                    {
                        WakeUp();
                    }
                    else if (hour == sleepHour)
                    {
                        GoToSleep();
                    }
                    break;

                case ResidentState.WakingUp:
                    if (hour == leaveHour)
                    {
                        LeaveHome();
                    }
                    break;

                case ResidentState.OutsideBuilding:
                    if (hour == returnHour)
                    {
                        ReturnHome();
                    }
                    break;
            }
        }

        private void WakeUp()
        {
            Debug.Log($"[Resident {residentId}] Waking up");
            residentState = ResidentState.WakingUp;
        }

        private void GoToSleep()
        {
            Debug.Log($"[Resident {residentId}] Going to sleep");
            residentState = ResidentState.Sleeping;
        }

        private void LeaveHome()
        {
            if (assignedApartment == null) return;

            Debug.Log($"[Resident {residentId}] Leaving home");
            residentState = ResidentState.LeavingHome;

            // Exit apartment
            assignedApartment.Exit(gameObject);

            // Show sprite
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = true;
            }

            // If apartment is on upper floor, go to elevator
            if (!assignedApartment.IsGroundFloor)
            {
                GoToElevatorDown();
            }
            else
            {
                // Ground floor - go directly outside
                residentState = ResidentState.LeavingBuilding;
                var lobby = GetTowerLobby();
                if (lobby != null)
                {
                    MoveTo(lobby.EntrancePosition);
                }
                else
                {
                    MoveTo(outsidePosition);
                }
            }
        }

        private void ReturnHome()
        {
            Debug.Log($"[Resident {residentId}] Returning home");

            // Show sprite
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = true;
            }

            residentState = ResidentState.ReturningToBuilding;
            TeleportTo(outsidePosition);
            gameObject.SetActive(true);

            // Go to lobby entrance
            var lobby = GetTowerLobby();
            if (lobby != null)
            {
                MoveTo(lobby.EntrancePosition);
            }
            else if (assignedApartment != null)
            {
                MoveTo(assignedApartment.EntrancePosition);
            }
        }

        private Lobby GetTowerLobby()
        {
            if (FloorSystemManager.Instance == null) return null;
            if (assignedApartment == null) return null;

            int targetTowerId = assignedApartment.TowerId;
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
            switch (residentState)
            {
                case ResidentState.LeavingHome:
                case ResidentState.WaitingForElevatorDown:
                    elevatorWaitTimer = 0f;
                    break;

                case ResidentState.ExitingElevator:
                    residentState = ResidentState.LeavingBuilding;
                    MoveTo(outsidePosition);
                    Debug.Log($"[Resident {residentId}] Exited elevator, heading outside");
                    break;

                case ResidentState.LeavingBuilding:
                    residentState = ResidentState.OutsideBuilding;
                    gameObject.SetActive(false);
                    Debug.Log($"[Resident {residentId}] Left building, now outside");
                    break;

                case ResidentState.ReturningToBuilding:
                    if (assignedApartment != null && !assignedApartment.IsGroundFloor)
                    {
                        GoToElevatorUp();
                    }
                    else
                    {
                        residentState = ResidentState.EnteringHome;
                        MoveTo(homePosition);
                    }
                    break;

                case ResidentState.WaitingForElevatorUp:
                    elevatorWaitTimer = 0f;
                    break;

                case ResidentState.EnteringHome:
                    ArriveHome();
                    break;

                default:
                    base.OnReachedDestination();
                    break;
            }
        }

        protected override void Update()
        {
            base.Update();

            switch (residentState)
            {
                case ResidentState.WaitingForElevatorDown:
                    UpdateElevatorWait(false);
                    break;

                case ResidentState.WaitingForElevatorUp:
                    UpdateElevatorWait(true);
                    break;

                case ResidentState.RidingElevatorDown:
                case ResidentState.RidingElevatorUp:
                    UpdateElevatorRide();
                    break;
            }
        }

        private void GoToElevatorDown()
        {
            if (ElevatorManager.Instance == null || assignedApartment == null)
            {
                residentState = ResidentState.LeavingBuilding;
                MoveTo(outsidePosition);
                return;
            }

            int currentFloor = assignedApartment.Floor;
            int towerId = assignedApartment.TowerId;

            targetShaft = ElevatorManager.Instance.FindShaftServingFloor(currentFloor, towerId);
            if (targetShaft == null)
            {
                Debug.LogWarning($"[Resident {residentId}] No elevator found");
                return;
            }

            residentState = ResidentState.WaitingForElevatorDown;
            Vector2 waitPos = targetShaft.GetWaitPosition(currentFloor);
            MoveTo(waitPos);

            targetShaft.CallToFloor(currentFloor, ElevatorDirection.Down, gameObject);
            Debug.Log($"[Resident {residentId}] Going to elevator to go down");
        }

        private void GoToElevatorUp()
        {
            if (ElevatorManager.Instance == null || assignedApartment == null)
            {
                residentState = ResidentState.EnteringHome;
                MoveTo(homePosition);
                return;
            }

            int targetFloor = assignedApartment.Floor;
            int towerId = assignedApartment.TowerId;

            targetShaft = ElevatorManager.Instance.FindShaftServingFloor(0, towerId);
            if (targetShaft == null)
            {
                Debug.LogWarning($"[Resident {residentId}] No elevator found");
                return;
            }

            residentState = ResidentState.WaitingForElevatorUp;
            Vector2 waitPos = targetShaft.GetWaitPosition(0);
            MoveTo(waitPos);

            targetShaft.CallToFloor(0, ElevatorDirection.Up, gameObject);
            Debug.Log($"[Resident {residentId}] Going to elevator to go up to floor {targetFloor + 1}F");
        }

        private void UpdateElevatorWait(bool goingUp)
        {
            elevatorWaitTimer += Time.deltaTime;

            int waitFloor = goingUp ? 0 : (assignedApartment != null ? assignedApartment.Floor : 0);
            var car = targetShaft?.GetAvailableCarAtFloor(waitFloor);

            if (car != null && !car.IsFull)
            {
                int destination = goingUp ? (assignedApartment?.Floor ?? 0) : 0;
                if (car.BoardPassenger(gameObject, destination))
                {
                    currentElevator = car;
                    residentState = goingUp ? ResidentState.RidingElevatorUp : ResidentState.RidingElevatorDown;
                    Debug.Log($"[Resident {residentId}] Boarded elevator");
                }
            }
            else if (elevatorWaitTimer > MAX_ELEVATOR_WAIT)
            {
                Debug.LogWarning($"[Resident {residentId}] Elevator timeout");
                elevatorWaitTimer = 0f;
                if (targetShaft != null)
                {
                    targetShaft.CallToFloor(waitFloor, goingUp ? ElevatorDirection.Up : ElevatorDirection.Down, gameObject);
                }
            }
        }

        private void UpdateElevatorRide()
        {
            if (currentElevator == null)
            {
                Debug.LogWarning($"[Resident {residentId}] Lost elevator reference");
                if (residentState == ResidentState.RidingElevatorUp)
                {
                    TeleportTo(homePosition);
                    ArriveHome();
                }
                else
                {
                    TeleportTo(outsidePosition);
                    residentState = ResidentState.OutsideBuilding;
                    gameObject.SetActive(false);
                }
                return;
            }

            bool goingUp = residentState == ResidentState.RidingElevatorUp;
            int destination = goingUp ? (assignedApartment?.Floor ?? 0) : 0;

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
                        residentState = ResidentState.EnteringHome;
                        MoveTo(homePosition);
                    }
                    else
                    {
                        residentState = ResidentState.ExitingElevator;
                        OnReachedDestination();
                    }

                    currentElevator = null;
                    Debug.Log($"[Resident {residentId}] Exited elevator at {destination + 1}F");
                }
            }
        }

        private void ArriveHome()
        {
            residentState = ResidentState.AtHome;
            currentState = PersonState.Idle;

            if (assignedApartment != null)
            {
                assignedApartment.Enter(gameObject);
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = false;
            }

            Debug.Log($"[Resident {residentId}] Arrived home");
        }

        public string GetStatusString()
        {
            return $"Resident {residentId}: {residentState}";
        }
    }
}
