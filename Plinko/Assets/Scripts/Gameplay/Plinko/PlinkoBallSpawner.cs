using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Gameplay
{
    public class PlinkoBallSpawner : MonoBehaviour
    {
        [SerializeField] private PlinkoBallPool ballPool;
        [SerializeField] private PlinkoBoard board;
        [SerializeField] private Transform spawnPoint;
        
        // TODO -> Move hard-coded values to an SO Config File
        private float _minSpawnInterval = 0.1f;
        private float _spawnPositionVariance = 0f;
        private int _maxSimultaneousBalls = 30;
        
        private Vector2 _baseSpawnPosition;
        private float _lastSpawnTime;
        private int _currentActiveBalls;
        private Coroutine _spawnCoroutine;
        
        public Action<int> OnBallLanded;
        public Action OnBallSpawned;
        public Action OnBallRefunded;
        
        public bool IsSpawning => _spawnCoroutine != null;
        public int ActiveBallCount => _currentActiveBalls;
        
        public void StartSpawning()
        {
            if (_spawnCoroutine != null)
                return;
            
            _spawnCoroutine = StartCoroutine(SpawnContinuously());
        }
        
        public void StopSpawning()
        {
            if (_spawnCoroutine != null)
            {
                StopCoroutine(_spawnCoroutine);
                _spawnCoroutine = null;
            }
        }
        
        public void ResetSpawner()
        {
            StopSpawning();
            ballPool.ReturnAllBalls();
            _currentActiveBalls = 0;
        }

        public void SetSpawnPosition(Vector2 position)
        {
            _baseSpawnPosition = position;
            spawnPoint.position = _baseSpawnPosition;
        }
        
        public bool HasActiveBalls()
        {
            return _currentActiveBalls > 0;
        }

        private bool TrySpawnBall()
        {
            if (Time.time - _lastSpawnTime < _minSpawnInterval)
                return false;
            
            if (_currentActiveBalls >= _maxSimultaneousBalls)
                return false;
            
            SpawnBall();
            return true;
        }
        
        private void SpawnBall()
        {
            float xVariation = Random.Range(-_spawnPositionVariance, _spawnPositionVariance);
            Vector2 spawnPos = _baseSpawnPosition + new Vector2(xVariation, 0);
            
            PlinkoBall ball = ballPool.GetBall(spawnPos);
            
            if (ball != null)
            {
                ball.OnBucketEntered += HandleBallLanded;
                ball.OnDespawnedWithoutLanding += HandleBallOutOfBounds;
                ball.EnableTrailRenderer(true);
                _currentActiveBalls++;
                _lastSpawnTime = Time.time;
                
                // Notify that ball was spawned (client decrements here)
                OnBallSpawned?.Invoke();  // ← NEW

            }
        }
        
        private void HandleBallLanded(PlinkoBall ball, int bucketIndex)
        {
            ball.OnBucketEntered -= HandleBallLanded;
            ball.OnDespawnedWithoutLanding -= HandleBallOutOfBounds;
            _currentActiveBalls--;
            
            OnBallLanded?.Invoke(bucketIndex);
            
            ballPool.ReturnBall(ball);
            
            PlinkoBucket bucket = board.GetBucket(bucketIndex);
            bucket?.TriggerHitEffect();
        }
        
        private void HandleBallOutOfBounds(PlinkoBall ball)
        {
            ball.OnBucketEntered -= HandleBallLanded;
            ball.OnDespawnedWithoutLanding -= HandleBallOutOfBounds;
            _currentActiveBalls--;
    
            // Notify game manager to refund the ball
            OnBallRefunded?.Invoke();
    
            ballPool.ReturnBall(ball);
    
            Debug.LogWarning($"[SPAWNER] Ball out of bounds, refunded (active: {_currentActiveBalls})");
        }
        
        private IEnumerator SpawnContinuously()
        {
            while (true)
            {
                TrySpawnBall();
                yield return null;
            }
        }
    }
}