using System;
using System.Collections.Generic;
using UnityEngine;
using Backend;
using Data;

namespace Gameplay
{
    public class PlinkoBatchProcessor
    {
        private const int BATCH_SIZE = 5;
        private const float BATCH_TIMEOUT = 2f;

        public Action<float, float> OnBatchProcessed; // Wallet Balance, Reward Earned
        public Action OnBatchStarted;
        public Action OnBatchCompleted;
        public Action<List<BallResultData>> OnBallResult;
        
        private readonly IBackendService _backend;
        private readonly List<BallLandData> _pendingBallLandings = new List<BallLandData>();
        private float _lastBatchTime;
        private int _totalDroppedBallCount;

        public PlinkoBatchProcessor(IBackendService backend, int totalDroppedBallCount)
        {
            _backend = backend;
            _totalDroppedBallCount = totalDroppedBallCount;
            _lastBatchTime = Time.time;
        }

        public void AddBallLanding(int bucketIndex)
        {
            _totalDroppedBallCount++;
            _pendingBallLandings.Add(new BallLandData(_totalDroppedBallCount, bucketIndex));

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

        public void Reset()
        {
            _pendingBallLandings.Clear();
            _totalDroppedBallCount = 0;
            _lastBatchTime = Time.time;
        }

        private async void SendBatch()
        {
            if (_pendingBallLandings.Count == 0)
                return;

            List<BallLandData> batch = new List<BallLandData>(_pendingBallLandings);
            _pendingBallLandings.Clear();
            _lastBatchTime = Time.time;
            
            int[] bucketIndices = new int[batch.Count];
            for (int i = 0; i < bucketIndices.Length; i++)
            {
                bucketIndices[i] = batch[i].BucketIndex;
            }

            OnBatchStarted?.Invoke();

            RewardResponse response = await _backend.ProcessBallLandings(bucketIndices);

            OnBatchCompleted?.Invoke();

            if (response.Success)
            {
                OnBatchProcessed?.Invoke(response.WalletBalance, response.RewardEarned);
                
                List<BallResultData> ballResults = await _backend.GetBatchResults(batch.ToArray());
                OnBallResult?.Invoke(ballResults);

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