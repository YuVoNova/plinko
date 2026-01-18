using TMPro;
using UnityEngine;

namespace Gameplay
{
    public class PlinkoBucket : MonoBehaviour
    {
        [SerializeField] private int bucketIndex;
        [SerializeField] private TextMeshProUGUI multiplierText;
        [SerializeField] private SpriteRenderer background;

        public int BucketIndex => bucketIndex;
        
        public void Initialize(int index)
        {
            bucketIndex = index;
            gameObject.name = $"Bucket_{index}";
        }
        
        public void UpdateDisplay(float multiplier)
        {
            if (multiplierText != null)
                multiplierText.text = $"x{multiplier:F1}";
        }
        
        public void SetColor(Color color)
        {
            if (!background)
                return;
            
            background.color = color;
        }
        
        public void TriggerHitEffect()
        {
            // TODO -> Apply trigger effect here
        }
    }
}