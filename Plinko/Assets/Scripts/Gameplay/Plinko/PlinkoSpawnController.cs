using System;
using UnityEngine;

namespace Gameplay
{
    public class PlinkoSpawnController
    {
        public Action OnBallSpawned;
        public Action<int> OnBallCountChanged;
        public Action OnAllBallsSpawned;
        public Action OnLevelBallsComplete;

        private readonly PlinkoBallSpawner _spawner;
        
        private int _currentBallCount;
        private int _ballsDroppedThisLevel;
        private int _ballsRequiredForLevel;
        private bool _isSpawningLocked;

        public int CurrentBallCount => _currentBallCount;
        public int BallsDroppedThisLevel => _ballsDroppedThisLevel;
        public bool HasActiveBalls => _spawner.HasActiveBalls();
        public int ActiveBallCount => _spawner.ActiveBallCount;

        public PlinkoSpawnController(PlinkoBallSpawner spawner)
        {
            _spawner = spawner;
        }

        #region Initialization

        public void SubscribeToSpawner()
        {
            _spawner.OnBallSpawned += HandleBallSpawned;
            _spawner.OnBallRefunded += HandleBallRefunded;
        }

        public void UnsubscribeFromSpawner()
        {
            _spawner.OnBallSpawned -= HandleBallSpawned;
            _spawner.OnBallRefunded -= HandleBallRefunded;
        }

        public void SetSpawnPosition(Vector2 position)
        {
            _spawner.SetSpawnPosition(position);
        }

        #endregion

        #region State Management

        public void Initialize(int ballCount, int ballsRequiredForLevel)
        {
            _currentBallCount = ballCount;
            _ballsRequiredForLevel = ballsRequiredForLevel;
            _ballsDroppedThisLevel = 0;
            _isSpawningLocked = false;
        }

        public void SetBallsRequiredForLevel(int amount)
        {
            _ballsRequiredForLevel = amount;
        }

        public void SetBallCount(int count)
        {
            _currentBallCount = count;
            _ballsDroppedThisLevel = 0;
            OnBallCountChanged?.Invoke(_currentBallCount);
        }

        public void LockSpawning()
        {
            _isSpawningLocked = true;
        }

        public void UnlockSpawning()
        {
            _isSpawningLocked = false;
        }

        public void ResetSpawner()
        {
            _spawner.ResetSpawner();
        }

        #endregion

        #region Spawning Control

        public bool CanSpawn(bool isValidState, bool isLevelingUp)
        {
            if (!isValidState)
                return false;

            if (isLevelingUp)
                return false;

            if (_isSpawningLocked)
                return false;

            if (_currentBallCount <= 0)
                return false;

            return true;
        }

        public void StartSpawning()
        {
            _spawner.StartSpawning();
        }

        public void StopSpawning()
        {
            _spawner.StopSpawning();
        }

        #endregion

        #region Event Handlers

        private void HandleBallSpawned()
        {
            if (_currentBallCount <= 0)
            {
                Debug.LogError("[SPAWN] Spawned ball but count at 0!");
                StopSpawning();
                return;
            }

            _currentBallCount--;
            _ballsDroppedThisLevel++;

            Debug.Log($"[SPAWN] Ball spawned | Remaining: {_currentBallCount} | Progress: {_ballsDroppedThisLevel}/{_ballsRequiredForLevel}");

            OnBallCountChanged?.Invoke(_currentBallCount);
            OnBallSpawned?.Invoke();

            CheckSpawningComplete();
        }

        private void HandleBallRefunded()
        {
            _currentBallCount++;
            _ballsDroppedThisLevel--;

            Debug.Log($"[SPAWN] Ball refunded | Remaining: {_currentBallCount} | Progress: {_ballsDroppedThisLevel}/{_ballsRequiredForLevel}");

            OnBallCountChanged?.Invoke(_currentBallCount);
        }

        private void CheckSpawningComplete()
        {
            if (_currentBallCount <= 0)
            {
                StopSpawning();
                Debug.Log("[SPAWN] All balls have spawned! Waiting for them to land...");
                OnAllBallsSpawned?.Invoke();
            }

            if (_ballsDroppedThisLevel >= _ballsRequiredForLevel)
            {
                StopSpawning();
                OnLevelBallsComplete?.Invoke();
            }
        }

        #endregion
    }
}