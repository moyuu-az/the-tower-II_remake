using UnityEngine;
using TowerGame.Core;
using TowerGame.Building;

namespace TowerGame.People
{
    /// <summary>
    /// Employee states for commuting behavior
    /// </summary>
    public enum EmployeeState
    {
        AtHome,              // Off-screen, waiting for work time
        CommutingToWork,     // Walking from spawn to office entrance
        WaitingForElevator,  // Waiting at elevator on ground floor
        RidingElevatorUp,    // Inside elevator going up
        ExitingElevator,     // Exiting elevator on destination floor
        EnteringBuilding,    // Walking from entrance/elevator to work position
        Working,             // At desk, working
        LeavingBuilding,     // Walking from work position to elevator/entrance
        WaitingForElevatorDown, // Waiting at elevator to go down
        RidingElevatorDown,  // Inside elevator going down
        ExitingElevatorDown, // Exiting elevator on ground floor
        CommutingHome        // Walking from entrance to spawn point
    }

    /// <summary>
    /// Employee with daily commuting schedule
    /// </summary>
    public class Employee : Person
    {
        [Header("Employee Settings")]
        [SerializeField] private int employeeId;
        [SerializeField] private int arrivalHour = 8;
        [SerializeField] private int departureHour = 18;

        [Header("References")]
        [SerializeField] private OfficeBuilding assignedOffice;

        [Header("Positions")]
        [SerializeField] private Vector2 homePosition;
        [SerializeField] private Vector2 workPosition;

        [Header("Employee State (Read Only)")]
        [SerializeField] private EmployeeState employeeState = EmployeeState.AtHome;

        // Elevator handling
        private ElevatorCar currentElevator;
        private ElevatorShaft targetShaft;
        private int targetFloor;
        private float elevatorWaitTimer;
        private const float MAX_ELEVATOR_WAIT = 30f;

        // Properties
        public EmployeeState State => employeeState;
        public int EmployeeId => employeeId;
        public bool IsAtWork => employeeState == EmployeeState.Working;

        protected override void Awake()
        {
            base.Awake();
        }

        private void Start()
        {
            // Subscribe to time events
            if (GameTimeManager.Instance != null)
            {
                GameTimeManager.Instance.OnHourChanged += OnHourChanged;
            }

            // Start at home position
            TeleportTo(homePosition);
            employeeState = EmployeeState.AtHome;
            gameObject.SetActive(false); // Hide until commuting
        }

        private void OnDestroy()
        {
            if (GameTimeManager.Instance != null)
            {
                GameTimeManager.Instance.OnHourChanged -= OnHourChanged;
            }
        }

        /// <summary>
        /// Initialize the employee with office and home position
        /// </summary>
        public void Initialize(int id, OfficeBuilding office, Vector2 spawnPoint)
        {
            employeeId = id;
            assignedOffice = office;
            homePosition = spawnPoint;

            // Assign a work position based on employee ID
            if (office != null)
            {
                workPosition = office.GetWorkPosition(id);
            }

            // Add some randomness to arrival/departure times
            arrivalHour = 8; // Fixed for now, could add random offset
            departureHour = 18;

            Debug.Log($"[Employee {id}] Initialized. Home: {homePosition}, Work: {workPosition}");
        }

        private void OnHourChanged(int hour)
        {
            // Check if it's time to go to work
            if (hour == arrivalHour && employeeState == EmployeeState.AtHome)
            {
                StartCommute();
            }
            // Check if it's time to leave work
            else if (hour == departureHour && employeeState == EmployeeState.Working)
            {
                LeaveWork();
            }
        }

        /// <summary>
        /// Start commuting to work
        /// </summary>
        private void StartCommute()
        {
            // Don't commute if no office assigned
            if (assignedOffice == null)
            {
                Debug.Log($"[Employee {employeeId}] No office assigned, staying home");
                return;
            }

            Debug.Log($"[Employee {employeeId}] Starting commute to work");

            gameObject.SetActive(true);
            TeleportTo(homePosition);
            employeeState = EmployeeState.CommutingToWork;

            // If office is on upper floor, go to lobby first
            if (!assignedOffice.IsGroundFloor)
            {
                // Get lobby entrance (ground level)
                var lobby = GetTowerLobby();
                if (lobby != null)
                {
                    MoveTo(lobby.EntrancePosition);
                    Debug.Log($"[Employee {employeeId}] Going to lobby first for upper floor office");
                    return;
                }
            }

            // Ground floor office - go directly to entrance
            MoveTo(assignedOffice.EntrancePosition);
        }

