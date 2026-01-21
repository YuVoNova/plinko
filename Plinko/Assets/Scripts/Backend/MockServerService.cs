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
        private const string PLAYER_DATA_KEY = "PlinkoPlayerData";
        private const int MIN_LATENCY_MS = 50;
        private const int MAX_LATENCY_MS = 150;

        [SerializeField] private bool enableLogMessages = true;

        private PlayerSession _currentSession;
        private PlayerData _playerData;
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

            _playerData = LoadPlayerData();
            
            float timeUntilReset = _sessionValidator.GetTimeUntilReset(_playerData.GetSessionStartTime());
            bool needsSessionReset = timeUntilReset <= 0;

            if (needsSessionReset)
            {
                _playerData.ResetSession();
                SavePlayerData();
                timeUntilReset = SessionValidator.RESET_INTERVAL_SECONDS;
            }
            
            LevelConfigServerData firstLevelConfig = _levelDatabase.GetConfig(1);
    
            _currentSession = new PlayerSession
            {
                SessionId = Guid.NewGuid().ToString(),
                CurrentLevel = 1,
                CurrentBallCount = firstLevelConfig.BallAmount,
                TotalBallsDroppedThisLevel = 0,
                LastBatchTime = DateTime.UtcNow,
                TotalBatchesProcessed = 0,
                PlayerData = _playerData
            };

            LogMessage($"Game initialized. Wallet: {_playerData.WalletBalance:F2}, Timer: {timeUntilReset:F0}s remaining");

            return new InitResponse
            {
                Success = true,
                BallCount = _currentSession.CurrentBallCount,
                CurrentLevel = _currentSession.CurrentLevel,
                WalletBalance = _playerData.WalletBalance,
                TimeUntilReset = timeUntilReset,
                RewardHistory = new List<BallResultData>(_playerData.RewardHistory),
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
            
            SavePlayerData();

            LogMessage($"Batch processed: {bucketIndices.Length} balls, +{reward:F2} coins, Balance: {_currentSession.WalletBalance:F2}");

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
            {
                _playerData.FullReset();
                DeletePlayerData();
            }
            else
            {
                _playerData.ResetSession();
                SavePlayerData();
            }

            LevelConfigServerData firstLevelConfig = _levelDatabase.GetConfig(1);
            _currentSession.CurrentBallCount = firstLevelConfig.BallAmount;
            _currentSession.CurrentLevel = firstLevelConfig.Level;
            _currentSession.TotalBallsDroppedThisLevel = 0;

            string message = isFullReset ? "Full reset. Wallet cleared." : $"Game reset. Wallet preserved: {_currentSession.WalletBalance:F2}";
            LogMessage(message);

            return new ResetResponse
            {
                Success = true,
                BallCount = _currentSession.CurrentBallCount,
                CurrentLevel = _currentSession.CurrentLevel,
                WalletBalance = _currentSession.WalletBalance,
                TimeUntilReset = SessionValidator.RESET_INTERVAL_SECONDS,
                Message = message
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

            _playerData.AddHistoryEntry(breakdown);
            SavePlayerData();
            
            return breakdown;
        }
        
        public async Task<float> AddBalance(float amount)
        {
            await SimulateNetworkDelay();
    
            if (_currentSession == null)
                return 0f;
    
            _currentSession.WalletBalance += amount;
            SavePlayerData();
    
            LogMessage($"Balance added: +{amount:F2}, New balance: {_currentSession.WalletBalance:F2}");
    
            return _currentSession.WalletBalance;
        }

        private async Task SimulateNetworkDelay()
        {
            int delay = Random.Range(MIN_LATENCY_MS, MAX_LATENCY_MS);
            await Task.Delay(delay);
        }
        
        private void SavePlayerData()
        {
            if (_playerData == null)
                return;
    
            string json = JsonUtility.ToJson(_playerData);
            PlayerPrefs.SetString(PLAYER_DATA_KEY, json);
            PlayerPrefs.Save();
    
            LogMessage("Player data saved");
        }

        private PlayerData LoadPlayerData()
        {
            if (PlayerPrefs.HasKey(PLAYER_DATA_KEY))
            {
                string json = PlayerPrefs.GetString(PLAYER_DATA_KEY);
                PlayerData data = JsonUtility.FromJson<PlayerData>(json);
                LogMessage("Player data loaded");
                return data;
            }
    
            LogMessage("No saved data, creating new PlayerData");
            return new PlayerData();
        }

        private void DeletePlayerData()
        {
            PlayerPrefs.DeleteKey(PLAYER_DATA_KEY);
            PlayerPrefs.Save();
            LogMessage("Player data deleted");
        }

        private void LogMessage(string message)
        {
            if (enableLogMessages)
                Debug.Log($"[SERVER] {message}");
        }
    }
}