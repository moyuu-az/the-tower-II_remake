using System;
using TowerGame.Building;
using TowerGame.Economy;

namespace TowerGame.Core.Services
{
    /// <summary>
    /// Service interface for economic management.
    /// Handles money, transactions, building costs, and rent collection.
    /// </summary>
    public interface IEconomyService : IService
    {
        /// <summary>
        /// Current money balance
        /// </summary>
        long CurrentMoney { get; }

        /// <summary>
        /// Building cost configuration
        /// </summary>
        BuildingCosts Costs { get; }

        /// <summary>
        /// Event fired when money changes
        /// </summary>
        event Action<long> OnMoneyChanged;

        /// <summary>
        /// Event fired on transaction (amount, reason, isIncome)
        /// </summary>
        event Action<long, string, bool> OnTransaction;

        /// <summary>
        /// Check if player can afford an amount
        /// </summary>
        bool CanAfford(long amount);

        /// <summary>
        /// Try to spend money
        /// </summary>
        /// <param name="amount">Amount to spend</param>
        /// <param name="reason">Reason for spending</param>
        /// <returns>True if successful</returns>
        bool TrySpend(long amount, string reason);

        /// <summary>
        /// Add money (income, rent, etc.)
        /// </summary>
        /// <param name="amount">Amount to add</param>
        /// <param name="reason">Reason for income</param>
        void Earn(long amount, string reason);

        /// <summary>
        /// Force set money (for debugging)
        /// </summary>
        void SetMoney(long amount);

        /// <summary>
        /// Get cost for a building type
        /// </summary>
        long GetBuildingCost(BuildingType type);

        /// <summary>
        /// Get cost for a tenant type
        /// </summary>
        long GetTenantCost(TenantType type);

        /// <summary>
        /// Get daily rent for a tenant type
        /// </summary>
        long GetDailyRent(TenantType type);

        /// <summary>
        /// Calculate demolition refund
        /// </summary>
        long GetDemolitionRefund(long originalCost);

        /// <summary>
        /// Try to purchase a building
        /// </summary>
        bool TryPurchaseBuilding(BuildingType type);

        /// <summary>
        /// Try to purchase a tenant
        /// </summary>
        bool TryPurchaseTenant(TenantType type);

        /// <summary>
        /// Collect rent from all tenants
        /// </summary>
        void CollectAllRent();

        /// <summary>
        /// Get total income from history
        /// </summary>
        long GetTotalIncome();

        /// <summary>
        /// Get total expenses from history
        /// </summary>
        long GetTotalExpenses();

        /// <summary>
        /// Get formatted money string
        /// </summary>
        string GetFormattedMoney();
    }
}
