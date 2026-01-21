using TMPro;
using UnityEngine;

namespace UI
{
    public class WalletDisplay : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI walletText;
        [SerializeField] private float countDuration = 0.5f;

        private float _currentValue;
        private float _targetValue;
        private float _velocity;
        private float _startValue;
        private float _elapsedTime;
        private bool _isAnimating;

        private void Update()
        {
            if (!_isAnimating)
                return;

            _elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsedTime / countDuration);

            _currentValue = Mathf.Lerp(_startValue, _targetValue, t);

            if (t >= 1f)
            {
                _currentValue = _targetValue;
                _isAnimating = false;
            }

            UpdateDisplay();
        }

        public void SetBalance(float balance)
        {
            _startValue = _currentValue;
            _targetValue = balance;
            _elapsedTime = 0f;
            _isAnimating = true;
        }

        private void UpdateDisplay()
        {
            if (walletText)
                walletText.text = $"${_currentValue:F2}";
        }
    }
}