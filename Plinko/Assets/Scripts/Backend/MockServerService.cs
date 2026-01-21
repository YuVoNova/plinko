using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Data;
using Random = UnityEngine.Random;

namespace Backend
{
    public class MockServerService : MonoBehaviour
    {
        private const int MIN_LATENCY_MS = 50;
        private const int MAX_LATENCY_MS = 150;

        [SerializeField] private bool enableLogMessages = true;

        private PlayerSession _currentSession;
        private LevelConfigDatabase _levelDatabase;
        private SessionValidator _sessionValidator;
        private RewardValidator _rewardValidator;

        private void Awake()
        {
            InitializeServer();
        }

        private void InitializeServer()
        {
            _levelDatabase = new LevelConfigDatabase();
            _sessionValidator = new SessionValidator(_levelDatabase);
            _rewardValidator = new RewardValidator(_levelDatabase);

            LogMessage("Server initialized");
        }

        public async Task<InitResponse> InitializeGame()
        {
            await SimulateNetworkDelay();

            _currentSession = new PlayerSession();

            LogMessage($"New session created: {_currentSession.SessionId}");

            return new InitResponse
            {
                Success = true,
                BallCount = _currentSession.CurrentBallCount,
                CurrentLevel = _currentSession.CurrentLevel,
                WalletBalance = _currentSession.WalletBalance,
                ServerTime = GetServerTimestamp(),
                Message = "Game initialized"
            };
        }

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

        public async Task<RewardResponse> ProcessBallLandings(int[] bucketIndices)
        {
            await SimulateNetworkDelay();

            if (_currentSession == null)
            {
                return new RewardResponse
                {
                    Success = false,
                    Message = "No active session"
                };
            }

            if (!_sessionValidator.ValidateBatchRequest(_currentSession, bucketIndices, out string error))
            {
                return new RewardResponse
                {
                    Success = false,
                    Message = $"Validation failed: {error}"
                };
            }

            float reward = _rewardValidator.CalculateReward(_currentSession.CurrentLevel, bucketIndices);

            _currentSession.WalletBalance += reward;
            _currentSession.TotalBatchesProcessed++;
            _currentSession.LastBatchTime = DateTime.UtcNow;

            LogMessage(
                $"Batch processed: {bucketIndices.Length} balls, +{reward:F2} coins, Balance: {_currentSession.WalletBalance:F2}");

            return new RewardResponse
            {
                Success = true,
                WalletBalance = _currentSession.WalletBalance,
                RewardEarned = reward,
                BallsProcessed = bucketIndices.Length,
                Message = "Rewards calculated"
            };
        }

        public async Task<LevelProgressionResponse> CheckLevelProgression(int clientBallsDropped)
        {
            await SimulateNetworkDelay();

            if (_currentSession == null)
            {
                return new LevelProgressionResponse { Success = false, Message = "No session" };
            }

            _currentSession.TotalBallsDroppedThisLevel = clientBallsDropped;

            LevelConfigServerData currentConfig = _levelDatabase.GetConfig(_currentSession.CurrentLevel);

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

            if (_currentSession == null)
            {
                return new LevelAdvanceResponse { Success = false, Message = "No session" };
            }

            int maxLevel = _levelDatabase.GetMaxLevel();

            if (_currentSession.CurrentLevel >= maxLevel)
            {
                LogMessage($"Already at max level {maxLevel}");
                return new LevelAdvanceResponse
                {
                    Success = true,
                    NewLevel = _currentSession.CurrentLevel,
                    NewBallAmount = _currentSession.CurrentBallCount,
                    Message = "Max level reached"
                };
            }

            _currentSession.CurrentLevel++;
            _currentSession.TotalBallsDroppedThisLevel = 0;

            LevelConfigServerData newLevelConfig = _levelDatabase.GetConfig(_currentSession.CurrentLevel);
            _currentSession.CurrentBallCount = newLevelConfig.BallAmount;

            LogMessage($"Level advanced to {_currentSession.CurrentLevel}, granted {newLevelConfig.BallAmount} balls");

            return new LevelAdvanceResponse
            {
                Success = true,
                NewLevel = _currentSession.CurrentLevel,
                NewBallAmount = _currentSession.CurrentBallCount,
                Message = $"Advanced to level {_currentSession.CurrentLevel}"
            };
        }

