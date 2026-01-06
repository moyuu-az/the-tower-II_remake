using UnityEngine;
using TowerGame.Grid;
using TowerGame.Economy;
using TowerGame.Building;

namespace TowerGame.Core.Services
{
    /// <summary>
    /// Bootstrap MonoBehaviour that registers all services at scene start.
    /// Uses [DefaultExecutionOrder(-100)] to ensure execution before other scripts.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class ServiceBootstrap : MonoBehaviour
    {
        [Header("Service References (Auto-discovered if null)")]
        [SerializeField] private GameTimeManager timeManager;
        [SerializeField] private EconomyManager economyManager;
        [SerializeField] private GridManager gridManager;
        [SerializeField] private FloorSystemManager floorSystemManager;

        [Header("Settings")]
        [SerializeField] private bool logRegistrations = true;

        private void Awake()
        {
            // Auto-discover managers if not assigned
            DiscoverManagers();

            // Register all services
            RegisterServices();

            if (logRegistrations)
            {
                ServiceLocator.LogRegisteredServices();
            }
        }

        private void OnDestroy()
        {
            // Clear all services on scene unload
            ServiceLocator.Clear(shutdownServices: true);

            if (logRegistrations)
            {
                Debug.Log("[ServiceBootstrap] All services cleared on destroy");
            }
        }

        /// <summary>
        /// Auto-discover manager components if not serialized
        /// </summary>
        private void DiscoverManagers()
        {
            if (timeManager == null)
            {
                timeManager = FindObjectOfType<GameTimeManager>();
            }

            if (economyManager == null)
            {
                economyManager = FindObjectOfType<EconomyManager>();
            }

            if (gridManager == null)
            {
                gridManager = FindObjectOfType<GridManager>();
            }

            if (floorSystemManager == null)
            {
                floorSystemManager = FindObjectOfType<FloorSystemManager>();
            }
        }

        /// <summary>
        /// Register all discovered managers as services
        /// </summary>
        private void RegisterServices()
        {
            // Register TimeService
            if (timeManager != null)
            {
                var timeService = new TimeServiceWrapper(timeManager);
                ServiceLocator.TryRegister<ITimeService>(timeService, initialize: true);
            }
            else
            {
                Debug.LogWarning("[ServiceBootstrap] GameTimeManager not found, ITimeService not registered");
            }

            // Register EconomyService
            if (economyManager != null)
            {
                var economyService = new EconomyServiceWrapper(economyManager);
                ServiceLocator.TryRegister<IEconomyService>(economyService, initialize: true);
            }
            else
            {
                Debug.LogWarning("[ServiceBootstrap] EconomyManager not found, IEconomyService not registered");
            }

            // Register GridService
            if (gridManager != null)
            {
                var gridService = new GridServiceWrapper(gridManager);
                ServiceLocator.TryRegister<IGridService>(gridService, initialize: true);
            }
            else
            {
                Debug.LogWarning("[ServiceBootstrap] GridManager not found, IGridService not registered");
            }

            // Register BuildingService
            if (floorSystemManager != null)
            {
                var buildingService = new BuildingServiceWrapper(floorSystemManager);
                ServiceLocator.TryRegister<IBuildingService>(buildingService, initialize: true);
            }
            else
            {
                Debug.LogWarning("[ServiceBootstrap] FloorSystemManager not found, IBuildingService not registered");
            }
        }

        /// <summary>
        /// Force re-registration of all services (for testing)
        /// </summary>
        public void ReregisterServices()
        {
            ServiceLocator.Clear(shutdownServices: true);
            DiscoverManagers();
            RegisterServices();
        }
    }
}
