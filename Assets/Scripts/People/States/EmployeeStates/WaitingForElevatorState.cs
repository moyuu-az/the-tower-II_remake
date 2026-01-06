using UnityEngine;

namespace TowerGame.People.States.EmployeeStates
{
    /// <summary>
    /// State when employee is waiting at elevator on ground floor to go up
    /// </summary>
    public class WaitingForElevatorState : EmployeeStateBase
    {
        private const float MAX_ELEVATOR_WAIT = 30f;

        public override void Enter(Employee context)
        {
            context.ResetElevatorWaitTimer();
            Log(context, "Waiting for elevator to go up");
        }

        public override void Update(Employee context)
        {
            context.IncrementElevatorWaitTimer(Time.deltaTime);

            // Check if elevator has arrived
            var car = context.GetAvailableElevatorAtFloor(0);
            if (car != null && !car.IsFull)
            {
                // Board elevator
                if (context.TryBoardElevator(car, context.TargetFloor))
                {
                    context.ChangeState(EmployeeState.RidingElevatorUp);
                    Log(context, "Boarded elevator going up");
                }
            }
            else if (context.ElevatorWaitTimer > MAX_ELEVATOR_WAIT)
            {
                // Timeout - cannot reach upper floor, go home
                LogWarning(context, "Elevator timeout going UP - cannot reach upper floor, going home");
                context.ChangeState(EmployeeState.CommutingHome);
            }
        }

        public override void OnReachedDestination(Employee context)
        {
            // Arrived at elevator wait position
            Log(context, "Arrived at elevator wait position");
        }
    }
}
