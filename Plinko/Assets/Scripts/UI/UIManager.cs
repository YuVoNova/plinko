using System.Collections.Generic;
using Core;
using Data;
using Gameplay;
using UnityEngine;

namespace UI
{
    public class UIManager : MonoBehaviour
    {
        [SerializeField] private WalletDisplay walletDisplay;
        [SerializeField] private LevelDisplay levelDisplay;
        [SerializeField] private BallCountDisplay ballCountDisplay;
        [SerializeField] private TimerDisplay timerDisplay;
        [SerializeField] private LevelUpDisplay levelUpDisplay;
        [SerializeField] private HistoryDisplay historyDisplay;
        [SerializeField] private DevToolsDisplay devToolsDisplay;
        [SerializeField] private BatchProcessingIndicator batchIndicator;
        
        [SerializeField] private PlinkoGameManager gameManager;
        
        private void Start()
        {
            SubscribeEvents();
        }
        
        private void OnDestroy()
        {
            UnsubscribeEvents();
        }
        
        private void SubscribeEvents()
        {
            if (gameManager)
            {
                gameManager.OnWalletUpdated += HandleWalletUpdated;
                gameManager.OnLevelChanged += HandleLevelChanged;
                gameManager.OnBallCountChanged += HandleBallCountChanged;
                gameManager.OnTimerUpdated += HandleTimerUpdated;
                gameManager.OnStateChanged += HandleStateChanged;
                gameManager.OnBallResult += HandleBallResult;
                gameManager.OnBatchProcessingChanged += HandleBatchProcessingChanged;
                gameManager.OnHistoryLoaded += HandleHistoryLoaded;
            }
            
            if (devToolsDisplay)
            {
                devToolsDisplay.OnManualSessionReset += HandleManualSessionReset;
                devToolsDisplay.OnManualFullReset += HandleManualFullReset;
                devToolsDisplay.OnAddBalance += HandleAddBalance;
            }
        }
        
        private void UnsubscribeEvents()
        {
            if (gameManager)
            {
                gameManager.OnWalletUpdated -= HandleWalletUpdated;
                gameManager.OnLevelChanged -= HandleLevelChanged;
                gameManager.OnBallCountChanged -= HandleBallCountChanged;
                gameManager.OnTimerUpdated -= HandleTimerUpdated;
                gameManager.OnStateChanged -= HandleStateChanged;
                gameManager.OnBallResult -= HandleBallResult;
                gameManager.OnBatchProcessingChanged -= HandleBatchProcessingChanged;
                gameManager.OnHistoryLoaded -= HandleHistoryLoaded;
            }
            
            if (devToolsDisplay)
            {
                devToolsDisplay.OnManualSessionReset -= HandleManualSessionReset;
                devToolsDisplay.OnManualFullReset -= HandleManualFullReset;
                devToolsDisplay.OnAddBalance -= HandleAddBalance;
            }
        }
        
        #region Event Handlers
        
        private void HandleWalletUpdated(float balance)
        {
            walletDisplay?.SetBalance(balance);
        }
        
        private void HandleLevelChanged(int level)
        {
            levelDisplay?.UpdateLevel(level);
        }
        
        private void HandleBallCountChanged(int count)
        {
            ballCountDisplay?.UpdateBallCount(count);
        }
        
        private void HandleTimerUpdated(float secondsRemaining)
        {
            timerDisplay?.UpdateTimer(secondsRemaining);
        }
        
        private void HandleStateChanged(GameState oldState, GameState newState)
        {
            devToolsDisplay?.UpdateState(newState);
            
            if (newState == GameState.LevelTransition)
            {
                ShowLevelUpPopup();
            }
        }
        
        private void HandleBallResult(BallResultData ballResultData)
        {
            RecordDrop(ballResultData);
        }

        private void HandleBatchProcessingChanged(bool isProcessing)
        {
            if (isProcessing)
                batchIndicator?.Show();
            else
                batchIndicator?.Hide();
        }

        private void HandleHistoryLoaded(List<BallResultData> history)
        {
            historyDisplay?.LoadHistory(history);
        }
        
        private void ShowLevelUpPopup()
        {
            if (!levelUpDisplay || !gameManager)
                return;
            
            int newLevel = gameManager.CurrentLevel;
            int ballCount = gameManager.CurrentBallCount;
            levelUpDisplay.Show(newLevel, ballCount);
        }
        
        #endregion
        
        #region Dev Tools Handlers
        
        private void HandleManualSessionReset()
        {
            Debug.Log("[UI] Manual session reset requested");
            gameManager?.ManualSessionReset();
            historyDisplay?.ClearHistory();
        }
        
        private void HandleManualFullReset()
        {
            Debug.Log("[UI] Manual full reset requested");
            gameManager?.ManualFullReset();
            historyDisplay?.ClearHistory();
        }
        
        private void HandleAddBalance(float amount)
        {
            Debug.Log($"[UI] Add balance requested: +${amount}");
            gameManager?.AddBalance(amount);
        }
        
        #endregion
        
        #region History Management
        
        public void RecordDrop(BallResultData ballResultData)
        {
            historyDisplay?.AddEntry(ballResultData);
        }
        
        #endregion
    }
}