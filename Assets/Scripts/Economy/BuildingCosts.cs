using UnityEngine;

namespace TowerGame.Economy
{
    /// <summary>
    /// ScriptableObject containing all building costs and economic parameters
    /// The Tower II style pricing structure
    /// </summary>
    [CreateAssetMenu(fileName = "BuildingCosts", menuName = "TowerGame/Building Costs")]
    public class BuildingCosts : ScriptableObject
    {
        [Header("Foundation Costs")]
        [Tooltip("Cost to build a lobby (1F foundation)")]
        public long lobbyCost = 100000;

        [Header("Structure Costs")]
        [Tooltip("Cost to build a floor structure (2F+)")]
        public long floorCost = 50000;

        [Header("Tenant Costs")]
        [Tooltip("Cost to build an office")]
        public long officeCost = 80000;

        [Tooltip("Cost to build a restaurant")]
        public long restaurantCost = 150000;

        [Tooltip("Cost to build a shop")]
        public long shopCost = 120000;

        [Tooltip("Cost to build an apartment")]
        public long apartmentCost = 200000;

        [Header("Transportation Costs")]
        [Tooltip("Cost to build an elevator shaft")]
        public long elevatorCost = 300000;

        [Tooltip("Cost per additional floor for elevator extension")]
        public long elevatorExtensionCostPerFloor = 50000;

        [Header("Demolition")]
        [Tooltip("Percentage of original cost refunded when demolishing (0.0-1.0)")]
        [Range(0f, 1f)]
        public float demolitionRefundRate = 0.5f;

        [Header("Daily Rent Income")]
        [Tooltip("Daily rent from an occupied office")]
        public long officeRentPerDay = 5000;

        [Tooltip("Daily rent from an occupied restaurant")]
        public long restaurantRentPerDay = 8000;

        [Tooltip("Daily rent from an occupied shop")]
        public long shopRentPerDay = 6000;

        [Tooltip("Daily rent from an occupied apartment")]
        public long apartmentRentPerDay = 3000;

        [Header("Maintenance Costs (Daily)")]
        [Tooltip("Daily elevator maintenance cost")]
        public long elevatorMaintenancePerDay = 1000;

        [Tooltip("Daily floor maintenance cost")]
        public long floorMaintenancePerDay = 500;

        [Header("Bonus Multipliers")]
        [Tooltip("Rent multiplier for restaurants during lunch hours (11:00-14:00)")]
        public float restaurantLunchBonus = 1.5f;

        [Tooltip("Rent multiplier for restaurants during dinner hours (18:00-21:00)")]
        public float restaurantDinnerBonus = 1.3f;

        [Tooltip("Rent multiplier for shops on weekends")]
        public float shopWeekendBonus = 1.2f;

        /// <summary>
        /// Get cost description for UI display
        /// </summary>
        public string GetCostDescription(TenantType type)
        {
            switch (type)
            {
                case TenantType.Office:
                    return $"オフィス: ¥{officeCost:N0} (家賃: ¥{officeRentPerDay:N0}/日)";
                case TenantType.Restaurant:
                    return $"レストラン: ¥{restaurantCost:N0} (家賃: ¥{restaurantRentPerDay:N0}/日)";
                case TenantType.Shop:
                    return $"ショップ: ¥{shopCost:N0} (家賃: ¥{shopRentPerDay:N0}/日)";
                case TenantType.Apartment:
                    return $"アパート: ¥{apartmentCost:N0} (家賃: ¥{apartmentRentPerDay:N0}/日)";
                default:
                    return "不明";
            }
        }
    }
}
