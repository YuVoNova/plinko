using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Backend;
using Core;
using Data;
using UnityEngine;

namespace Gameplay
{
    public class PlinkoGameManager : MonoBehaviour
    {
        private const float SESSION_CHECK_INTERVAL = 1f;

        public Action<int> OnBallCountChanged;
        public Action<int> OnLevelChanged;
        public Action<float> OnWalletUpdated;
        public Action<float> OnTimerUpdated;
        public Action<GameState, GameState> OnStateChanged;
        public Action<BallResultData> OnBallResult;
        public Action<bool> OnBatchProcessingChanged;
        public Action<List<BallResultData>> OnHistoryLoaded;

        [SerializeField] private PlinkoBoard board;
        [SerializeField] private PlinkoBallSpawner spawner;
        [SerializeField] private InputHandler inputHandler;
        [SerializeField] private MockServerService backend;

        private PlinkoSpawnController _plinkoSpawnController;
        private PlinkoLevelController _plinkoLevelController;
        private PlinkoBatchProcessor _batchProcessor;
        private PlinkoSessionManager _sessionManager;

        private GameState _currentState;

        public int CurrentLevel => _plinkoLevelController?.CurrentLevel ?? 0;
        public int CurrentBallCount => _plinkoSpawnController?.CurrentBallCount ?? 0;

        #region Unity Lifecycle

        private async void Start()
        {
            await InitializeGame();
        }

        private void Update()
        {
            _batchProcessor?.CheckTimeout();
        }

        private void OnDestroy()
        {
            UnsubscribeAll();
        }

        #endregion

        #region Initialization

        private async Task InitializeGame()
        {
            ChangeState(GameState.Initializing);

            InitializeControllers();
            InitializeBatchProcessor();
            InitializeSessionManager();

            board.InitializeBoard();
            _plinkoSpawnController.SetSpawnPosition(board.GetSpawnPosition());

            InitResponse initResponse = await backend.InitializeGame();

            if (!initResponse.Success)
            {
                Debug.LogError($"[GAME] Failed to initialize: {initResponse.Message}");
                return;
            }

            LevelLoadResult levelResult = await _plinkoLevelController.LoadLevel(initResponse.CurrentLevel);

            if (!levelResult.Success)
            {
                Debug.LogError("[GAME] Failed to load initial level");
                return;
            }

            _plinkoLevelController.Initialize(initResponse.CurrentLevel);
            _plinkoSpawnController.Initialize(initResponse.BallCount, levelResult.BallAmount);

            SetupInput();

            _sessionManager.StartMonitoring();

            ChangeState(GameState.Ready);

            NotifyUIInitialized(initResponse);

            Debug.Log($"[GAME] Initialized: {initResponse.BallCount} balls, Level {initResponse.CurrentLevel}");
        }

        private void InitializeControllers()
        {
            _plinkoSpawnController = new PlinkoSpawnController(spawner);
            _plinkoSpawnController.SubscribeToSpawner();
            _plinkoSpawnController.OnBallCountChanged += HandleBallCountChanged;
            _plinkoSpawnController.OnLevelBallsComplete += HandleLevelBallsComplete;

            _plinkoLevelController = new PlinkoLevelController(backend, board);
            _plinkoLevelController.OnLevelChanged += HandleLevelChanged;
            _plinkoLevelController.OnLevelUpStarted += HandleLevelUpStarted;
            _plinkoLevelController.OnLevelUpCompleted += HandleLevelUpCompleted;

            spawner.OnBallLanded += HandleBallLanded;
        }

        private void InitializeBatchProcessor()
        {
            _batchProcessor = new PlinkoBatchProcessor(backend, 0);
            _batchProcessor.OnBatchProcessed += HandleBatchProcessed;
            _batchProcessor.OnBatchStarted += HandleBatchStarted;
            _batchProcessor.OnBatchCompleted += HandleBatchCompleted;
            _batchProcessor.OnBallResult += HandleBallRewardsCalculated;
        }

        private void InitializeSessionManager()
        {
            _sessionManager = new PlinkoSessionManager(backend, SESSION_CHECK_INTERVAL);
            _sessionManager.OnTimerUpdated += HandleTimerUpdated;
            _sessionManager.OnResetNeeded += HandleResetNeeded;
        }

        private void SetupInput()
        {
            if (inputHandler == null) return;

            inputHandler.OnSpawnPressed += HandleSpawnPressed;
            inputHandler.OnSpawnReleased += HandleSpawnReleased;
        }

        #endregion

