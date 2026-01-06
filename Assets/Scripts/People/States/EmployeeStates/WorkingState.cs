namespace TowerGame.People.States.EmployeeStates
{
    /// <summary>
    /// State when employee is at work position, working
    /// </summary>
    public class WorkingState : EmployeeStateBase
    {
        public override void Enter(Employee context)
        {
            context.SetPersonState(PersonState.Working);
            Log(context, "Started working");
        }

        public override void Update(Employee context)
        {
            // No update logic - state changes triggered by time event
        }

        public override void Exit(Employee context)
        {
            // Will transition to leaving building when time comes
        }
    }
}
