using System;

namespace Backend
{
    [Serializable]
    public class PlayerSession
    {
        public string SessionId;
        public DateTime SessionStartTime;
        public DateTime LastResetTime;
        
        public int CurrentBallCount;
        public int CurrentLevel;
        public float WalletBalance;
        
        public int TotalBallsDroppedThisLevel;
        public int TotalBallsDroppedThisSession;
        
        public int TotalBatchesProcessed;
        public DateTime LastBatchTime;
        
        public PlayerSession()
        {
            // This method generates a unique ID to identify the user session.
            SessionId = Guid.NewGuid().ToString();
            SessionStartTime = DateTime.UtcNow;
            LastResetTime = DateTime.UtcNow;
            
            CurrentBallCount = 200;
            CurrentLevel = 1;
            WalletBalance = 0f;
            
            TotalBallsDroppedThisLevel = 0;
            TotalBallsDroppedThisSession = 0;
            TotalBatchesProcessed = 0;
        }
    }
}