        #region State Management

        private void ChangeState(GameState newState)
        {
            if (_currentState == newState) return;

            GameState oldState = _currentState;
            _currentState = newState;

            Debug.Log($"[STATE] {oldState} → {newState}");

            OnStateChanged?.Invoke(oldState, newState);
        }

        private bool IsSpawnableState()
        {
            return _currentState is GameState.Ready or GameState.Playing;
        }

        #endregion

        #region Input Handlers

        private void HandleSpawnPressed()
        {
            if (!_plinkoSpawnController.CanSpawn(IsSpawnableState(), _plinkoLevelController.IsLevelingUp))
                return;

            ChangeState(GameState.Playing);
            _plinkoSpawnController.StartSpawning();
        }

        private void HandleSpawnReleased()
        {
            _plinkoSpawnController.StopSpawning();

            if (_currentState == GameState.Playing)
                ChangeState(GameState.Ready);
        }

        #endregion

        #region Spawn Controller Handlers

        private void HandleBallCountChanged(int count)
        {
            OnBallCountChanged?.Invoke(count);
        }

        private async void HandleLevelBallsComplete()
        {
            if (_plinkoLevelController.IsLevelingUp)
                return;

            ChangeState(GameState.LevelTransition);
            _plinkoSpawnController.StopSpawning();

            await _plinkoLevelController.CheckAndProcessLevelUp(
                _plinkoSpawnController.BallsDroppedThisLevel,
                FlushAndWaitForBalls
            );
        }

        #endregion

        #region Level Controller Handlers

        private void HandleLevelChanged(int level)
        {
            OnLevelChanged?.Invoke(level);
        }

        private void HandleLevelUpStarted()
        {
            _plinkoSpawnController.LockSpawning();
        }

        private void HandleLevelUpCompleted()
        {
            _plinkoSpawnController.SetBallCount(_plinkoLevelController.BallsRequiredForLevel);
            _plinkoSpawnController.SetBallsRequiredForLevel(_plinkoLevelController.BallsRequiredForLevel);
            _plinkoSpawnController.UnlockSpawning();

            OnBallCountChanged?.Invoke(_plinkoSpawnController.CurrentBallCount);

            ChangeState(GameState.Ready);
        }

        #endregion

        #region Ball Landing & Batch Handlers

        private void HandleBallLanded(int bucketIndex)
        {
            Debug.Log($"[GAME] Ball landed in Bucket {bucketIndex}");
            _batchProcessor.AddBallLanding(bucketIndex);
        }

        private void HandleBatchStarted()
        {
            OnBatchProcessingChanged?.Invoke(true);
        }

        private void HandleBatchCompleted()
        {
            OnBatchProcessingChanged?.Invoke(false);
        }

        private void HandleBallRewardsCalculated(List<BallResultData> rewards)
        {
            foreach (BallResultData reward in rewards)
            {
                OnBallResult?.Invoke(reward);
            }
        }

        private void HandleBatchProcessed(float walletBalance, float rewardEarned)
        {
            OnWalletUpdated?.Invoke(walletBalance);
        }

        #endregion

        #region Session Handlers

        private void HandleTimerUpdated(float timeRemaining)
        {
            OnTimerUpdated?.Invoke(timeRemaining);
        }

        private async void HandleResetNeeded()
        {
            await PerformReset();
        }

        #endregion

        #region Reset

        public async void ManualSessionReset()
        {
            Debug.Log("[GAME] Manual session reset triggered.");
            await PerformReset();
        }

        public async void ManualFullReset()
        {
            Debug.Log("[GAME] Manual full reset triggered.");
            await PerformReset(true);
        }

        private async Task PerformReset(bool isFullReset = false)
        {
            ChangeState(GameState.Resetting);

            _plinkoSpawnController.StopSpawning();
            inputHandler?.ForceRelease();

            await FlushAndWaitForBalls();
            _batchProcessor.Reset();

            _sessionManager.StopMonitoring();

            ResetResponse response = await backend.ResetGame(isFullReset);

            if (!response.Success)
            {
                Debug.LogError($"[GAME] Failed to reset: {response.Message}");
                ChangeState(GameState.Ready);
                return;
            }

            _plinkoLevelController.Initialize(response.CurrentLevel);
            
            LevelLoadResult levelResult = await _plinkoLevelController.LoadLevel(response.CurrentLevel);
            
            _plinkoSpawnController.Initialize(response.BallCount, levelResult.BallAmount);
            _plinkoSpawnController.ResetSpawner();

            _sessionManager.StartMonitoring();

            NotifyUIReset(response);

            ChangeState(GameState.Ready);

            if (isFullReset)
                Debug.Log("[GAME] Full reset complete.");
            else
                Debug.Log($"[GAME] Reset complete. Wallet preserved: {response.WalletBalance:F2}");
        }

