namespace TowerGame.People.States.EmployeeStates
{
    /// <summary>
    /// State when employee is at home, waiting for work time
    /// </summary>
    public class AtHomeState : EmployeeStateBase
    {
        public override void Enter(Employee context)
        {
            context.TeleportTo(context.HomePosition);
            context.SetVisible(false);
            Log(context, "At home, waiting for work time");
        }

        public override void Update(Employee context)
        {
            // No update logic - state changes triggered by time event
        }

        public override void Exit(Employee context)
        {
            context.SetVisible(true);
        }
    }
}
