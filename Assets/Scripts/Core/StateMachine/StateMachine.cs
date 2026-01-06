using System;
using System.Collections.Generic;
using UnityEngine;

namespace TowerGame.Core.StateMachine
{
    /// <summary>
    /// Generic state machine implementation
    /// </summary>
    /// <typeparam name="TState">Enum type representing states</typeparam>
    /// <typeparam name="TContext">Context type that states operate on</typeparam>
    public class StateMachine<TState, TContext> where TState : Enum
    {
        private readonly Dictionary<TState, IState<TContext>> states = new Dictionary<TState, IState<TContext>>();
        private TState currentStateType;
        private IState<TContext> currentState;
        private readonly TContext context;
        private bool isInitialized;

        /// <summary>
        /// Current state type
        /// </summary>
        public TState CurrentState => currentStateType;

        /// <summary>
        /// Whether the state machine has been initialized with an initial state
        /// </summary>
        public bool IsInitialized => isInitialized;

        /// <summary>
        /// Event fired when state changes
        /// </summary>
        public event Action<TState, TState> OnStateChanged;

        /// <summary>
        /// Create a new state machine with the given context
        /// </summary>
        /// <param name="context">The context object that states will operate on</param>
        public StateMachine(TContext context)
        {
            this.context = context;
        }

        /// <summary>
        /// Register a state implementation for a given state type
        /// </summary>
        /// <param name="stateType">The state enum value</param>
        /// <param name="state">The state implementation</param>
        public void RegisterState(TState stateType, IState<TContext> state)
        {
            if (states.ContainsKey(stateType))
            {
                Debug.LogWarning($"[StateMachine] State {stateType} already registered, overwriting");
            }
            states[stateType] = state;
        }

        /// <summary>
        /// Set the initial state without calling Exit on any previous state
        /// </summary>
        /// <param name="initialState">The initial state to set</param>
        public void Initialize(TState initialState)
        {
            if (!states.TryGetValue(initialState, out var state))
            {
                Debug.LogError($"[StateMachine] Cannot initialize: State {initialState} not registered");
                return;
            }

            currentStateType = initialState;
            currentState = state;
            isInitialized = true;
            currentState.Enter(context);
        }

        /// <summary>
        /// Change to a new state, calling Exit on current and Enter on new
        /// </summary>
        /// <param name="newState">The state to change to</param>
        public void ChangeState(TState newState)
        {
            if (!states.TryGetValue(newState, out var state))
            {
                Debug.LogError($"[StateMachine] Cannot change state: State {newState} not registered");
                return;
            }

            // Skip if already in this state
            if (isInitialized && EqualityComparer<TState>.Default.Equals(currentStateType, newState))
            {
                return;
            }

            TState previousState = currentStateType;

            // Exit current state
            currentState?.Exit(context);

            // Update state
            currentStateType = newState;
            currentState = state;
            isInitialized = true;

            // Enter new state
            currentState.Enter(context);

            // Fire event
            OnStateChanged?.Invoke(previousState, newState);
        }

        /// <summary>
        /// Update the current state. Should be called every frame.
        /// </summary>
        public void Update()
        {
            if (!isInitialized)
            {
                return;
            }

            currentState?.Update(context);
        }

        /// <summary>
        /// Check if a state is registered
        /// </summary>
        /// <param name="stateType">The state type to check</param>
        /// <returns>True if the state is registered</returns>
        public bool HasState(TState stateType)
        {
            return states.ContainsKey(stateType);
        }

        /// <summary>
        /// Get the current state implementation
        /// </summary>
        /// <returns>The current state or null if not initialized</returns>
        public IState<TContext> GetCurrentStateInstance()
        {
            return currentState;
        }
    }
}
