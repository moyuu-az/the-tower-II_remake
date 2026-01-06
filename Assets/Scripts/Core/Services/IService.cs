namespace TowerGame.Core.Services
{
    /// <summary>
    /// Base interface for all services managed by the ServiceLocator.
    /// All services must implement this interface to be registered.
    /// </summary>
    public interface IService
    {
        /// <summary>
        /// Initialize the service. Called after registration.
        /// </summary>
        void Initialize();

        /// <summary>
        /// Shutdown the service. Called before unregistration.
        /// </summary>
        void Shutdown();
    }
}
