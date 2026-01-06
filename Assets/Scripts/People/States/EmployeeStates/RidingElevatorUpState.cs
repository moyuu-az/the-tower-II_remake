using TowerGame.Building;

namespace TowerGame.People.States.EmployeeStates
{
    /// <summary>
    /// State when employee is riding the elevator going up
    /// </summary>
    public class RidingElevatorUpState : EmployeeStateBase
    {
        public override void Enter(Employee context)
        {
            Log(context, "Riding elevator up");
        }

        public override void Update(Employee context)
        {
            var elevator = context.CurrentElevator;

            if (elevator == null)
            {
                // Lost elevator reference - emergency teleport to work
                LogWarning(context, "Lost elevator reference during ride up!");
                context.TeleportTo(context.WorkPosition);
                context.EnterOffice();
                context.ChangeState(EmployeeState.EnteringBuilding);
                return;
            }

            // Check if elevator reached destination
            if (elevator.CurrentFloor == context.TargetFloor &&
                (elevator.State == ElevatorCarState.DoorsOpen || elevator.State == ElevatorCarState.Idle))
            {
                if (elevator.ShouldPassengerExit(context.gameObject))
                {
                    context.ExitElevatorAtFloor(context.TargetFloor);
                    context.ChangeState(EmployeeState.ExitingElevator);
                    Log(context, $"Exited elevator at {context.TargetFloor + 1}F");
                }
            }
        }
    }
}