        /// <summary>
        /// Get the lobby for the tower this employee's office is in
        /// </summary>
        private Lobby GetTowerLobby()
        {
            if (FloorSystemManager.Instance == null) return null;
            if (assignedOffice == null) return null;

            // Get the tower ID from the assigned office
            int targetTowerId = assignedOffice.TowerId;

            // Find the tower that matches the office's tower ID
            var towers = FloorSystemManager.Instance.GetAllTowers();
            foreach (var tower in towers)
            {
                if (tower.towerId == targetTowerId && tower.lobby != null)
                {
                    return tower.lobby;
                }
            }

            // Fallback: if no matching tower found, return first available lobby
            Debug.LogWarning($"[Employee {employeeId}] Could not find lobby for tower {targetTowerId}, using fallback");
            foreach (var tower in towers)
            {
                if (tower.lobby != null)
                {
                    return tower.lobby;
                }
            }
            return null;
        }

        /// <summary>
        /// Leave work and go home
        /// </summary>
        private void LeaveWork()
        {
            Debug.Log($"[Employee {employeeId}] Leaving work");

            employeeState = EmployeeState.LeavingBuilding;

            // Exit office
            if (assignedOffice != null)
            {
                assignedOffice.Exit(gameObject);
                assignedOffice.ReleaseWorkPosition(workPosition);
            }

            // Move to entrance first
            MoveTo(assignedOffice.EntrancePosition);
        }

        protected override void OnReachedDestination()
        {
            switch (employeeState)
            {
                case EmployeeState.CommutingToWork:
                    // Arrived at entrance, check if need elevator
                    if (assignedOffice != null && !assignedOffice.IsGroundFloor)
                    {
                        // Need elevator to reach upper floor
                        GoToElevator();
                    }
                    else
                    {
                        // Ground floor office, go directly
                        employeeState = EmployeeState.EnteringBuilding;
                        if (assignedOffice != null)
                        {
                            assignedOffice.Enter(gameObject);
                        }
                        MoveTo(workPosition);
                        Debug.Log($"[Employee {employeeId}] Entering building (1F)");
                    }
                    break;

                case EmployeeState.WaitingForElevator:
                    // Arrived at elevator wait position, now wait for car
                    Debug.Log($"[Employee {employeeId}] Waiting for elevator");
                    elevatorWaitTimer = 0f;
                    break;

                case EmployeeState.ExitingElevator:
                    // Exited elevator, now go to work position
                    employeeState = EmployeeState.EnteringBuilding;
                    if (assignedOffice != null)
                    {
                        assignedOffice.Enter(gameObject);
                    }
                    MoveTo(workPosition);
                    Debug.Log($"[Employee {employeeId}] Exited elevator, heading to office");
                    break;

                case EmployeeState.EnteringBuilding:
                    // Arrived at work position, start working
                    employeeState = EmployeeState.Working;
                    currentState = PersonState.Working;
                    Debug.Log($"[Employee {employeeId}] Started working");
                    break;

                case EmployeeState.LeavingBuilding:
                    // Check if need elevator to exit
                    if (assignedOffice != null && !assignedOffice.IsGroundFloor)
                    {
                        // Need elevator to go down
                        GoToElevatorDown();
                    }
                    else
                    {
                        // Ground floor, go directly home
                        employeeState = EmployeeState.CommutingHome;
                        MoveTo(homePosition);
                        Debug.Log($"[Employee {employeeId}] Leaving building, heading home");
                    }
                    break;

                case EmployeeState.WaitingForElevatorDown:
                    // Arrived at elevator wait position
                    Debug.Log($"[Employee {employeeId}] Waiting for elevator to go down");
                    elevatorWaitTimer = 0f;
                    break;

                case EmployeeState.ExitingElevatorDown:
                    // Exited elevator on ground floor, go home
                    employeeState = EmployeeState.CommutingHome;
                    MoveTo(homePosition);
                    Debug.Log($"[Employee {employeeId}] Exited elevator, heading home");
                    break;

                case EmployeeState.CommutingHome:
                    // Arrived home
                    employeeState = EmployeeState.AtHome;
                    currentState = PersonState.Idle;
                    gameObject.SetActive(false);
                    Debug.Log($"[Employee {employeeId}] Arrived home");
                    break;

                default:
                    base.OnReachedDestination();
                    break;
            }
        }

