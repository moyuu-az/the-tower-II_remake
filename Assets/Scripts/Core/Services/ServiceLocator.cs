using System;
using System.Collections.Generic;
using UnityEngine;

namespace TowerGame.Core.Services
{
    /// <summary>
    /// ServiceLocator pattern implementation as an alternative to singletons.
    /// Provides type-safe service registration and retrieval with clear error handling.
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, IService> services = new Dictionary<Type, IService>();
        private static bool isInitialized;

        /// <summary>
        /// Event fired when a service is registered
        /// </summary>
        public static event Action<Type> OnServiceRegistered;

        /// <summary>
        /// Event fired when a service is unregistered
        /// </summary>
        public static event Action<Type> OnServiceUnregistered;

        /// <summary>
        /// Check if ServiceLocator has been initialized
        /// </summary>
        public static bool IsInitialized => isInitialized;

        /// <summary>
        /// Get the number of registered services
        /// </summary>
        public static int ServiceCount => services.Count;

        /// <summary>
        /// Register a service with the ServiceLocator.
        /// </summary>
        /// <typeparam name="T">The service interface type</typeparam>
        /// <param name="service">The service implementation</param>
        /// <param name="initialize">Whether to call Initialize() on the service</param>
        /// <exception cref="ArgumentNullException">Thrown when service is null</exception>
        /// <exception cref="InvalidOperationException">Thrown when service type is already registered</exception>
        public static void Register<T>(T service, bool initialize = true) where T : class, IService
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service), $"Cannot register null service for type {typeof(T).Name}");
            }

            Type type = typeof(T);

            if (services.ContainsKey(type))
            {
                throw new InvalidOperationException(
                    $"[ServiceLocator] Service of type {type.Name} is already registered. " +
                    "Unregister the existing service first or use TryRegister.");
            }

            services[type] = service;
            isInitialized = true;

            if (initialize)
            {
                try
                {
                    service.Initialize();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[ServiceLocator] Failed to initialize service {type.Name}: {ex.Message}");
                    services.Remove(type);
                    throw;
                }
            }

            Debug.Log($"[ServiceLocator] Registered service: {type.Name}");
            OnServiceRegistered?.Invoke(type);
        }

        /// <summary>
        /// Try to register a service. Returns false if already registered.
        /// </summary>
        /// <typeparam name="T">The service interface type</typeparam>
        /// <param name="service">The service implementation</param>
        /// <param name="initialize">Whether to call Initialize() on the service</param>
        /// <returns>True if registration succeeded, false if already registered</returns>
        public static bool TryRegister<T>(T service, bool initialize = true) where T : class, IService
        {
            if (service == null || services.ContainsKey(typeof(T)))
            {
                return false;
            }

            try
            {
                Register(service, initialize);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get a registered service.
        /// </summary>
        /// <typeparam name="T">The service interface type</typeparam>
        /// <returns>The registered service</returns>
        /// <exception cref="InvalidOperationException">Thrown when service is not registered</exception>
        public static T Get<T>() where T : class, IService
        {
            Type type = typeof(T);

            if (!services.TryGetValue(type, out IService service))
            {
                throw new InvalidOperationException(
                    $"[ServiceLocator] Service of type {type.Name} is not registered. " +
                    "Make sure the service is registered before accessing it.");
            }

            return service as T;
        }

        /// <summary>
        /// Try to get a registered service.
        /// </summary>
        /// <typeparam name="T">The service interface type</typeparam>
        /// <param name="service">The output service if found</param>
        /// <returns>True if service was found, false otherwise</returns>
        public static bool TryGet<T>(out T service) where T : class, IService
        {
            Type type = typeof(T);

            if (services.TryGetValue(type, out IService foundService))
            {
                service = foundService as T;
                return service != null;
            }

            service = null;
            return false;
        }

        /// <summary>
        /// Check if a service is registered.
        /// </summary>
        /// <typeparam name="T">The service interface type</typeparam>
        /// <returns>True if the service is registered</returns>
        public static bool IsRegistered<T>() where T : class, IService
        {
            return services.ContainsKey(typeof(T));
        }

        /// <summary>
        /// Unregister a service.
        /// </summary>
        /// <typeparam name="T">The service interface type</typeparam>
        /// <param name="shutdown">Whether to call Shutdown() on the service</param>
        /// <returns>True if unregistration succeeded</returns>
        public static bool Unregister<T>(bool shutdown = true) where T : class, IService
        {
            Type type = typeof(T);

            if (!services.TryGetValue(type, out IService service))
            {
                Debug.LogWarning($"[ServiceLocator] Attempted to unregister non-existent service: {type.Name}");
                return false;
            }

            if (shutdown)
            {
                try
                {
                    service.Shutdown();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[ServiceLocator] Error during service shutdown {type.Name}: {ex.Message}");
                }
            }

            services.Remove(type);
            Debug.Log($"[ServiceLocator] Unregistered service: {type.Name}");
            OnServiceUnregistered?.Invoke(type);

            return true;
        }

        /// <summary>
        /// Clear all registered services. Typically called during scene transitions.
        /// </summary>
        /// <param name="shutdownServices">Whether to call Shutdown() on all services</param>
        public static void Clear(bool shutdownServices = true)
        {
            if (shutdownServices)
            {
                foreach (var kvp in services)
                {
                    try
                    {
                        kvp.Value.Shutdown();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[ServiceLocator] Error during shutdown of {kvp.Key.Name}: {ex.Message}");
                    }
                }
            }

            int count = services.Count;
            services.Clear();
            isInitialized = false;

            Debug.Log($"[ServiceLocator] Cleared {count} services");
        }

        /// <summary>
        /// Get all registered service types (for debugging).
        /// </summary>
        /// <returns>Array of registered service types</returns>
        public static Type[] GetRegisteredTypes()
        {
            Type[] types = new Type[services.Count];
            services.Keys.CopyTo(types, 0);
            return types;
        }

        /// <summary>
        /// Log all registered services (for debugging).
        /// </summary>
        public static void LogRegisteredServices()
        {
            Debug.Log($"[ServiceLocator] Registered services ({services.Count}):");
            foreach (var kvp in services)
            {
                Debug.Log($"  - {kvp.Key.Name}: {kvp.Value.GetType().Name}");
            }
        }
    }
}
