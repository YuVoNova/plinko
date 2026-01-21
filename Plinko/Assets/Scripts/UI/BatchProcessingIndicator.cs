using UnityEngine;

namespace UI
{
    public class BatchProcessingIndicator : MonoBehaviour
    {
        [SerializeField] private GameObject indicatorObject;
        
        private void Awake()
        {
            Hide();
        }
        
        public void Show()
        {
            if (indicatorObject)
                indicatorObject.SetActive(true);
        }
        
        public void Hide()
        {
            if (indicatorObject)
                indicatorObject.SetActive(false);
        }
    }
}