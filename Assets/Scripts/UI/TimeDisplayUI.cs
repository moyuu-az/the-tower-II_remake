using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TowerGame.Core;

namespace TowerGame.UI
{
    /// <summary>
    /// Displays current game time on UI
    /// </summary>
    public class TimeDisplayUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI timeText;
        [SerializeField] private Text legacyTimeText; // Fallback if TMP not available

        [Header("Display Settings")]
        [SerializeField] private bool showDay = true;
        [SerializeField] private string format = "Day {0} - {1}";

        private void Start()
        {
            if (GameTimeManager.Instance != null)
            {
                GameTimeManager.Instance.OnTimeUpdated += UpdateDisplay;
                UpdateDisplay(GameTimeManager.Instance.CurrentHour);
            }
        }

        private void OnDestroy()
        {
            if (GameTimeManager.Instance != null)
            {
                GameTimeManager.Instance.OnTimeUpdated -= UpdateDisplay;
            }
        }

        private void UpdateDisplay(float currentHour)
        {
            if (GameTimeManager.Instance == null) return;

            string displayText;
            if (showDay)
            {
                displayText = string.Format(format,
                    GameTimeManager.Instance.CurrentDay,
                    GameTimeManager.Instance.GetFormattedTime());
            }
            else
            {
                displayText = GameTimeManager.Instance.GetFormattedTime();
            }

            if (timeText != null)
            {
                timeText.text = displayText;
            }
            else if (legacyTimeText != null)
            {
                legacyTimeText.text = displayText;
            }
        }

        /// <summary>
        /// Manual refresh of the display
        /// </summary>
        public void Refresh()
        {
            if (GameTimeManager.Instance != null)
            {
                UpdateDisplay(GameTimeManager.Instance.CurrentHour);
            }
        }
    }
}
