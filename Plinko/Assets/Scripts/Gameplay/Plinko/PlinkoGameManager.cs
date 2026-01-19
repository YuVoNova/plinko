using System;
using UnityEngine;
using System.Threading.Tasks;
using Backend;
using Core;

namespace Gameplay
{
    public class PlinkoGameManager : MonoBehaviour
    {
        private const float SESSION_CHECK_INTERVAL = 1f;

        public Action<int> OnBallCountChanged;
        public Action<int> OnLevelChanged;
        public Action<float> OnWalletUpdated;
        public Action<float> OnTimerUpdated;
        public Action<GameState, GameState> OnStateChanged; // Old State, New State

        [SerializeField] private PlinkoBoard board;
        [SerializeField] private PlinkoBallSpawner spawner;
        [SerializeField] private InputHandler inputHandler;
        [SerializeField] private MockServerService backend;

        private PlinkoBatchProcessor _batchProcessor;
        private PlinkoSessionManager _sessionManager;

        private GameState _currentState;
        private int _currentBallCount;
        private int _currentLevel;
        private int _ballAmount;
        private int _ballsDroppedThisLevel;
        private bool _isLevelingUp = false;

        #region Unity Functions

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
            if (inputHandler != null)
            {
                inputHandler.OnSpawnPressed -= StartSpawning;
                inputHandler.OnSpawnReleased -= StopSpawning;
            }

            if (spawner != null)
            {
                spawner.OnBallSpawned -= HandleBallSpawned;
                spawner.OnBallLanded -= HandleBallLanded;
                spawner.OnBallRefunded -= HandleBallRefunded;
            }

            if (_batchProcessor != null)
            {
                _batchProcessor.FlushBatch();
                _batchProcessor.OnBatchProcessed -= HandleBatchProcessed;
            }

            if (_sessionManager != null)
            {
                _sessionManager.OnTimerUpdated -= OnTimerUpdated;
                _sessionManager.OnResetNeeded -= HandleResetNeeded;
                _sessionManager.StopMonitoring();
            }
        }

        #endregion

        #region Initialization

        private async Task InitializeGame()
        {
            ChangeState(GameState.Initializing);

            InitializeBatchProcessor();
            InitializeSessionManager();

            board.InitializeBoard();
            spawner.SetSpawnPosition(board.GetSpawnPosition());

            InitResponse initResponse = await backend.InitializeGame();

            if (!initResponse.Success)
            {
                Debug.LogError($"[GAME] Failed to initialize: {initResponse.Message}");
                return;
            }

            _currentBallCount = initResponse.BallCount;
            _currentLevel = initResponse.CurrentLevel;
            _ballsDroppedThisLevel = 0;

            await LoadLevel(_currentLevel);

            SetupInput();
            SetupSpawner();

            _sessionManager.StartMonitoring();

            ChangeState(GameState.Ready);

            NotifyUI(initResponse.WalletBalance);

            Debug.Log($"[GAME] Initialized: {_currentBallCount} balls, Level {_currentLevel}");
        }

        private void InitializeBatchProcessor()
        {
            _batchProcessor = new PlinkoBatchProcessor(backend);
            _batchProcessor.OnBatchProcessed += HandleBatchProcessed;
        }

        private void InitializeSessionManager()
        {
            _sessionManager = new PlinkoSessionManager(backend, SESSION_CHECK_INTERVAL);
            _sessionManager.OnTimerUpdated += OnTimerUpdated;
            _sessionManager.OnResetNeeded += HandleResetNeeded;
        }

        private void SetupInput()
        {
            if (inputHandler == null) return;

            inputHandler.OnSpawnPressed += StartSpawning;
            inputHandler.OnSpawnReleased += StopSpawning;
        }

        private void SetupSpawner()
        {
            if (spawner == null)
                return;

            spawner.OnBallLanded += HandleBallLanded;
            spawner.OnBallSpawned += HandleBallSpawned;
            spawner.OnBallRefunded += HandleBallRefunded;
        }

