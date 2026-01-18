using System;

namespace Data
{
    [Serializable]
    public class LevelConfig
    {
        public int levelID;
        public float[] bucketMultipliers;
        
        public LevelConfig(int levelID, float[] bucketMultipliers)
        {
            this.levelID = levelID;
            this.bucketMultipliers = bucketMultipliers;
        }
    }
}