        #endregion

        #region Helper Methods

        private async Task FlushAndWaitForBalls()
        {
            _batchProcessor.FlushBatch();
            await WaitForActiveBalls();
            _batchProcessor.FlushBatch();
        }

        private async Task WaitForActiveBalls()
        {
            const int maxWaitTime = 8000;
            const int checkInterval = 100;

            int totalWaitTime = 0;
            int initialActive = _plinkoSpawnController.ActiveBallCount;

            if (initialActive == 0)
            {
                Debug.Log("[GAME] No active balls to wait for.");
                return;
            }

            Debug.Log($"[GAME] Waiting for {initialActive} active balls...");

            while (_plinkoSpawnController.HasActiveBalls && totalWaitTime < maxWaitTime)
            {
                await Task.Delay(checkInterval);
                totalWaitTime += checkInterval;

                if (totalWaitTime % 1000 == 0)
                    Debug.Log($"[GAME] Waiting... {_plinkoSpawnController.ActiveBallCount} balls active. ({totalWaitTime}ms)");
            }

            if (_plinkoSpawnController.HasActiveBalls)
            {
                Debug.LogWarning($"[GAME] Timeout! Force clearing {_plinkoSpawnController.ActiveBallCount} stuck balls.");
                _plinkoSpawnController.ResetSpawner();
            }
            else
            {
                Debug.Log($"[GAME] All balls settled in {totalWaitTime}ms.");
            }
        }

        #endregion

        #region UI Notifications

        private void NotifyUIInitialized(InitResponse response)
        {
            OnBallCountChanged?.Invoke(response.BallCount);
            OnLevelChanged?.Invoke(response.CurrentLevel);
            OnWalletUpdated?.Invoke(response.WalletBalance);
            OnTimerUpdated?.Invoke(response.TimeUntilReset);

            if (response.RewardHistory?.Count > 0)
                OnHistoryLoaded?.Invoke(response.RewardHistory);
        }

        private void NotifyUIReset(ResetResponse response)
        {
            OnBallCountChanged?.Invoke(response.BallCount);
            OnLevelChanged?.Invoke(response.CurrentLevel);
            OnWalletUpdated?.Invoke(response.WalletBalance);
        }

        #endregion

        #region Cleanup

        private void UnsubscribeAll()
        {
            if (inputHandler != null)
            {
                inputHandler.OnSpawnPressed -= HandleSpawnPressed;
                inputHandler.OnSpawnReleased -= HandleSpawnReleased;
            }

            if (spawner != null)
            {
                spawner.OnBallLanded -= HandleBallLanded;
            }

            if (_plinkoSpawnController != null)
            {
                _plinkoSpawnController.UnsubscribeFromSpawner();
                _plinkoSpawnController.OnBallCountChanged -= HandleBallCountChanged;
                _plinkoSpawnController.OnLevelBallsComplete -= HandleLevelBallsComplete;
            }

            if (_plinkoLevelController != null)
            {
                _plinkoLevelController.OnLevelChanged -= HandleLevelChanged;
                _plinkoLevelController.OnLevelUpStarted -= HandleLevelUpStarted;
                _plinkoLevelController.OnLevelUpCompleted -= HandleLevelUpCompleted;
            }

            if (_batchProcessor != null)
            {
                _batchProcessor.FlushBatch();
                _batchProcessor.OnBatchProcessed -= HandleBatchProcessed;
                _batchProcessor.OnBatchStarted -= HandleBatchStarted;
                _batchProcessor.OnBatchCompleted -= HandleBatchCompleted;
                _batchProcessor.OnBallResult -= HandleBallRewardsCalculated;
            }

            if (_sessionManager != null)
            {
                _sessionManager.OnTimerUpdated -= HandleTimerUpdated;
                _sessionManager.OnResetNeeded -= HandleResetNeeded;
                _sessionManager.StopMonitoring();
            }
        }

        #endregion

        #region Dev Tools

        public async void AddBalance(float amount)
        {
            float newBalance = await backend.AddBalance(amount);
            OnWalletUpdated?.Invoke(newBalance);
            Debug.Log($"[GAME] Balance added: +{amount:F2}, New balance: {newBalance:F2}");
        }

        #endregion
    }
}