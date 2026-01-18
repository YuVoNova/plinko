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
        
        private float _minSpawnInterval = 0.1f;
        private float _spawnPositionVariance = 0f;
        private int _maxSimultaneousBalls = 30;
        
        private Vector2 _baseSpawnPosition;
        private float _lastSpawnTime;
        private int _currentActiveBalls;
        private Coroutine _spawnCoroutine;
        
        public Action<int> OnBallLanded;
        
        public bool IsSpawning => _spawnCoroutine != null;
        
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
                ball.EnableTrailRenderer(true);
                _currentActiveBalls++;
                _lastSpawnTime = Time.time;
            }
        }
        
        private void HandleBallLanded(PlinkoBall ball, int bucketIndex)
        {
            ball.OnBucketEntered -= HandleBallLanded;
            _currentActiveBalls--;
            
            OnBallLanded?.Invoke(bucketIndex);
            
            ballPool.ReturnBall(ball);
            
            PlinkoBucket bucket = board.GetBucket(bucketIndex);
            bucket?.TriggerHitEffect();
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