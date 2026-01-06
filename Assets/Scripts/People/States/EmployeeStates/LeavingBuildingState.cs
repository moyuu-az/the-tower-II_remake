namespace TowerGame.People.States.EmployeeStates
{
    /// <summary>
    /// State when employee is walking from work position to elevator/entrance
    /// </summary>
    public class LeavingBuildingState : EmployeeStateBase
    {
        public override void Enter(Employee context)
        {
            context.ExitOffice();
            context.MoveTo(context.AssignedOffice.EntrancePosition);
            Log(context, "Leaving building");
        }

        public override void OnReachedDestination(Employee context)
        {
            // Check if need elevator to exit
            if (context.AssignedOffice != null && !context.AssignedOffice.IsGroundFloor)
            {
                // Need elevator to go down
                context.RequestGoToElevatorDown();
            }
            else
            {
                // Ground floor, go directly home
                context.MoveTo(context.HomePosition);
                context.ChangeState(EmployeeState.CommutingHome);
            }
        }
    }
}
