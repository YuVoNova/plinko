using System.Collections.Generic;
using System.Threading.Tasks;
using Data;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Backend
{
    public class MockServerService : MonoBehaviour, IBackendService
    {
        private const int MIN_LATENCY_MS = 50;
        private const int MAX_LATENCY_MS = 150;

        [SerializeField] private bool enableLogMessages = true;

        private PlayerDataStore _dataStore;
        private LevelConfigDatabase _levelDatabase;
        private SessionValidator _sessionValidator;
        private RewardValidator _rewardValidator;

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeServer();
        }

        private void InitializeServer()
        {
            _levelDatabase = new LevelConfigDatabase();
            _sessionValidator = new SessionValidator(_levelDatabase);
            _rewardValidator = new RewardValidator(_levelDatabase);
            _dataStore = new PlayerDataStore(_levelDatabase, LogMessage);

            LogMessage("Server initialized");
        }

        #endregion

        #region Game Initialization

        public async Task<InitResponse> InitializeGame()
        {
            await SimulateNetworkDelay();

            PlayerData playerData = _dataStore.Load();

            float timeUntilReset = _sessionValidator.GetTimeUntilReset(playerData.GetSessionStartTime());
            bool needsSessionReset = timeUntilReset <= 0;

            if (needsSessionReset)
            {
                playerData.ResetSession();
                timeUntilReset = SessionValidator.RESET_INTERVAL_SECONDS;
            }

            _dataStore.CreateSession(playerData);

            if (needsSessionReset)
                _dataStore.Save();

            LogMessage($"Game initialized. Wallet: {playerData.WalletBalance:F2}, Timer: {timeUntilReset:F0}s remaining");

            return new InitResponse
            {
                Success = true,
                BallCount = _dataStore.CurrentSession.CurrentBallCount,
                CurrentLevel = _dataStore.CurrentSession.CurrentLevel,
                WalletBalance = playerData.WalletBalance,
                TimeUntilReset = timeUntilReset,
                RewardHistory = new List<BallResultData>(playerData.RewardHistory),
                Message = "Game initialized"
            };
        }

        #endregion

        #region Level Management

        public async Task<LevelConfigResponse> GetLevelConfig(int level)
        {
            await SimulateNetworkDelay();

            LevelConfigServerData serverConfig = _levelDatabase.GetConfig(level);
            LevelConfig clientConfig = serverConfig.ToClientConfig();

            LogMessage($"Level {level} config sent (gives {serverConfig.BallAmount} balls)");

            return new LevelConfigResponse
            {
                Success = true,
                LevelConfig = clientConfig,
                BallAmount = serverConfig.BallAmount,
                Message = $"Level {level} config"
            };
        }

        public async Task<LevelProgressionResponse> CheckLevelProgression(int clientBallsDropped)
        {
            await SimulateNetworkDelay();

            if (!_dataStore.HasSession)
                return new LevelProgressionResponse { Success = false, Message = "No session" };

            _dataStore.UpdateBallsDropped(clientBallsDropped);

            LevelConfigServerData currentConfig = _levelDatabase.GetConfig(_dataStore.CurrentSession.CurrentLevel);
            bool shouldLevelUp = clientBallsDropped >= currentConfig.BallAmount;

            LogMessage($"Progression check: {clientBallsDropped}/{currentConfig.BallAmount} balls dropped");

            return new LevelProgressionResponse
            {
                Success = true,
                ShouldLevelUp = shouldLevelUp,
                BallsDropped = clientBallsDropped,
                BallsRequired = currentConfig.BallAmount,
                Message = shouldLevelUp ? "Ready to level up!" : "Keep playing"
            };
        }

        public async Task<LevelAdvanceResponse> AdvanceLevel()
        {
            await SimulateNetworkDelay();

            if (!_dataStore.HasSession)
                return new LevelAdvanceResponse { Success = false, Message = "No session" };

            int currentLevel = _dataStore.CurrentSession.CurrentLevel;
            int maxLevel = _levelDatabase.GetMaxLevel();

            if (currentLevel >= maxLevel)
            {
                LogMessage($"Already at max level {maxLevel}");
                return new LevelAdvanceResponse
                {
                    Success = true,
                    NewLevel = currentLevel,
                    NewBallAmount = _dataStore.CurrentSession.CurrentBallCount,
                    Message = "Max level reached"
                };
            }

            int newLevel = currentLevel + 1;
            LevelConfigServerData newLevelConfig = _levelDatabase.GetConfig(newLevel);

            _dataStore.UpdateSessionLevel(newLevel, newLevelConfig.BallAmount);

            LogMessage($"Level advanced to {newLevel}, granted {newLevelConfig.BallAmount} balls");

            return new LevelAdvanceResponse
            {
                Success = true,
                NewLevel = newLevel,
                NewBallAmount = newLevelConfig.BallAmount,
                Message = $"Advanced to level {newLevel}"
            };
        }

        #endregion

        #region Reward Processing

        public async Task<RewardResponse> ProcessBallLandings(int[] bucketIndices)
        {
            await SimulateNetworkDelay();

            if (!_dataStore.HasSession)
                return new RewardResponse { Success = false, Message = "No active session" };

            if (!_sessionValidator.ValidateBatchRequest(_dataStore.CurrentSession, bucketIndices, out string error))
                return new RewardResponse { Success = false, Message = $"Validation failed: {error}" };

            float reward = _rewardValidator.CalculateReward(_dataStore.CurrentSession.CurrentLevel, bucketIndices);

            _dataStore.UpdateSessionAfterBatch(reward);

            LogMessage($"Batch processed: {bucketIndices.Length} balls, +{reward:F2} coins, Balance: {_dataStore.CurrentSession.WalletBalance:F2}");

            return new RewardResponse
            {
                Success = true,
                WalletBalance = _dataStore.CurrentSession.WalletBalance,
                RewardEarned = reward,
                BallsProcessed = bucketIndices.Length,
                Message = "Rewards calculated"
            };
        }

        public async Task<List<BallResultData>> GetBatchResults(BallLandData[] ballLandDataArray)
        {
            await SimulateNetworkDelay();

            if (!_dataStore.HasSession)
                return new List<BallResultData>();

            LevelConfigServerData config = _levelDatabase.GetConfig(_dataStore.CurrentSession.CurrentLevel);
            List<BallResultData> breakdown = new List<BallResultData>();

            foreach (BallLandData ballLandData in ballLandDataArray)
            {
                if (ballLandData.BucketIndex < 0 || ballLandData.BucketIndex >= config.Multipliers.Length)
                    continue;

                float multiplier = config.Multipliers[ballLandData.BucketIndex];
                float reward = config.BaseRewardPerBall * multiplier;

                breakdown.Add(new BallResultData(ballLandData.DropNumber, ballLandData.BucketIndex, multiplier, reward));
            }

            _dataStore.AddHistoryEntries(breakdown);

            return breakdown;
        }

        public async Task<float> AddBalance(float amount)
        {
            await SimulateNetworkDelay();

            if (!_dataStore.HasSession)
                return 0f;

            _dataStore.AddBalance(amount);

            LogMessage($"Balance added: +{amount:F2}, New balance: {_dataStore.CurrentSession.WalletBalance:F2}");

            return _dataStore.CurrentSession.WalletBalance;
        }

        #endregion

        #region Session Management

        public async Task<SessionCheckResponse> CheckSessionStatus()
        {
            await SimulateNetworkDelay();

            if (!_dataStore.HasSession)
                return new SessionCheckResponse { Success = false, Message = "No session" };

            bool needsReset = _sessionValidator.NeedsReset(_dataStore.CurrentSession);
            float timeUntilReset = _sessionValidator.GetTimeUntilReset(_dataStore.CurrentSession);

            return new SessionCheckResponse
            {
                Success = true,
                NeedsReset = needsReset,
                TimeUntilReset = timeUntilReset,
                Message = needsReset ? "Session expired" : $"{timeUntilReset:F0}s remaining"
            };
        }

        public async Task<ResetResponse> ResetGame(bool isFullReset)
        {
            await SimulateNetworkDelay();

            if (!_dataStore.HasSession)
                return new ResetResponse { Success = false, Message = "No session" };

            _dataStore.ResetSession(isFullReset);

            string message = isFullReset
                ? "Full reset. Wallet cleared."
                : $"Game reset. Wallet preserved: {_dataStore.CurrentSession.WalletBalance:F2}";

            LogMessage(message);

            return new ResetResponse
            {
                Success = true,
                BallCount = _dataStore.CurrentSession.CurrentBallCount,
                CurrentLevel = _dataStore.CurrentSession.CurrentLevel,
                WalletBalance = _dataStore.CurrentSession.WalletBalance,
                TimeUntilReset = SessionValidator.RESET_INTERVAL_SECONDS,
                Message = message
            };
        }

        #endregion

        #region Utilities

        private async Task SimulateNetworkDelay()
        {
            int delay = Random.Range(MIN_LATENCY_MS, MAX_LATENCY_MS);
            await Task.Delay(delay);
        }

        private void LogMessage(string message)
        {
            if (enableLogMessages)
                Debug.Log($"[SERVER] {message}");
        }

        #endregion
    }
}