        protected override void Update()
        {
            // CRITICAL: Call base class movement logic
            base.Update();

            // Handle elevator waiting states
            switch (employeeState)
            {
                case EmployeeState.WaitingForElevator:
                    UpdateElevatorWait(true);
                    break;

                case EmployeeState.WaitingForElevatorDown:
                    UpdateElevatorWait(false);
                    break;

                case EmployeeState.RidingElevatorUp:
                case EmployeeState.RidingElevatorDown:
                    UpdateElevatorRide();
                    break;
            }
        }

        private void GoToElevator()
        {
            if (ElevatorManager.Instance == null)
            {
                // No elevator system - cannot reach upper floors, go home
                Debug.LogWarning($"[Employee {employeeId}] No elevator system available, cannot reach upper floor - going home");
                employeeState = EmployeeState.CommutingHome;
                MoveTo(homePosition);
                return;
            }

            // Find elevator shaft for this tower
            int towerId = GetTowerId();
            int currentFloor = 0; // Employee starts from lobby (ground floor)
            targetFloor = assignedOffice.Floor;

            targetShaft = ElevatorManager.Instance.FindShaftServingFloor(currentFloor, towerId);
            if (targetShaft == null)
            {
                // No elevator shaft found - cannot reach upper floors, go home
                Debug.LogWarning($"[Employee {employeeId}] No elevator shaft found for tower {towerId}, cannot reach floor {targetFloor + 1}F - going home");
                employeeState = EmployeeState.CommutingHome;
                MoveTo(homePosition);
                return;
            }

            // Move to elevator wait position at current floor (lobby)
            employeeState = EmployeeState.WaitingForElevator;
            Vector2 waitPos = targetShaft.GetWaitPosition(currentFloor);
            MoveTo(waitPos);

            // Call elevator to current floor going up
            targetShaft.CallToFloor(currentFloor, ElevatorDirection.Up, gameObject);
            Debug.Log($"[Employee {employeeId}] Going to elevator shaft at segment {targetShaft.SegmentX}, wait pos: {waitPos}, target floor {targetFloor + 1}F");
        }

        /// <summary>
        /// Get the tower ID for this employee's office
        /// </summary>
        private int GetTowerId()
        {
            if (assignedOffice != null)
            {
                return assignedOffice.TowerId;
            }
            // Fallback to tower 0 if no office assigned
            return 0;
        }

        private void GoToElevatorDown()
        {
            if (ElevatorManager.Instance == null || targetShaft == null)
            {
                // No elevator, go directly home
                employeeState = EmployeeState.CommutingHome;
                MoveTo(homePosition);
                return;
            }

            int currentFloor = assignedOffice != null ? assignedOffice.Floor : 0;

            // Move to elevator wait position on current floor
            employeeState = EmployeeState.WaitingForElevatorDown;
            Vector2 waitPos = targetShaft.GetWaitPosition(currentFloor);
            MoveTo(waitPos);

            // Call elevator
            targetShaft.CallToFloor(currentFloor, ElevatorDirection.Down, gameObject);
            Debug.Log($"[Employee {employeeId}] Going to elevator to go down from {currentFloor + 1}F");
        }

