using System;
using UnityEngine;
using TowerGame.Core;

namespace TowerGame.Economy
{
    /// <summary>
    /// Transaction record for economy history
    /// </summary>
    [System.Serializable]
    public class Transaction
    {
        public long amount;
        public string reason;
        public string timestamp;
        public bool isIncome;

        public Transaction(long amount, string reason, bool isIncome)
        {
            this.amount = amount;
            this.reason = reason;
            this.isIncome = isIncome;
            this.timestamp = DateTime.Now.ToString("HH:mm:ss");
        }
    }

    /// <summary>
    /// Central manager for all economic activities in the tower
    /// Handles money, transactions, rent collection, and building costs
    /// </summary>
    public class EconomyManager : MonoBehaviour
    {
        public static EconomyManager Instance { get; private set; }

        [Header("Initial Settings")]
        [SerializeField] private long startingMoney = 1000000; // 100万円スタート

        [Header("Building Costs")]
        [SerializeField] private BuildingCosts buildingCosts;

        [Header("Runtime State (Read Only)")]
        [SerializeField] private long currentMoney;

        // Transaction history (limited to recent transactions)
        private const int MaxTransactionHistory = 100;
        [SerializeField] private System.Collections.Generic.List<Transaction> transactionHistory = new System.Collections.Generic.List<Transaction>();

        // Events
        public event Action<long> OnMoneyChanged;
        public event Action<long, string, bool> OnTransaction; // amount, reason, isIncome

        // Properties
        public long CurrentMoney => currentMoney;
        public BuildingCosts Costs => buildingCosts;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Initialize money
            currentMoney = startingMoney;
        }

        private void Start()
        {
            // Auto-create BuildingCosts if not assigned
            if (buildingCosts == null)
            {
                buildingCosts = ScriptableObject.CreateInstance<BuildingCosts>();
                Debug.LogWarning("[EconomyManager] BuildingCosts not assigned, using default values");
            }

            // Subscribe to day change for rent collection
            if (GameTimeManager.Instance != null)
            {
                GameTimeManager.Instance.OnDayChanged += OnDayChanged;
            }

            Debug.Log($"[EconomyManager] Initialized with ¥{currentMoney:N0}");
        }

        private void OnDestroy()
        {
            if (GameTimeManager.Instance != null)
            {
                GameTimeManager.Instance.OnDayChanged -= OnDayChanged;
            }
        }

        #region Money Operations

        /// <summary>
        /// Check if the player can afford a purchase
        /// </summary>
        public bool CanAfford(long amount)
        {
            return currentMoney >= amount;
        }

        /// <summary>
        /// Try to spend money. Returns true if successful.
        /// </summary>
        public bool TrySpend(long amount, string reason)
        {
            if (amount <= 0)
            {
                Debug.LogWarning($"[EconomyManager] Invalid spend amount: {amount}");
                return false;
            }

            if (!CanAfford(amount))
            {
                Debug.Log($"[EconomyManager] Cannot afford ¥{amount:N0} for {reason}. Current: ¥{currentMoney:N0}");
                return false;
            }

            currentMoney -= amount;
            RecordTransaction(amount, reason, false);

            Debug.Log($"[EconomyManager] Spent ¥{amount:N0} for {reason}. Remaining: ¥{currentMoney:N0}");
            OnMoneyChanged?.Invoke(currentMoney);
            OnTransaction?.Invoke(amount, reason, false);

            return true;
        }

        /// <summary>
        /// Add money (income, rent, etc.)
        /// </summary>
        public void Earn(long amount, string reason)
        {
            if (amount <= 0)
            {
                Debug.LogWarning($"[EconomyManager] Invalid earn amount: {amount}");
                return;
            }

            currentMoney += amount;
            RecordTransaction(amount, reason, true);

            Debug.Log($"[EconomyManager] Earned ¥{amount:N0} from {reason}. Total: ¥{currentMoney:N0}");
            OnMoneyChanged?.Invoke(currentMoney);
            OnTransaction?.Invoke(amount, reason, true);
        }

        /// <summary>
        /// Force set money (for debugging/testing)
        /// </summary>
        public void SetMoney(long amount)
        {
            currentMoney = System.Math.Max(0L, amount);
            OnMoneyChanged?.Invoke(currentMoney);
            Debug.Log($"[EconomyManager] Money set to ¥{currentMoney:N0}");
        }

