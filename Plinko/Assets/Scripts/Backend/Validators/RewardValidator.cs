using UnityEngine;

namespace Backend
{
    public class RewardValidator
    {
        private readonly LevelConfigDatabase _database;
        
        public RewardValidator(LevelConfigDatabase database)
        {
            _database = database;
        }
        
        public float CalculateReward(int level, int[] bucketIndices)
        {
            LevelConfigServerData config = _database.GetConfig(level);
            float totalReward = 0f;
            
            foreach (int bucketIndex in bucketIndices)
            {
                if (bucketIndex < 0 || bucketIndex >= config.Multipliers.Length)
                {
                    Debug.LogWarning($"[REWARD] Invalid bucket index {bucketIndex}, skipping");
                    continue;
                }
                
                float multiplier = config.Multipliers[bucketIndex];
                float reward = config.BaseRewardPerBall * multiplier;
                
                totalReward += reward;
                
                Debug.Log($"[REWARD] Bucket {bucketIndex}: {config.BaseRewardPerBall} × {multiplier} = {reward}");
            }
            
            return totalReward;
        }
    }
}