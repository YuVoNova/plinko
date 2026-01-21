using System;
using System.Collections.Generic;

namespace Data
{
    [Serializable]
    public class PlayerData
    {
        private const int MAX_HISTORY_ENTRIES = 5;
        
        public float WalletBalance;
        public long SessionStartTimestamp;
        public List<BallResultData> RewardHistory;
        
        public PlayerData()
        {
            WalletBalance = 0f;
            SessionStartTimestamp = GetCurrentTimestamp();
            RewardHistory = new List<BallResultData>();
        }
        
        public void AddHistoryEntry(BallResultData entry)
        {
            RewardHistory.Add(entry);
            
            while (RewardHistory.Count > MAX_HISTORY_ENTRIES)
            {
                RewardHistory.RemoveAt(0);
            }
        }
        
        public void AddHistoryEntry(List<BallResultData> entries)
        {
            foreach (BallResultData entry in entries)
            {
                AddHistoryEntry(entry);
            }
        }
        
        public void ResetSession()
        {
            SessionStartTimestamp = GetCurrentTimestamp();
        }
        
        public void FullReset()
        {
            WalletBalance = 0f;
            SessionStartTimestamp = GetCurrentTimestamp();
            RewardHistory.Clear();
        }
        
        public DateTime GetSessionStartTime()
        {
            return DateTimeOffset.FromUnixTimeSeconds(SessionStartTimestamp).UtcDateTime;
        }
        
        private static long GetCurrentTimestamp()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
    }
}