        private void RecordTransaction(long amount, string reason, bool isIncome)
        {
            transactionHistory.Add(new Transaction(amount, reason, isIncome));

            // Keep history limited
            while (transactionHistory.Count > MaxTransactionHistory)
            {
                transactionHistory.RemoveAt(0);
            }
        }

        #endregion

        #region Building Costs

        /// <summary>
        /// Get the cost for a building type
        /// </summary>
        public long GetBuildingCost(Building.BuildingType type)
        {
            if (buildingCosts == null) return 0;

            switch (type)
            {
                case Building.BuildingType.Lobby:
                    return buildingCosts.lobbyCost;
                case Building.BuildingType.Floor:
                    return buildingCosts.floorCost;
                case Building.BuildingType.Office:
                    return buildingCosts.officeCost;
                case Building.BuildingType.Elevator:
                    return buildingCosts.elevatorCost;
                // Extended types (will be added)
                default:
                    return 0;
            }
        }

        /// <summary>
        /// Get the cost for a tenant type
        /// </summary>
        public long GetTenantCost(TenantType type)
        {
            if (buildingCosts == null) return 0;

            switch (type)
            {
                case TenantType.Office:
                    return buildingCosts.officeCost;
                case TenantType.Restaurant:
                    return buildingCosts.restaurantCost;
                case TenantType.Shop:
                    return buildingCosts.shopCost;
                case TenantType.Apartment:
                    return buildingCosts.apartmentCost;
                default:
                    return 0;
            }
        }

        /// <summary>
        /// Get the daily rent for a tenant type
        /// </summary>
        public long GetDailyRent(TenantType type)
        {
            if (buildingCosts == null) return 0;

            switch (type)
            {
                case TenantType.Office:
                    return buildingCosts.officeRentPerDay;
                case TenantType.Restaurant:
                    return buildingCosts.restaurantRentPerDay;
                case TenantType.Shop:
                    return buildingCosts.shopRentPerDay;
                case TenantType.Apartment:
                    return buildingCosts.apartmentRentPerDay;
                default:
                    return 0;
            }
        }

        /// <summary>
        /// Calculate demolition refund for a building
        /// </summary>
        public long GetDemolitionRefund(long originalCost)
        {
            if (buildingCosts == null) return 0;
            return (long)(originalCost * buildingCosts.demolitionRefundRate);
        }

        /// <summary>
        /// Try to purchase a building
        /// </summary>
        public bool TryPurchaseBuilding(Building.BuildingType type)
        {
            long cost = GetBuildingCost(type);
            return TrySpend(cost, $"{type} 建設");
        }

        /// <summary>
        /// Try to purchase a tenant
        /// </summary>
        public bool TryPurchaseTenant(TenantType type)
        {
            long cost = GetTenantCost(type);
            return TrySpend(cost, $"{type} 建設");
        }

        #endregion

        #region Rent Collection

        private void OnDayChanged(int day)
        {
            CollectAllRent();
        }

        /// <summary>
        /// Collect rent from all tenants
        /// </summary>
        public void CollectAllRent()
        {
            // Find all tenants and collect rent
            var tenants = FindObjectsOfType<Building.Tenant>();
            long totalRent = 0;

            foreach (var tenant in tenants)
            {
                if (tenant.IsOccupied)
                {
                    long rent = tenant.CollectRent();
                    totalRent += rent;
                }
            }

            if (totalRent > 0)
            {
                Earn(totalRent, "テナント家賃収入");
            }
        }

        #endregion

        #region Statistics

        /// <summary>
        /// Get total income from transaction history
        /// </summary>
        public long GetTotalIncome()
        {
            long total = 0;
            foreach (var t in transactionHistory)
            {
                if (t.isIncome) total += t.amount;
            }
            return total;
        }

        /// <summary>
        /// Get total expenses from transaction history
        /// </summary>
        public long GetTotalExpenses()
        {
            long total = 0;
            foreach (var t in transactionHistory)
            {
                if (!t.isIncome) total += t.amount;
            }
            return total;
        }

        /// <summary>
        /// Get formatted money string
        /// </summary>
        public string GetFormattedMoney()
        {
            return $"¥{currentMoney:N0}";
        }

        #endregion
    }

    /// <summary>
    /// Tenant types for the economy system
    /// </summary>
    public enum TenantType
    {
        Office,
        Restaurant,
        Shop,
        Apartment
    }
}
