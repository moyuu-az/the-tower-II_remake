using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TowerGame.Economy;
using TowerGame.Building;

namespace TowerGame.UI
{
    /// <summary>
    /// UI component for displaying economy information
    /// Shows current money, costs, and transaction notifications
    /// </summary>
    public class EconomyUI : MonoBehaviour
    {
        [Header("Money Display")]
        [SerializeField] private TextMeshProUGUI moneyText;
        [SerializeField] private Text legacyMoneyText;

        [Header("Cost Display")]
        [SerializeField] private TextMeshProUGUI costText;
        [SerializeField] private Text legacyCostText;

        [Header("Transaction Notification")]
        [SerializeField] private GameObject transactionPopup;
        [SerializeField] private TextMeshProUGUI transactionText;
        [SerializeField] private float popupDuration = 2f;

        [Header("Visual Settings")]
        [SerializeField] private Color incomeColor = new Color(0.2f, 0.8f, 0.2f);
        [SerializeField] private Color expenseColor = new Color(0.8f, 0.2f, 0.2f);
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color warningColor = new Color(1f, 0.6f, 0.2f);
        [SerializeField] private Color dangerColor = new Color(0.9f, 0.2f, 0.2f);

        private float popupTimer;
        private bool showingPopup;

        private void Start()
        {
            // Subscribe to economy events
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.OnMoneyChanged += UpdateMoneyDisplay;
                EconomyManager.Instance.OnTransaction += ShowTransactionPopup;
            }

            // Subscribe to building placer for cost display
            if (BuildingPlacer.Instance != null)
            {
                BuildingPlacer.Instance.OnBuildingTypeChanged += UpdateCostDisplay;
            }

            // Initial update
            UpdateMoneyDisplay(EconomyManager.Instance?.CurrentMoney ?? 0);
            UpdateCostDisplay(BuildingType.None);

            // Hide popup initially
            if (transactionPopup != null)
            {
                transactionPopup.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.OnMoneyChanged -= UpdateMoneyDisplay;
                EconomyManager.Instance.OnTransaction -= ShowTransactionPopup;
            }

            if (BuildingPlacer.Instance != null)
            {
                BuildingPlacer.Instance.OnBuildingTypeChanged -= UpdateCostDisplay;
            }
        }

        private void Update()
        {
            // Handle popup timer
            if (showingPopup)
            {
                popupTimer -= Time.deltaTime;
                if (popupTimer <= 0)
                {
                    HideTransactionPopup();
                }
            }
        }

        /// <summary>
        /// Update the money display
        /// </summary>
        private void UpdateMoneyDisplay(long money)
        {
            string moneyString = $"¥{money:N0}";

            // Determine color based on money amount
            Color textColor = normalColor;
            if (money < 50000)
            {
                textColor = dangerColor;
            }
            else if (money < 200000)
            {
                textColor = warningColor;
            }

            if (moneyText != null)
            {
                moneyText.text = moneyString;
                moneyText.color = textColor;
            }
            else if (legacyMoneyText != null)
            {
                legacyMoneyText.text = moneyString;
                legacyMoneyText.color = textColor;
            }
        }

        /// <summary>
        /// Update the cost display for selected building type
        /// </summary>
        private void UpdateCostDisplay(BuildingType type)
        {
            if (EconomyManager.Instance == null)
            {
                SetCostText("");
                return;
            }

            if (type == BuildingType.None || type == BuildingType.Demolition)
            {
                SetCostText("");
                return;
            }

            long cost = EconomyManager.Instance.GetBuildingCost(type);
            bool canAfford = EconomyManager.Instance.CanAfford(cost);

            string costString = $"建設費: ¥{cost:N0}";
            Color textColor = canAfford ? normalColor : dangerColor;

            if (costText != null)
            {
                costText.text = costString;
                costText.color = textColor;
            }
            else if (legacyCostText != null)
            {
                legacyCostText.text = costString;
                legacyCostText.color = textColor;
            }
        }

        private void SetCostText(string text)
        {
            if (costText != null)
            {
                costText.text = text;
            }
            else if (legacyCostText != null)
            {
                legacyCostText.text = text;
            }
        }

        /// <summary>
        /// Show transaction notification popup
        /// </summary>
        private void ShowTransactionPopup(long amount, string reason, bool isIncome)
        {
            if (transactionPopup == null) return;

            string prefix = isIncome ? "+" : "-";
            string text = $"{prefix}¥{amount:N0}\n{reason}";

            if (transactionText != null)
            {
                transactionText.text = text;
                transactionText.color = isIncome ? incomeColor : expenseColor;
            }

            transactionPopup.SetActive(true);
            popupTimer = popupDuration;
            showingPopup = true;
        }

        private void HideTransactionPopup()
        {
            if (transactionPopup != null)
            {
                transactionPopup.SetActive(false);
            }
            showingPopup = false;
        }

        /// <summary>
        /// Manual refresh of economy display
        /// </summary>
        public void RefreshDisplay()
        {
            if (EconomyManager.Instance != null)
            {
                UpdateMoneyDisplay(EconomyManager.Instance.CurrentMoney);
            }

            if (BuildingPlacer.Instance != null)
            {
                UpdateCostDisplay(BuildingPlacer.Instance.SelectedBuildingType);
            }
        }
    }
}