        private async Task LoadLevel(int level)
        {
            LevelConfigResponse response = await backend.GetLevelConfig(level);

            if (response.Success && response.LevelConfig != null)
            {
                _ballAmount = response.BallAmount;

                board.ConfigureBucketsFromServer(response.LevelConfig);

                Debug.Log($"[GAME] Level {level} loaded. | Balls for this level: {_ballAmount}");
            }
            else
            {
                Debug.LogError($"[GAME] Failed to load level: {response.Message}");
            }
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

        #endregion

        #region Ball Spawning

        private void StartSpawning()
        {
            if (!CanSpawn())
                return;

            if (_currentBallCount <= 0)
            {
                Debug.LogWarning("[GAME] No balls remaining!");
                return;
            }

            ChangeState(GameState.Playing);
            spawner.StartSpawning();
        }

        private void StopSpawning()
        {
            spawner.StopSpawning();

            if (_currentState == GameState.Playing)
            {
                ChangeState(GameState.Ready);
            }
        }

        private bool CanSpawn()
        {
            return _currentState is GameState.Ready or GameState.Playing && _currentBallCount > 0;
        }

        #endregion

        #region Ball Landing & Batching

        private void HandleBallSpawned()
        {
            if (_currentBallCount <= 0)
            {
                Debug.LogError("[GAME] Spawned ball but count at 0!");
                StopSpawning();
                return;
            }

            _currentBallCount--;
            _ballsDroppedThisLevel++;

            Debug.Log($"[GAME] Ball spawned | Remaining: {_currentBallCount} | Progress: {_ballsDroppedThisLevel}/{_ballAmount}");

            OnBallCountChanged?.Invoke(_currentBallCount);

            if (_currentBallCount <= 0)
            {
                StopSpawning();
                Debug.Log($"[GAME] All balls have spawned! Waiting for them to land...");
            }

            // Check if ready to level up (based on spawn count, not landing count)
            if (_ballsDroppedThisLevel >= _ballAmount && !_isLevelingUp)
            {
                // Stop spawning, wait for balls to land
                StopSpawning();
                CheckLevelProgression();
            }
        }

        private void HandleBallLanded(int bucketIndex)
        {
            Debug.Log($"[GAME] Ball landed in Bucket {bucketIndex}");

            _batchProcessor.AddBallLanding(bucketIndex);
        }

        private void HandleBallRefunded()
        {
            _currentBallCount++;
            _ballsDroppedThisLevel--;

            Debug.Log($"[GAME] Ball refunded (out of bounds) | Remaining: {_currentBallCount} | Progress: {_ballsDroppedThisLevel}/{_ballAmount}");

            OnBallCountChanged?.Invoke(_currentBallCount);
        }

        private void HandleBatchProcessed(float walletBalance, float rewardEarned)
        {
            OnWalletUpdated?.Invoke(walletBalance);
        }

        #endregion

        #region Level Progression

        private async void CheckLevelProgression()
        {
            if (_isLevelingUp)
            {
                Debug.LogWarning("[GAME] Level up already in progress, ignoring...");
                return;
            }

            _isLevelingUp = true;

            LevelProgressionResponse response = await backend.CheckLevelProgression(_ballsDroppedThisLevel);

            if (response.Success && response.ShouldLevelUp)
            {
                await LevelUp();
            }

            _isLevelingUp = false;
        }

        private async Task LevelUp()
        {
            Debug.Log($"[GAME] Leveled up! Current level is {_currentLevel} with {_ballAmount} balls.");

            ChangeState(GameState.LevelTransition);
            StopSpawning();

            _batchProcessor.FlushBatch();
            await WaitForActiveBalls();
            _batchProcessor.FlushBatch();

            LevelAdvanceResponse response = await backend.AdvanceLevel();

            if (!response.Success)
            {
                Debug.LogError($"[GAME] Level advance failed: {response.Message}");
                ChangeState(GameState.Ready);
                return;
            }

            _currentLevel = response.NewLevel;
            _currentBallCount = response.NewBallAmount;
            _ballsDroppedThisLevel = 0;

            await LoadLevel(_currentLevel);

            OnLevelChanged?.Invoke(_currentLevel);
            OnBallCountChanged?.Invoke(_currentBallCount);

            await Task.Delay(1000);

            ChangeState(GameState.Ready);

            Debug.Log($"[GAME] Advanced to level {_currentLevel} | New ball amount: {_ballAmount}");
        }

        private async Task WaitForActiveBalls()
        {
            const int maxWaitTime = 8000;
            const int checkInterval = 100;

            int totalWaitTime = 0;
            int initialActive = spawner.ActiveBallCount;

            if (initialActive == 0)
            {
                Debug.Log("[GAME] No active balls to wait for.");
                return;
            }

            Debug.Log($"[GAME] Waiting for {initialActive} active balls...");

            while (spawner.HasActiveBalls() && totalWaitTime < maxWaitTime)
            {
                await Task.Delay(checkInterval);
                totalWaitTime += checkInterval;

                if (totalWaitTime % 1000 == 0)
                {
                    Debug.Log($"[GAME] Waiting... {spawner.ActiveBallCount} balls active. (Total Wait Time: {totalWaitTime}ms)");
                }
            }

            if (spawner.HasActiveBalls())
            {
                Debug.LogWarning($"[GAME] Timeout! Force clearing {spawner.ActiveBallCount} stuck balls.");
                spawner.ResetSpawner();
            }
            else
            {
                Debug.Log($"[GAME] All balls settled in {totalWaitTime}ms.");
            }
        }

        #endregion

        #region Reset

        private async void HandleResetNeeded()
        {
            await PerformReset();
        }

        private async Task PerformReset()
        {
            ChangeState(GameState.Resetting);

            StopSpawning();
            _batchProcessor.FlushBatch();

            await WaitForActiveBalls();

            _batchProcessor.FlushBatch();

            _sessionManager.StopMonitoring();

            ResetResponse response = await backend.ResetGame();

            if (!response.Success)
            {
                Debug.LogError($"[GAME] Failed to reset: {response.Message}");
                ChangeState(GameState.Ready);
                return;
            }

            _currentBallCount = response.BallCount;
            _currentLevel = response.CurrentLevel;
            _ballsDroppedThisLevel = 0;

            spawner.ResetSpawner();

            await LoadLevel(_currentLevel);

            _sessionManager.StartMonitoring();

            NotifyUI(response.WalletBalance);

            ChangeState(GameState.Ready);

            Debug.Log($"[GAME] Reset complete. Wallet preserved: {response.WalletBalance:F2}");
        }

        public async void ManualReset()
        {
            Debug.Log("[GAME] Manual reset triggered.");
            await PerformReset();
        }

        #endregion

        #region UI Notifications

        private void NotifyUI(float walletBalance)
        {
            OnBallCountChanged?.Invoke(_currentBallCount);
            OnLevelChanged?.Invoke(_currentLevel);
            OnWalletUpdated?.Invoke(walletBalance);
        }

        #endregion
    }
}