using System;

namespace Data
{
    [Serializable]
    public class BallResultData
    {
        public int DropNumber;
        public int BucketIndex;
        public float Multiplier;
        public float Reward;
        
        public BallResultData(int dropNumber, int bucketIndex, float multiplier, float reward)
        {
            DropNumber = dropNumber;
            BucketIndex = bucketIndex;
            Multiplier = multiplier;
            Reward = reward;
        }
    }
}