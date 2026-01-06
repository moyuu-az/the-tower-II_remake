using TowerGame.Core.StateMachine;
using TowerGame.People.States.EmployeeStates;

namespace TowerGame.People.States
{
    /// <summary>
    /// State machine implementation for Employee behavior.
    ///
    /// Required methods/properties on Employee class for this state machine to work:
    ///
    /// Properties:
    /// - HomePosition (Vector2) - Position when at home
    /// - WorkPosition (Vector2) - Position when working
    /// - AssignedOffice (OfficeBuilding) - The office building assigned to this employee
    /// - CurrentElevator (ElevatorCar) - The elevator car currently riding
    /// - TargetFloor (int) - Target floor for elevator
    /// - ElevatorWaitTimer (float) - Timer for elevator waiting
    ///
    /// Methods:
    /// - SetVisible(bool visible) - Show/hide the employee GameObject
    /// - SetPersonState(PersonState state) - Set the base person state
    /// - ChangeState(EmployeeState newState) - Change to a new employee state
    /// - GetTowerLobby() - Get the lobby for the assigned office's tower
    /// - RequestGoToElevator() - Start going to elevator (going up)
    /// - RequestGoToElevatorDown() - Start going to elevator (going down)
    /// - RequestEnterBuilding() - Start entering the building
    /// - EnterOffice() - Enter the assigned office
    /// - ExitOffice() - Exit the assigned office
    /// - ResetElevatorWaitTimer() - Reset the elevator wait timer
    /// - IncrementElevatorWaitTimer(float delta) - Increment the elevator wait timer
    /// - GetAvailableElevatorAtFloor(int floor) - Get available elevator car at floor
    /// - TryBoardElevator(ElevatorCar car, int destination) - Try to board an elevator
    /// - ExitElevatorAtFloor(int floor) - Exit elevator at specified floor
    /// - RecallElevator(int floor, ElevatorDirection direction) - Re-call elevator
    /// </summary>
    public class EmployeeStateMachine : StateMachine<EmployeeState, Employee>
    {
        private readonly Employee employee;

        /// <summary>
        /// Create a new employee state machine with all states registered
        /// </summary>
        /// <param name="employee">The employee context</param>
        public EmployeeStateMachine(Employee employee) : base(employee)
        {
            this.employee = employee;

            // Register all employee states
            RegisterState(EmployeeState.AtHome, new AtHomeState());
            RegisterState(EmployeeState.CommutingToWork, new CommutingToWorkState());
            RegisterState(EmployeeState.WaitingForElevator, new WaitingForElevatorState());
            RegisterState(EmployeeState.RidingElevatorUp, new RidingElevatorUpState());
            RegisterState(EmployeeState.ExitingElevator, new ExitingElevatorState());
            RegisterState(EmployeeState.EnteringBuilding, new EnteringBuildingState());
            RegisterState(EmployeeState.Working, new WorkingState());
            RegisterState(EmployeeState.LeavingBuilding, new LeavingBuildingState());
            RegisterState(EmployeeState.WaitingForElevatorDown, new WaitingForElevatorDownState());
            RegisterState(EmployeeState.RidingElevatorDown, new RidingElevatorDownState());
            RegisterState(EmployeeState.ExitingElevatorDown, new ExitingElevatorDownState());
            RegisterState(EmployeeState.CommutingHome, new CommutingHomeState());
        }

        /// <summary>
        /// Handle when the employee reaches their movement destination.
        /// Delegates to the current state's OnReachedDestination method.
        /// </summary>
        public void OnReachedDestination()
        {
            var currentStateInstance = GetCurrentStateInstance();
            if (currentStateInstance is EmployeeStateBase employeeState)
            {
                employeeState.OnReachedDestination(employee);
            }
        }
    }
}
