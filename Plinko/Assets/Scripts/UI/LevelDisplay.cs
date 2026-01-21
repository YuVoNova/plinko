using TMPro;
using UnityEngine;

namespace UI
{
    public class LevelDisplay : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI levelText;
        
        public void UpdateLevel(int level)
        {
            if (levelText != null)
                levelText.text = $"Level {level}";
        }
    }
}