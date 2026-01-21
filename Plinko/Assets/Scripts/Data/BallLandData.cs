using System;

namespace Data
{
    [Serializable]
    public class BallLandData
    {
        public int DropNumber;
        public int BucketIndex;
        
        public BallLandData(int dropNumber, int bucketIndex)
        {
            DropNumber = dropNumber;
            BucketIndex = bucketIndex;
        }
    }
}