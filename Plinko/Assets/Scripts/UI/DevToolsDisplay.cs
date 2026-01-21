using System;
using Core;
using TMPro;
using UnityEngine;

namespace UI
{
    public class DevToolsDisplay : MonoBehaviour
    {
        public Action OnManualSessionReset;
        public Action OnManualFullReset;
        public Action<float> OnAddBalance;
        
        [SerializeField] private TextMeshProUGUI stateText;
        [SerializeField] private float addBalanceAmount = 100f;
        
        public void UpdateState(GameState state)
        {
            if (stateText != null)
                stateText.text = $"State: {state}";
        }
        
        public void ManualSessionReset()
        {
            OnManualSessionReset?.Invoke();
        }
        
        public void ManualFullReset()
        {
            OnManualFullReset?.Invoke();
        }
        
        public void AddBalance()
        {
            OnAddBalance?.Invoke(addBalanceAmount);
        }
    }
}