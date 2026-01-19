using System;
using UnityEngine;

namespace Backend
{
    public class SessionValidator
    {
        private const float RESET_INTERVAL_SECONDS = 900f;  // 15 Minutes
        private const int MAX_BALLS_PER_BATCH = 10;
        private const float MIN_BATCH_INTERVAL = 0.1f;
        
        private readonly LevelConfigDatabase _database;
        
        public SessionValidator(LevelConfigDatabase database)
        {
            _database = database;
        }
        
        public bool NeedsReset(PlayerSession session)
        {
            TimeSpan timeSinceReset = DateTime.UtcNow - session.LastResetTime;
            return timeSinceReset.TotalSeconds >= RESET_INTERVAL_SECONDS;
        }
        
        public float GetTimeUntilReset(PlayerSession session)
        {
            TimeSpan timeSinceReset = DateTime.UtcNow - session.LastResetTime;
            float remaining = RESET_INTERVAL_SECONDS - (float)timeSinceReset.TotalSeconds;
            return Mathf.Max(0, remaining);
        }
        
        public bool ValidateBatchRequest(PlayerSession session, int[] bucketIndices, out string errorMessage)
        {
            errorMessage = null;
            
            if (bucketIndices.Length > MAX_BALLS_PER_BATCH)
            {
                errorMessage = $"Batch too large: {bucketIndices.Length} (Maximum amount: {MAX_BALLS_PER_BATCH})";
                Debug.LogWarning($"[SERVER] {errorMessage}");
                return false;
            }
            
            // Checking batch timing to prevent spam.
            TimeSpan timeSinceLastBatch = DateTime.UtcNow - session.LastBatchTime;
            if (timeSinceLastBatch.TotalSeconds < MIN_BATCH_INTERVAL)
            {
                errorMessage = $"Batch requests too frequent ({timeSinceLastBatch.TotalSeconds:F3}s < {MIN_BATCH_INTERVAL}s)";
                Debug.LogWarning($"[SERVER] {errorMessage}");
                return false;
            }
            
            // Checking current level config to validate bucket count.
            LevelConfigServerData config = _database.GetConfig(session.CurrentLevel);
            int bucketCount = config.Multipliers.Length;
            
            foreach (int bucketIndex in bucketIndices)
            {
                if (bucketIndex < 0 || bucketIndex >= bucketCount)
                {
                    errorMessage = $"Invalid bucket index: {bucketIndex} (valid: 0-{bucketCount - 1})";
                    Debug.LogWarning($"[SERVER] {errorMessage}");
                    return false;
                }
            }
            
            return true;
        }
    }
}