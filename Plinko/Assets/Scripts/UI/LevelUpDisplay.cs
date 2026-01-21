using System.Collections;
using TMPro;
using UnityEngine;

namespace UI
{
    public class LevelUpDisplay : MonoBehaviour
    {
        [SerializeField] private GameObject panelObject;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI ballAmountText;
        [SerializeField] private float autoCloseDelay = 2f;
        
        private Coroutine _autoCloseCoroutine;
        
        private void Awake()
        {
            Hide();
        }
        
        public void Show(int newLevel, int ballAmount)
        {
            panelObject?.SetActive(true);
            
            if (levelText)
                levelText.text = $"LEVEL {newLevel}!";
            
            if (ballAmountText)
                ballAmountText.text = $"+{ballAmount} Balls";
            
            if (_autoCloseCoroutine != null)
                StopCoroutine(_autoCloseCoroutine);
            
            _autoCloseCoroutine = StartCoroutine(AutoClose());
        }
        
        public void Hide()
        {
            if (_autoCloseCoroutine != null)
            {
                StopCoroutine(_autoCloseCoroutine);
                _autoCloseCoroutine = null;
            }
            
            panelObject?.SetActive(false);
        }
        
        private IEnumerator AutoClose()
        {
            yield return new WaitForSeconds(autoCloseDelay);
            Hide();
        }
    }
}