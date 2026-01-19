using System;
using System.Collections.Generic;
using Data;
using UnityEngine;

namespace Backend
{
    public class LevelConfigDatabase
    {
        private Dictionary<int, LevelConfigServerData> _levels;
        
        public LevelConfigDatabase()
        {
            InitializeConfigs();
        }
        
        public LevelConfigServerData GetConfig(int level)
        {
            if (_levels.TryGetValue(level, out LevelConfigServerData config))
                return config;
            
            // If level doesn't exist, return last level config.
            Debug.LogWarning($"Level {level} not found, returning max level config.");
            return _levels[GetMaxLevel()];
        }
        
        public int GetMaxLevel() => _levels.Count;
        
        private void InitializeConfigs()
        {
            // TODO -> Move hard-coded levels to an SO Config File
            
            _levels = new Dictionary<int, LevelConfigServerData>
            {
                // Level 1
                [1] = new LevelConfigServerData
                {
                    Level = 1,
                    Multipliers = new float[] { 0.2f, 0.4f, 0.6f, 0.8f, 1.0f, 1.5f, 1.0f, 0.8f, 0.6f, 0.4f, 0.2f },
                    BallAmount = 200,
                    BaseRewardPerBall = 1f
                },
                // Level 2
                [2] = new LevelConfigServerData
                {
                    Level = 2,
                    Multipliers = new float[] { 0.3f, 0.5f, 0.8f, 1.2f, 1.5f, 2.5f, 1.5f, 1.2f, 0.8f, 0.5f, 0.3f },
                    BallAmount = 300,
                    BaseRewardPerBall = 1.2f
                },
                // Level 3
                [3] = new LevelConfigServerData
                {
                    Level = 3,
                    Multipliers = new float[] { 0.5f, 0.8f, 1.2f, 1.8f, 2.5f, 4.0f, 2.5f, 1.8f, 1.2f, 0.8f, 0.5f },
                    BallAmount = 500,
                    BaseRewardPerBall = 1.5f
                },
                // Level 4
                [4] = new LevelConfigServerData
                {
                    Level = 4,
                    Multipliers = new float[] { 0.8f, 1.0f, 1.5f, 2.5f, 3.5f, 6.0f, 3.5f, 2.5f, 1.5f, 1.0f, 0.8f },
                    BallAmount = 1000,
                    BaseRewardPerBall = 2f
                },
                // Level 5
                [5] = new LevelConfigServerData
                {
                    Level = 5,
                    Multipliers = new float[] { 1.0f, 1.5f, 2.0f, 3.0f, 5.0f, 10.0f, 5.0f, 3.0f, 2.0f, 1.5f, 1.0f },
                    BallAmount = 999999,
                    BaseRewardPerBall = 3f
                }
            };
        }
    }
    
    [Serializable]
    public class LevelConfigServerData
    {
        public int Level;
        public float[] Multipliers;
        public int BallAmount;
        public float BaseRewardPerBall;
        
        /// <summary>
        /// Convert to client-safe LevelConfig
        /// </summary>
        public LevelConfig ToClientConfig()
        {
            return new LevelConfig(Level, Multipliers);
        }
    }
}