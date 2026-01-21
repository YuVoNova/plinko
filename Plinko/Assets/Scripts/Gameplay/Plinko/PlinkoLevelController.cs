using System;
using System.Threading.Tasks;
using Backend;
using Data;
using UnityEngine;

namespace Gameplay
{
    public class PlinkoLevelController
    {
        public Action<int> OnLevelChanged;
        public Action OnLevelUpStarted;
        public Action OnLevelUpCompleted;

        private readonly IBackendService _backend;
        private readonly PlinkoBoard _board;

        private int _currentLevel;
        private int _ballsRequiredForLevel;
        private bool _isLevelingUp;

        public int CurrentLevel => _currentLevel;
        public int BallsRequiredForLevel => _ballsRequiredForLevel;
        public bool IsLevelingUp => _isLevelingUp;

        public PlinkoLevelController(IBackendService backend, PlinkoBoard board)
        {
            _backend = backend;
            _board = board;
        }

        #region Initialization

        public void Initialize(int level)
        {
            _currentLevel = level;
            _isLevelingUp = false;
        }

        public async Task<LevelLoadResult> LoadLevel(int level)
        {
            LevelConfigResponse response = await _backend.GetLevelConfig(level);

            if (!response.Success || response.LevelConfig == null)
            {
                Debug.LogError($"[LEVEL] Failed to load level: {response.Message}");
                return new LevelLoadResult { Success = false };
            }

            _ballsRequiredForLevel = response.BallAmount;
            _board.ConfigureBucketsFromServer(response.LevelConfig);

            Debug.Log($"[LEVEL] Level {level} loaded | Balls for this level: {_ballsRequiredForLevel}");

            return new LevelLoadResult
            {
                Success = true,
                BallAmount = response.BallAmount
            };
        }

        #endregion

        #region Level Progression

        public async Task<bool> CheckAndProcessLevelUp(int ballsDropped, Func<Task> onBeforeLevelUp)
        {
            if (_isLevelingUp)
            {
                Debug.LogWarning("[LEVEL] Level up already in progress, ignoring...");
                return false;
            }

            _isLevelingUp = true;

            LevelProgressionResponse response = await _backend.CheckLevelProgression(ballsDropped);

            if (response.Success && response.ShouldLevelUp)
            {
                await PerformLevelUp(onBeforeLevelUp);
                _isLevelingUp = false;
                return true;
            }

            _isLevelingUp = false;
            return false;
        }

        private async Task PerformLevelUp(Func<Task> onBeforeLevelUp)
        {
            Debug.Log($"[LEVEL] Leveling up from level {_currentLevel}...");

            OnLevelUpStarted?.Invoke();

            // Let GameManager handle flush and wait
            if (onBeforeLevelUp != null)
                await onBeforeLevelUp();

            LevelAdvanceResponse response = await _backend.AdvanceLevel();

            if (!response.Success)
            {
                Debug.LogError($"[LEVEL] Level advance failed: {response.Message}");
                OnLevelUpCompleted?.Invoke();
                return;
            }

            _currentLevel = response.NewLevel;

            await LoadLevel(_currentLevel);

            OnLevelChanged?.Invoke(_currentLevel);

            // Brief delay for visual feedback
            await Task.Delay(1000);

            OnLevelUpCompleted?.Invoke();

            Debug.Log($"[LEVEL] Advanced to level {_currentLevel} | Ball amount: {_ballsRequiredForLevel}");
        }

        #endregion
    }

    public struct LevelLoadResult
    {
        public bool Success;
        public int BallAmount;
    }
}