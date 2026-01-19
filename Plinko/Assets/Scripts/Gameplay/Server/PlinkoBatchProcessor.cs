using System;
using System.Collections.Generic;
using UnityEngine;
using Backend;

namespace Gameplay
{
    public class PlinkoBatchProcessor
    {
        private const int BATCH_SIZE = 5;
        private const float BATCH_TIMEOUT = 2f;
        
        private readonly MockServerService _backend;
        private readonly List<int> _pendingBallLandings = new List<int>();
        private float _lastBatchTime;
        
        // Events
        public Action<float, float> OnBatchProcessed;   // Wallet Balance, Reward Earned
        public Action OnBatchStarted;
        public Action OnBatchCompleted;
        
        public PlinkoBatchProcessor(MockServerService backend)
        {
            _backend = backend;
            _lastBatchTime = Time.time;
        }
        
        public void AddBallLanding(int bucketIndex)
        {
            _pendingBallLandings.Add(bucketIndex);
            
            if (_pendingBallLandings.Count >= BATCH_SIZE)
            {
                SendBatch();
            }
        }
        
        public void CheckTimeout()
        {
            if (_pendingBallLandings.Count > 0 && Time.time - _lastBatchTime > BATCH_TIMEOUT)
            {
                SendBatch();
            }
        }
        
        public void FlushBatch()
        {
            if (_pendingBallLandings.Count > 0)
            {
                SendBatch();
            }
        }
        
        private async void SendBatch()
        {
            if (_pendingBallLandings.Count == 0) return;
    
            List<int> batch = new List<int>(_pendingBallLandings);
            _pendingBallLandings.Clear();
            _lastBatchTime = Time.time;
    
            OnBatchStarted?.Invoke();
    
            RewardResponse response = await _backend.ProcessBallLandings(batch.ToArray());
    
            OnBatchCompleted?.Invoke();
    
            if (response.Success)
            {
                OnBatchProcessed?.Invoke(response.WalletBalance, response.RewardEarned);
        
                Debug.Log($"[BATCH] Processed {batch.Count} balls | Reward: +{response.RewardEarned:F2} | " +
                          $"Balance: {response.WalletBalance:F2} | Buckets: [{string.Join(", ", batch)}]");
            }
            else
            {
                Debug.LogError($"[BATCH] Failed: {response.Message}");
            }
        }
    }
}