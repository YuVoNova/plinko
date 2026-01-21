using TMPro;
using UnityEngine;

namespace UI
{
    public class TimerDisplay : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI timerText;
        
        public void UpdateTimer(float secondsRemaining)
        {
            if (timerText == null)
                return;
            
            int minutes = Mathf.FloorToInt(secondsRemaining / 60f);
            int seconds = Mathf.FloorToInt(secondsRemaining % 60f);
            
            timerText.text = $"Reset in {minutes}m {seconds}s";
        }
    }
}