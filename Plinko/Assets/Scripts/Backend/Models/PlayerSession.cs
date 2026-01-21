using System;
using Data;

namespace Backend
{
    [Serializable]
    public class PlayerSession
    {
        public string SessionId;
        public int CurrentLevel;
        public int CurrentBallCount;
        public int TotalBallsDroppedThisLevel;
        public DateTime LastBatchTime;
        public int TotalBatchesProcessed;
        
        public PlayerData PlayerData;
        
        public float WalletBalance
        {
            get => PlayerData.WalletBalance;
            set => PlayerData.WalletBalance = value;
        }
        
        public DateTime LastResetTime => PlayerData.GetSessionStartTime();
    }
}