using TMPro;
using UnityEngine;
using Data;

namespace UI
{
    public class HistoryEntryUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI dropNumberText;
        [SerializeField] private TextMeshProUGUI multiplierText;
        [SerializeField] private TextMeshProUGUI rewardText;
        
        public void SetData(BallResultData entry)
        {
            if (dropNumberText)
                dropNumberText.text = $"Drop #{entry.DropNumber}";
            
            if (multiplierText)
                multiplierText.text = $"x{entry.Multiplier:F1}";
            
            if (rewardText)
                rewardText.text = $"+${entry.Reward:F2}";
        }
    }
}