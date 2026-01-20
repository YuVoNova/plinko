using Data;

namespace Backend
{
    public class InitResponse
    {
        public bool Success;
        public int BallCount;
        public int CurrentLevel;
        public float WalletBalance;
        public string Message;
        public long ServerTime;
    }
    
    public class LevelConfigResponse
    {
        public bool Success;
        public LevelConfig LevelConfig;
        public int BallAmount;
        public string Message;
    }
    
    public class RewardResponse
    {
        public bool Success;
        public float WalletBalance;
        public float RewardEarned;
        public int BallsProcessed;
        public string Message;
    }
    
    public class LevelProgressionResponse
    {
        public bool Success;
        public bool ShouldLevelUp;
        public int BallsDropped;
        public int BallsRequired;
        public string Message;
    }
    
    public class LevelAdvanceResponse
    {
        public bool Success;
        public int NewLevel;
        public int NewBallAmount;
        public string Message;
    }
    
    public class ResetResponse
    {
        public bool Success;
        public int BallCount;
        public int CurrentLevel;
        public float WalletBalance;
        public string Message;
    }
    
    public class SessionCheckResponse
    {
        public bool Success;
        public bool NeedsReset;
        public float TimeUntilReset;
        public string Message;
    }
}