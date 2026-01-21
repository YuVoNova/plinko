using System;
using Data;
using UnityEngine;

namespace Backend
{
    public class PlayerDataStore
    {
        private const string PLAYER_DATA_KEY = "PlinkoPlayerData";

        private PlayerData _playerData;
        private PlayerSession _currentSession;

        private readonly LevelConfigDatabase _levelDatabase;
        private readonly Action<string> _logAction;

        public PlayerData PlayerData => _playerData;
        public PlayerSession CurrentSession => _currentSession;
        public bool HasSession => _currentSession != null;

        public PlayerDataStore(LevelConfigDatabase levelDatabase, Action<string> logAction = null)
        {
            _levelDatabase = levelDatabase;
            _logAction = logAction;
        }

        #region Session Management

        public void CreateSession(PlayerData playerData)
        {
            _playerData = playerData;

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

            Log("Session created");
        }

        public void UpdateSessionAfterBatch(float rewardEarned)
        {
            _currentSession.WalletBalance += rewardEarned;
            _currentSession.TotalBatchesProcessed++;
            _currentSession.LastBatchTime = DateTime.UtcNow;

            Save();
        }

        public void UpdateSessionLevel(int newLevel, int newBallCount)
        {
            _currentSession.CurrentLevel = newLevel;
            _currentSession.CurrentBallCount = newBallCount;
            _currentSession.TotalBallsDroppedThisLevel = 0;
        }

        public void UpdateBallsDropped(int ballsDropped)
        {
            _currentSession.TotalBallsDroppedThisLevel = ballsDropped;
        }

        public void AddBalance(float amount)
        {
            _currentSession.WalletBalance += amount;
            Save();
        }

        public void ResetSession(bool isFullReset)
        {
            if (isFullReset)
            {
                _playerData.FullReset();
                Delete();
            }
            else
            {
                _playerData.ResetSession();
                Save();
            }

            LevelConfigServerData firstLevelConfig = _levelDatabase.GetConfig(1);
            _currentSession.CurrentBallCount = firstLevelConfig.BallAmount;
            _currentSession.CurrentLevel = firstLevelConfig.Level;
            _currentSession.TotalBallsDroppedThisLevel = 0;
        }

        #endregion

        #region History

        public void AddHistoryEntry(BallResultData entry)
        {
            _playerData.AddHistoryEntry(entry);
            Save();
        }

        public void AddHistoryEntries(System.Collections.Generic.List<BallResultData> entries)
        {
            _playerData.AddHistoryEntry(entries);
            Save();
        }

        #endregion

        #region Persistence

        public PlayerData Load()
        {
            if (PlayerPrefs.HasKey(PLAYER_DATA_KEY))
            {
                string json = PlayerPrefs.GetString(PLAYER_DATA_KEY);
                PlayerData data = JsonUtility.FromJson<PlayerData>(json);
                Log("Player data loaded");
                return data;
            }

            Log("No saved data, creating new PlayerData");
            return new PlayerData();
        }

        public void Save()
        {
            if (_playerData == null)
                return;

            string json = JsonUtility.ToJson(_playerData);
            PlayerPrefs.SetString(PLAYER_DATA_KEY, json);
            PlayerPrefs.Save();

            Log("Player data saved");
        }

        public void Delete()
        {
            PlayerPrefs.DeleteKey(PLAYER_DATA_KEY);
            PlayerPrefs.Save();
            Log("Player data deleted");
        }

        #endregion

        #region Helpers

        private void Log(string message)
        {
            _logAction?.Invoke(message);
        }

        #endregion
    }
}