        public async Task<SessionCheckResponse> CheckSessionStatus()
        {
            await SimulateNetworkDelay();

            if (_currentSession == null)
            {
                return new SessionCheckResponse { Success = false, Message = "No session" };
            }

            bool needsReset = _sessionValidator.NeedsReset(_currentSession);
            float timeUntilReset = _sessionValidator.GetTimeUntilReset(_currentSession);

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

            if (_currentSession == null)
            {
                return new ResetResponse { Success = false, Message = "No session" };
            }
            
            if (isFullReset)
                _currentSession.WalletBalance = 0f;

            LevelConfigServerData firstLevelConfig = _levelDatabase.GetConfig(1);
            _currentSession.CurrentBallCount = firstLevelConfig.BallAmount;
            _currentSession.CurrentLevel = firstLevelConfig.Level;
            _currentSession.TotalBallsDroppedThisLevel = 0;
            _currentSession.LastResetTime = DateTime.UtcNow;

            string message = isFullReset ? "Full reset. Wallet cleared." : $"Game reset. Wallet preserved: {_currentSession.WalletBalance:F2}";
            LogMessage(message);

            return new ResetResponse
            {
                Success = true,
                BallCount = _currentSession.CurrentBallCount,
                CurrentLevel = _currentSession.CurrentLevel,
                WalletBalance = _currentSession.WalletBalance,
                Message = message
            };
        }
        
        public async Task<ResetResponse> FullReset()
        {
            await SimulateNetworkDelay();

            if (_currentSession == null)
                return new ResetResponse { Success = false, Message = "No session" };

            // Reset wallet to 0
            _currentSession.WalletBalance = 0f;

            LevelConfigServerData firstLevelConfig = _levelDatabase.GetConfig(1);
            _currentSession.CurrentBallCount = firstLevelConfig.BallAmount;
            _currentSession.CurrentLevel = firstLevelConfig.Level;
            _currentSession.TotalBallsDroppedThisLevel = 0;
            _currentSession.LastResetTime = DateTime.UtcNow;

            LogMessage("Full reset. Wallet cleared.");

            return new ResetResponse
            {
                Success = true,
                BallCount = _currentSession.CurrentBallCount,
                CurrentLevel = _currentSession.CurrentLevel,
                WalletBalance = 0f,
                Message = "Full reset complete"
            };
        }

        public async Task<List<BallResultData>> GetBatchResults(BallLandData[] ballLandDataArray)
        {
            await SimulateNetworkDelay();

            if (_currentSession == null)
                return new List<BallResultData>();

            LevelConfigServerData config = _levelDatabase.GetConfig(_currentSession.CurrentLevel);
            List<BallResultData> breakdown = new List<BallResultData>();

            foreach (BallLandData ballLandData in ballLandDataArray)
            {
                if (ballLandData.BucketIndex < 0 || ballLandData.BucketIndex >= config.Multipliers.Length)
                    continue;

                float multiplier = config.Multipliers[ballLandData.BucketIndex];
                float reward = config.BaseRewardPerBall * multiplier;

                breakdown.Add(new BallResultData(ballLandData.DropNumber, ballLandData.BucketIndex, multiplier, reward));
            }

            return breakdown;
        }

        private async Task SimulateNetworkDelay()
        {
            int delay = Random.Range(MIN_LATENCY_MS, MAX_LATENCY_MS);
            await Task.Delay(delay);
        }

        private long GetServerTimestamp()
        {
            // Using Unix Epoch difference as a timestamp ensures the time difference between the server's system clock to the client's.
            return (long)(DateTime.UtcNow - DateTime.UnixEpoch).TotalSeconds;
        }

        private void LogMessage(string message)
        {
            if (enableLogMessages)
                Debug.Log($"[SERVER] {message}");
        }
    }
}