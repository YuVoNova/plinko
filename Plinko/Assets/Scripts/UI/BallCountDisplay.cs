using TMPro;
using UnityEngine;

namespace UI
{
    public class BallCountDisplay : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI ballCountText;
        
        public void UpdateBallCount(int ballCount)
        {
            if (ballCountText)
                ballCountText.text = $"{ballCount}";
        }
    }
}