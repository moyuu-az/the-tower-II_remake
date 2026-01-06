using System;
using System.Collections.Generic;
using UnityEngine;

namespace TowerGame.Core.Events
{
    /// <summary>
    /// Central event bus for decoupled communication between game systems
    /// Replaces tight coupling via direct references and FindObjectOfType
    /// </summary>
    public static class EventBus
    {
        // Dictionary mapping event types to their handlers
        private static readonly Dictionary<Type, List<Delegate>> eventHandlers = new();

        // Queue for events published during iteration (prevents modification during enumeration)
        private static readonly Queue<(Type type, GameEvent evt)> pendingEvents = new();
        private static bool isPublishing = false;

        #region Subscribe/Unsubscribe

        /// <summary>
        /// Subscribe to an event type
        /// </summary>
        /// <typeparam name="T">Event type to subscribe to</typeparam>
        /// <param name="handler">Handler to call when event is published</param>
        public static void Subscribe<T>(Action<T> handler) where T : GameEvent
        {
            var type = typeof(T);

            if (!eventHandlers.ContainsKey(type))
            {
                eventHandlers[type] = new List<Delegate>();
            }

            if (!eventHandlers[type].Contains(handler))
            {
                eventHandlers[type].Add(handler);
            }
        }

        /// <summary>
        /// Unsubscribe from an event type
        /// </summary>
        /// <typeparam name="T">Event type to unsubscribe from</typeparam>
        /// <param name="handler">Handler to remove</param>
        public static void Unsubscribe<T>(Action<T> handler) where T : GameEvent
        {
            var type = typeof(T);

            if (eventHandlers.ContainsKey(type))
            {
                eventHandlers[type].Remove(handler);
            }
        }

        #endregion

        #region Publish

        /// <summary>
        /// Publish an event to all subscribers
        /// </summary>
        /// <typeparam name="T">Event type</typeparam>
        /// <param name="evt">Event instance</param>
        public static void Publish<T>(T evt) where T : GameEvent
        {
            var type = typeof(T);

            // If we're already publishing, queue this event for later
            if (isPublishing)
            {
                pendingEvents.Enqueue((type, evt));
                return;
            }

            PublishInternal(type, evt);

            // Process any events that were queued during publishing
            ProcessPendingEvents();
        }

        private static void PublishInternal(Type type, GameEvent evt)
        {
            if (!eventHandlers.ContainsKey(type))
            {
                return;
            }

            isPublishing = true;

            // Create a copy to avoid modification during iteration
            var handlers = new List<Delegate>(eventHandlers[type]);

            foreach (var handler in handlers)
            {
                try
                {
                    handler.DynamicInvoke(evt);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[EventBus] Error in event handler for {type.Name}: {ex.Message}\n{ex.StackTrace}");
                }
            }

            isPublishing = false;
        }

        private static void ProcessPendingEvents()
        {
            while (pendingEvents.Count > 0)
            {
                var (type, evt) = pendingEvents.Dequeue();
                PublishInternal(type, evt);
            }
        }

        #endregion

        #region Utility

        /// <summary>
        /// Clear all event subscriptions
        /// Call this on scene unload to prevent memory leaks
        /// </summary>
        public static void Clear()
        {
            eventHandlers.Clear();
            pendingEvents.Clear();
            isPublishing = false;
            Debug.Log("[EventBus] All subscriptions cleared");
        }

        /// <summary>
        /// Get the number of subscribers for an event type (for debugging)
        /// </summary>
        public static int GetSubscriberCount<T>() where T : GameEvent
        {
            var type = typeof(T);
            return eventHandlers.ContainsKey(type) ? eventHandlers[type].Count : 0;
        }

        /// <summary>
        /// Check if there are any subscribers for an event type
        /// </summary>
        public static bool HasSubscribers<T>() where T : GameEvent
        {
            return GetSubscriberCount<T>() > 0;
        }

        #endregion
    }

    /// <summary>
    /// MonoBehaviour helper for automatic event cleanup
    /// Inherit from this instead of MonoBehaviour for automatic unsubscription
    /// </summary>
    public abstract class EventSubscriber : MonoBehaviour
    {
        private readonly List<Action> unsubscribeActions = new();

        /// <summary>
        /// Subscribe to an event with automatic cleanup on destroy
        /// </summary>
        protected void SubscribeEvent<T>(Action<T> handler) where T : GameEvent
        {
            EventBus.Subscribe(handler);
            unsubscribeActions.Add(() => EventBus.Unsubscribe(handler));
        }

        protected virtual void OnDestroy()
        {
            foreach (var unsubscribe in unsubscribeActions)
            {
                unsubscribe();
            }
            unsubscribeActions.Clear();
        }
    }
}
