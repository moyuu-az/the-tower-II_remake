namespace TowerGame.People.States.EmployeeStates
{
    /// <summary>
    /// State when employee is walking from office entrance to home
    /// </summary>
    public class CommutingHomeState : EmployeeStateBase
    {
        public override void Enter(Employee context)
        {
            Log(context, "Commuting home");
        }

        public override void OnReachedDestination(Employee context)
        {
            // Arrived home
            context.ChangeState(EmployeeState.AtHome);
            context.SetPersonState(PersonState.Idle);
            Log(context, "Arrived home");
        }
    }
}
