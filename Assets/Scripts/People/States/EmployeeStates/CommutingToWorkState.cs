using TowerGame.Building;

namespace TowerGame.People.States.EmployeeStates
{
    /// <summary>
    /// State when employee is walking from home to office entrance
    /// </summary>
    public class CommutingToWorkState : EmployeeStateBase
    {
        public override void Enter(Employee context)
        {
            context.TeleportTo(context.HomePosition);

            // Determine destination based on office floor
            if (!context.AssignedOffice.IsGroundFloor)
            {
                // Upper floor office - go to lobby first
                var lobby = context.GetTowerLobby();
                if (lobby != null)
                {
                    context.MoveTo(lobby.EntrancePosition);
                    Log(context, "Commuting to lobby for upper floor office");
                    return;
                }
            }

            // Ground floor office - go directly to entrance
            context.MoveTo(context.AssignedOffice.EntrancePosition);
            Log(context, "Commuting to work (ground floor office)");
        }

        public override void OnReachedDestination(Employee context)
        {
            // Arrived at entrance, check if need elevator
            if (context.AssignedOffice != null && !context.AssignedOffice.IsGroundFloor)
            {
                // Need elevator to reach upper floor
                context.RequestGoToElevator();
            }
            else
            {
                // Ground floor office, enter directly
                context.RequestEnterBuilding();
            }
        }
    }
}
