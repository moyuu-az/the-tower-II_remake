using System;

namespace TowerGame.Core.Services
{
    /// <summary>
    /// Service interface for game time management.
    /// Provides time progression, day/night cycle, and time-based events.
    /// </summary>
    public interface ITimeService : IService
    {
        /// <summary>
        /// Current hour (0-23.99)
        /// </summary>
        float CurrentHour { get; }

        /// <summary>
        /// Current day number
        /// </summary>
        int CurrentDay { get; }

        /// <summary>
        /// Whether the game is paused
        /// </summary>
        bool IsPaused { get; set; }

        /// <summary>
        /// Time scale multiplier (real seconds to game seconds)
        /// </summary>
        float TimeScale { get; set; }

        /// <summary>
        /// Event fired when hour changes (integer hour value)
        /// </summary>
        event Action<int> OnHourChanged;

        /// <summary>
        /// Event fired when day changes
        /// </summary>
        event Action<int> OnDayChanged;

        /// <summary>
        /// Event fired every frame with current time
        /// </summary>
        event Action<float> OnTimeUpdated;

        /// <summary>
        /// Get formatted time string (HH:MM)
        /// </summary>
        string GetFormattedTime();

        /// <summary>
        /// Get full time string with day
        /// </summary>
        string GetFullTimeString();

        /// <summary>
        /// Check if current time is within working hours (8:00-18:00)
        /// </summary>
        bool IsWorkingHours();

        /// <summary>
        /// Check if current time is commuting time (7:00-8:30)
        /// </summary>
        bool IsCommutingTime();

        /// <summary>
        /// Check if current time is leaving time (17:30-19:00)
        /// </summary>
        bool IsLeavingTime();

        /// <summary>
        /// Set current time (for debugging)
        /// </summary>
        void SetTime(float hour);

        /// <summary>
        /// Skip to a specific hour
        /// </summary>
        void SkipToHour(int targetHour);
    }
}
