using System.Collections;
using UnityEngine;

namespace UI
{
    public class BatchProcessingIndicator : MonoBehaviour
    {
        [SerializeField] private GameObject indicatorObject;
        
        private float showDelayThreshold = 0.05f;
        
        private Coroutine _showCoroutine;
        private bool _isProcessing;
        
        private void Awake()
        {
            Hide();
        }
        
        public void Show()
        {
            _isProcessing = true;
            
            if (_showCoroutine != null)
                StopCoroutine(_showCoroutine);
            
            _showCoroutine = StartCoroutine(ShowAfterDelay());
        }
        
        public void Hide()
        {
            _isProcessing = false;
            
            if (_showCoroutine != null)
            {
                StopCoroutine(_showCoroutine);
                _showCoroutine = null;
            }
            
            indicatorObject?.SetActive(false);
        }
        
        private IEnumerator ShowAfterDelay()
        {
            yield return new WaitForSeconds(showDelayThreshold);
            
            if (_isProcessing)
                indicatorObject?.SetActive(true);
            
            _showCoroutine = null;
        }
    }
}