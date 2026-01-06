using System;
using UnityEngine;

namespace TowerGame.Core.Services
{
    /// <summary>
    /// Wrapper class that adapts GameTimeManager to ITimeService interface.
    /// </summary>
    public class TimeServiceWrapper : ITimeService
    {
        private readonly GameTimeManager manager;

        public TimeServiceWrapper(GameTimeManager manager)
        {
            this.manager = manager ?? throw new ArgumentNullException(nameof(manager));
        }

        #region IService Implementation

        public void Initialize()
        {
            Debug.Log("[TimeServiceWrapper] Initialized");
        }

        public void Shutdown()
        {
            Debug.Log("[TimeServiceWrapper] Shutdown");
        }

        #endregion

        #region ITimeService Implementation

        public float CurrentHour => manager.CurrentHour;
        public int CurrentDay => manager.CurrentDay;

        public bool IsPaused
        {
            get => manager.IsPaused;
            set => manager.IsPaused = value;
        }

        public float TimeScale
        {
            get => manager.TimeScale;
            set => manager.TimeScale = value;
        }

        public event Action<int> OnHourChanged
        {
            add => manager.OnHourChanged += value;
            remove => manager.OnHourChanged -= value;
        }

        public event Action<int> OnDayChanged
        {
            add => manager.OnDayChanged += value;
            remove => manager.OnDayChanged -= value;
        }

        public event Action<float> OnTimeUpdated
        {
            add => manager.OnTimeUpdated += value;
            remove => manager.OnTimeUpdated -= value;
        }

        public string GetFormattedTime() => manager.GetFormattedTime();
        public string GetFullTimeString() => manager.GetFullTimeString();
        public bool IsWorkingHours() => manager.IsWorkingHours();
        public bool IsCommutingTime() => manager.IsCommutingTime();
        public bool IsLeavingTime() => manager.IsLeavingTime();
        public void SetTime(float hour) => manager.SetTime(hour);
        public void SkipToHour(int targetHour) => manager.SkipToHour(targetHour);

        #endregion
    }
}
