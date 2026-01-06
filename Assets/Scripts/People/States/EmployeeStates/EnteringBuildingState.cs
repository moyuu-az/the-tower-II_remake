namespace TowerGame.People.States.EmployeeStates
{
    /// <summary>
    /// State when employee is walking from entrance/elevator to work position
    /// </summary>
    public class EnteringBuildingState : EmployeeStateBase
    {
        public override void Enter(Employee context)
        {
            Log(context, "Entering building, walking to work position");
        }

        public override void OnReachedDestination(Employee context)
        {
            // Arrived at work position, start working
            context.ChangeState(EmployeeState.Working);
        }
    }
}
