namespace TowerGame.Core.StateMachine
{
    /// <summary>
    /// Generic state interface for state machine pattern
    /// </summary>
    /// <typeparam name="TContext">The context type that the state operates on</typeparam>
    public interface IState<TContext>
    {
        /// <summary>
        /// Called when entering this state
        /// </summary>
        /// <param name="context">The context object</param>
        void Enter(TContext context);

        /// <summary>
        /// Called every frame while in this state
        /// </summary>
        /// <param name="context">The context object</param>
        void Update(TContext context);

        /// <summary>
        /// Called when exiting this state
        /// </summary>
        /// <param name="context">The context object</param>
        void Exit(TContext context);
    }
}