        private void UpdateElevatorWait(bool goingUp)
        {
            elevatorWaitTimer += Time.deltaTime;

            // Check if elevator has arrived
            int waitFloor = goingUp ? 0 : (assignedOffice != null ? assignedOffice.Floor : 0);
            var car = targetShaft?.GetAvailableCarAtFloor(waitFloor);

            if (car != null && !car.IsFull)
            {
                // Board elevator
                int destination = goingUp ? targetFloor : 0;
                if (car.BoardPassenger(gameObject, destination))
                {
                    currentElevator = car;
                    employeeState = goingUp ? EmployeeState.RidingElevatorUp : EmployeeState.RidingElevatorDown;
                    Debug.Log($"[Employee {employeeId}] Boarded elevator");
                }
            }
            else if (elevatorWaitTimer > MAX_ELEVATOR_WAIT)
            {
                // Timeout handling
                if (goingUp)
                {
                    // Cannot walk to upper floors - give up and go home
                    Debug.LogWarning($"[Employee {employeeId}] Elevator timeout going UP - cannot reach upper floor, going home");
                    employeeState = EmployeeState.CommutingHome;
                    MoveTo(homePosition);
                }
                else
                {
                    // Going down - can walk out from lobby level
                    // But if on upper floor, continue waiting (cannot teleport down)
                    if (waitFloor == 0)
                    {
                        // Already at ground floor, can leave
                        Debug.LogWarning($"[Employee {employeeId}] Elevator timeout at lobby, walking home");
                        employeeState = EmployeeState.CommutingHome;
                        MoveTo(homePosition);
                    }
                    else
                    {
                        // On upper floor - cannot walk down, keep waiting
                        Debug.LogWarning($"[Employee {employeeId}] Elevator timeout on floor {waitFloor + 1}F - continuing to wait");
                        elevatorWaitTimer = 0f; // Reset timer and keep waiting
                        // Re-call elevator in case it got stuck
                        if (targetShaft != null)
                        {
                            targetShaft.CallToFloor(waitFloor, ElevatorDirection.Down, gameObject);
                        }
                    }
                }
            }
        }

        private void UpdateElevatorRide()
        {
            if (currentElevator == null)
            {
                // Lost elevator reference - emergency fallback
                // Teleport to destination to avoid getting stuck
                Debug.LogWarning($"[Employee {employeeId}] Lost elevator reference during ride!");
                if (employeeState == EmployeeState.RidingElevatorUp)
                {
                    // Teleport to work position (emergency)
                    TeleportTo(workPosition);
                    employeeState = EmployeeState.EnteringBuilding;
                    if (assignedOffice != null)
                    {
                        assignedOffice.Enter(gameObject);
                    }
                    OnReachedDestination();
                }
                else
                {
                    // Teleport home (emergency)
                    TeleportTo(homePosition);
                    employeeState = EmployeeState.CommutingHome;
                    OnReachedDestination();
                }
                return;
            }

            // Check if elevator reached destination
            int destination = employeeState == EmployeeState.RidingElevatorUp ? targetFloor : 0;

            if (currentElevator.CurrentFloor == destination &&
                (currentElevator.State == ElevatorCarState.DoorsOpen || currentElevator.State == ElevatorCarState.Idle))
            {
                // Check if this employee should exit at current floor
                if (currentElevator.ShouldPassengerExit(gameObject))
                {
                    // Get exit position BEFORE exiting (car position is still valid)
                    Vector2 exitPos = currentElevator.GetExitPosition(destination);

                    // Exit elevator - this enables sprite
                    currentElevator.ExitPassenger(gameObject);

                    // TELEPORT to exit position (not walk!) - employee appears at elevator exit
                    TeleportTo(exitPos);

                    if (employeeState == EmployeeState.RidingElevatorUp)
                    {
                        employeeState = EmployeeState.ExitingElevator;
                    }
                    else
                    {
                        employeeState = EmployeeState.ExitingElevatorDown;
                    }

                    currentElevator = null;
                    Debug.Log($"[Employee {employeeId}] Exited elevator at {destination + 1}F, position: {exitPos}");

                    // Immediately trigger destination reached since we teleported
                    OnReachedDestination();
                }
            }
        }

        /// <summary>
        /// Force employee to go home (for debugging or events)
        /// </summary>
        public void ForceGoHome()
        {
            if (employeeState == EmployeeState.Working)
            {
                LeaveWork();
            }
        }

        /// <summary>
        /// Assign a new office to this employee
        /// </summary>
        public void AssignOffice(OfficeBuilding office)
        {
            assignedOffice = office;
            if (office != null)
            {
                workPosition = office.GetWorkPosition(employeeId);
                Debug.Log($"[Employee {employeeId}] Assigned to new office. Work position: {workPosition}");
            }
        }

        /// <summary>
        /// Get a status string for debugging
        /// </summary>
        public string GetStatusString()
        {
            return $"Employee {employeeId}: {employeeState}";
        }
    }
}
