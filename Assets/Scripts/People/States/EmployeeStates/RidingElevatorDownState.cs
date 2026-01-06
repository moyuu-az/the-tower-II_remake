using TowerGame.Building;

namespace TowerGame.People.States.EmployeeStates
{
    /// <summary>
    /// State when employee is riding the elevator going down
    /// </summary>
    public class RidingElevatorDownState : EmployeeStateBase
    {
        public override void Enter(Employee context)
        {
            Log(context, "Riding elevator down");
        }

        public override void Update(Employee context)
        {
            var elevator = context.CurrentElevator;

            if (elevator == null)
            {
                // Lost elevator reference - emergency teleport home
                LogWarning(context, "Lost elevator reference during ride down!");
                context.TeleportTo(context.HomePosition);
                context.ChangeState(EmployeeState.CommutingHome);
                return;
            }

            // Check if elevator reached ground floor
            if (elevator.CurrentFloor == 0 &&
                (elevator.State == ElevatorCarState.DoorsOpen || elevator.State == ElevatorCarState.Idle))
            {
                if (elevator.ShouldPassengerExit(context.gameObject))
                {
                    context.ExitElevatorAtFloor(0);
                    context.ChangeState(EmployeeState.ExitingElevatorDown);
                    Log(context, "Exited elevator at 1F");
                }
            }
        }
    }
}
