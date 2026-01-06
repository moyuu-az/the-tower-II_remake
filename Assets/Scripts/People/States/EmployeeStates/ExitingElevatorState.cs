namespace TowerGame.People.States.EmployeeStates
{
    /// <summary>
    /// State when employee has exited elevator on destination floor
    /// </summary>
    public class ExitingElevatorState : EmployeeStateBase
    {
        public override void Enter(Employee context)
        {
            Log(context, "Exited elevator, heading to office");
        }

        public override void OnReachedDestination(Employee context)
        {
            // Transition to entering building
            context.EnterOffice();
            context.MoveTo(context.WorkPosition);
            context.ChangeState(EmployeeState.EnteringBuilding);
        }
    }
}
