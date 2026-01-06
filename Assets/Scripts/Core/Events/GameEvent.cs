using System;

namespace TowerGame.Core.Events
{
    /// <summary>
    /// Base class for all game events
    /// Enables type-safe, decoupled communication between systems
    /// </summary>
    public abstract class GameEvent
    {
        public DateTime Timestamp { get; } = DateTime.Now;
    }

    #region Time Events

    /// <summary>
    /// Fired when an in-game hour changes
    /// </summary>
    public class HourChangedEvent : GameEvent
    {
        public int Hour { get; }
        public int Day { get; }

        public HourChangedEvent(int hour, int day)
        {
            Hour = hour;
            Day = day;
        }
    }

    /// <summary>
    /// Fired when an in-game day changes
    /// </summary>
    public class DayChangedEvent : GameEvent
    {
        public int Day { get; }

        public DayChangedEvent(int day)
        {
            Day = day;
        }
    }

    /// <summary>
    /// Fired when game time is updated (every frame)
    /// </summary>
    public class TimeUpdatedEvent : GameEvent
    {
        public float CurrentHour { get; }

        public TimeUpdatedEvent(float currentHour)
        {
            CurrentHour = currentHour;
        }
    }

    #endregion

    #region Economy Events

    /// <summary>
    /// Fired when money balance changes
    /// </summary>
    public class MoneyChangedEvent : GameEvent
    {
        public long NewBalance { get; }
        public long Delta { get; }
        public string Reason { get; }

        public MoneyChangedEvent(long newBalance, long delta, string reason)
        {
            NewBalance = newBalance;
            Delta = delta;
            Reason = reason;
        }
    }

    /// <summary>
    /// Fired when rent is collected
    /// </summary>
    public class RentCollectedEvent : GameEvent
    {
        public long TotalRent { get; }
        public int TenantCount { get; }

        public RentCollectedEvent(long totalRent, int tenantCount)
        {
            TotalRent = totalRent;
            TenantCount = tenantCount;
        }
    }

    #endregion

    #region Building Events

    /// <summary>
    /// Fired when a building is placed
    /// </summary>
    public class BuildingPlacedEvent : GameEvent
    {
        public BuildingType Type { get; }
        public int SegmentX { get; }
        public int Floor { get; }
        public int TowerId { get; }

        public BuildingPlacedEvent(BuildingType type, int segmentX, int floor, int towerId)
        {
            Type = type;
            SegmentX = segmentX;
            Floor = floor;
            TowerId = towerId;
        }
    }

    /// <summary>
    /// Fired when a building is demolished
    /// </summary>
    public class BuildingDemolishedEvent : GameEvent
    {
        public BuildingType Type { get; }
        public long RefundAmount { get; }

        public BuildingDemolishedEvent(BuildingType type, long refundAmount)
        {
            Type = type;
            RefundAmount = refundAmount;
        }
    }

    /// <summary>
    /// Building type enum (shared reference)
    /// </summary>
    public enum BuildingType
    {
        None,
        Lobby,
        Floor,
        Office,
        Restaurant,
        Shop,
        Apartment,
        Elevator,
        Demolition
    }

    #endregion

    #region Person Events

    /// <summary>
    /// Fired when a person enters a building
    /// </summary>
    public class PersonEnteredBuildingEvent : GameEvent
    {
        public int PersonId { get; }
        public int TowerId { get; }
        public int Floor { get; }

        public PersonEnteredBuildingEvent(int personId, int towerId, int floor)
        {
            PersonId = personId;
            TowerId = towerId;
            Floor = floor;
        }
    }

    /// <summary>
    /// Fired when a person exits a building
    /// </summary>
    public class PersonExitedBuildingEvent : GameEvent
    {
        public int PersonId { get; }
        public int TowerId { get; }

        public PersonExitedBuildingEvent(int personId, int towerId)
        {
            PersonId = personId;
            TowerId = towerId;
        }
    }

    /// <summary>
    /// Fired when a person's state changes
    /// </summary>
    public class PersonStateChangedEvent : GameEvent
    {
        public int PersonId { get; }
        public string PreviousState { get; }
        public string NewState { get; }

        public PersonStateChangedEvent(int personId, string previousState, string newState)
        {
            PersonId = personId;
            PreviousState = previousState;
            NewState = newState;
        }
    }

    #endregion

    #region Elevator Events

    /// <summary>
    /// Fired when an elevator arrives at a floor
    /// </summary>
    public class ElevatorArrivedEvent : GameEvent
    {
        public int ShaftId { get; }
        public int Floor { get; }
        public int PassengerCount { get; }

        public ElevatorArrivedEvent(int shaftId, int floor, int passengerCount)
        {
            ShaftId = shaftId;
            Floor = floor;
            PassengerCount = passengerCount;
        }
    }

    /// <summary>
    /// Fired when elevator is called
    /// </summary>
    public class ElevatorCalledEvent : GameEvent
    {
        public int Floor { get; }
        public bool GoingUp { get; }

        public ElevatorCalledEvent(int floor, bool goingUp)
        {
            Floor = floor;
            GoingUp = goingUp;
        }
    }

    #endregion

    #region Game State Events

    /// <summary>
    /// Fired when game is paused
    /// </summary>
    public class GamePausedEvent : GameEvent { }

    /// <summary>
    /// Fired when game is resumed
    /// </summary>
    public class GameResumedEvent : GameEvent { }

    /// <summary>
    /// Fired when game speed changes
    /// </summary>
    public class GameSpeedChangedEvent : GameEvent
    {
        public float SpeedMultiplier { get; }

        public GameSpeedChangedEvent(float speedMultiplier)
        {
            SpeedMultiplier = speedMultiplier;
        }
    }

    #endregion
}
