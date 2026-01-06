namespace TowerGame.People.States.EmployeeStates
{
    /// <summary>
    /// State when employee has exited elevator on ground floor
    /// </summary>
    public class ExitingElevatorDownState : EmployeeStateBase
    {
        public override void Enter(Employee context)
        {
            // Immediately transition to commuting home
            context.MoveTo(context.HomePosition);
            context.ChangeState(EmployeeState.CommutingHome);
            Log(context, "Exited elevator, heading home");
        }

        public override void OnReachedDestination(Employee context)
        {
            // Should not be called, transition happens in Enter
        }
    }
}
