using TowerGame.Core.StateMachine;
using UnityEngine;

namespace TowerGame.People.States
{
    /// <summary>
    /// Base class for all employee states providing common functionality
    /// </summary>
    public abstract class EmployeeStateBase : IState<Employee>
    {
        /// <summary>
        /// Called when entering this state
        /// </summary>
        public virtual void Enter(Employee context)
        {
            // Override in derived classes
        }

        /// <summary>
        /// Called every frame while in this state
        /// </summary>
        public virtual void Update(Employee context)
        {
            // Override in derived classes
        }

        /// <summary>
        /// Called when exiting this state
        /// </summary>
        public virtual void Exit(Employee context)
        {
            // Override in derived classes
        }

        /// <summary>
        /// Called when the employee reaches their movement destination
        /// Override to handle arrival at destination
        /// </summary>
        public virtual void OnReachedDestination(Employee context)
        {
            // Override in derived classes
        }

        /// <summary>
        /// Log a debug message with employee context
        /// </summary>
        protected void Log(Employee context, string message)
        {
            Debug.Log($"[Employee {context.EmployeeId}] {message}");
        }

        /// <summary>
        /// Log a warning message with employee context
        /// </summary>
        protected void LogWarning(Employee context, string message)
        {
            Debug.LogWarning($"[Employee {context.EmployeeId}] {message}");
        }
    }
}
