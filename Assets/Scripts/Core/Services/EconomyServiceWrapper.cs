using System;
using UnityEngine;
using TowerGame.Economy;
using TowerGame.Building;

namespace TowerGame.Core.Services
{
    /// <summary>
    /// Wrapper class that adapts EconomyManager to IEconomyService interface.
    /// </summary>
    public class EconomyServiceWrapper : IEconomyService
    {
        private readonly EconomyManager manager;

        public EconomyServiceWrapper(EconomyManager manager)
        {
            this.manager = manager ?? throw new ArgumentNullException(nameof(manager));
        }

        #region IService Implementation

        public void Initialize()
        {
            Debug.Log("[EconomyServiceWrapper] Initialized");
        }

        public void Shutdown()
        {
            Debug.Log("[EconomyServiceWrapper] Shutdown");
        }

        #endregion

        #region IEconomyService Implementation

        public long CurrentMoney => manager.CurrentMoney;
        public BuildingCosts Costs => manager.Costs;

        public event Action<long> OnMoneyChanged
        {
            add => manager.OnMoneyChanged += value;
            remove => manager.OnMoneyChanged -= value;
        }

        public event Action<long, string, bool> OnTransaction
        {
            add => manager.OnTransaction += value;
            remove => manager.OnTransaction -= value;
        }

        public bool CanAfford(long amount) => manager.CanAfford(amount);
        public bool TrySpend(long amount, string reason) => manager.TrySpend(amount, reason);
        public void Earn(long amount, string reason) => manager.Earn(amount, reason);
        public void SetMoney(long amount) => manager.SetMoney(amount);
        public long GetBuildingCost(BuildingType type) => manager.GetBuildingCost(type);
        public long GetTenantCost(TenantType type) => manager.GetTenantCost(type);
        public long GetDailyRent(TenantType type) => manager.GetDailyRent(type);
        public long GetDemolitionRefund(long originalCost) => manager.GetDemolitionRefund(originalCost);
        public bool TryPurchaseBuilding(BuildingType type) => manager.TryPurchaseBuilding(type);
        public bool TryPurchaseTenant(TenantType type) => manager.TryPurchaseTenant(type);
        public void CollectAllRent() => manager.CollectAllRent();
        public long GetTotalIncome() => manager.GetTotalIncome();
        public long GetTotalExpenses() => manager.GetTotalExpenses();
        public string GetFormattedMoney() => manager.GetFormattedMoney();

        #endregion
    }
}
