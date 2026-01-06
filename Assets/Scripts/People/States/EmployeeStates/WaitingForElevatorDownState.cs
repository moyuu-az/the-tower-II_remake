using UnityEngine;
using TowerGame.Building;

namespace TowerGame.People.States.EmployeeStates
{
    /// <summary>
    /// State when employee is waiting at elevator to go down
    /// </summary>
    public class WaitingForElevatorDownState : EmployeeStateBase
    {
        private const float MAX_ELEVATOR_WAIT = 30f;

        public override void Enter(Employee context)
        {
            context.ResetElevatorWaitTimer();
            Log(context, "Waiting for elevator to go down");
        }

        public override void Update(Employee context)
        {
            context.IncrementElevatorWaitTimer(Time.deltaTime);

            // Check if elevator has arrived
            int currentFloor = context.AssignedOffice?.Floor ?? 0;
            var car = context.GetAvailableElevatorAtFloor(currentFloor);

            if (car != null && !car.IsFull)
            {
                // Board elevator to go to ground floor
                if (context.TryBoardElevator(car, 0))
                {
                    context.ChangeState(EmployeeState.RidingElevatorDown);
                    Log(context, "Boarded elevator going down");
                }
            }
            else if (context.ElevatorWaitTimer > MAX_ELEVATOR_WAIT)
            {
                // Timeout handling
                if (currentFloor == 0)
                {
                    // Already at ground floor, can leave
                    LogWarning(context, "Elevator timeout at lobby, walking home");
                    context.MoveTo(context.HomePosition);
                    context.ChangeState(EmployeeState.CommutingHome);
                }
                else
                {
                    // On upper floor - cannot walk down, keep waiting
                    LogWarning(context, $"Elevator timeout on floor {currentFloor + 1}F - continuing to wait");
                    context.ResetElevatorWaitTimer();
                    context.RecallElevator(currentFloor, ElevatorDirection.Down);
                }
            }
        }

        public override void OnReachedDestination(Employee context)
        {
            // Arrived at elevator wait position
            Log(context, "Arrived at elevator wait position (going down)");
        }
    }
}
