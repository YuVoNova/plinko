using System.Collections.Generic;
using System.Threading.Tasks;
using Data;

namespace Backend
{
    public interface IBackendService
    {
        Task<InitResponse> InitializeGame();
        Task<LevelConfigResponse> GetLevelConfig(int level);
        Task<RewardResponse> ProcessBallLandings(int[] bucketIndices);
        Task<List<BallResultData>> GetBatchResults(BallLandData[] ballLandDataArray);
        Task<LevelProgressionResponse> CheckLevelProgression(int clientBallsDropped);
        Task<LevelAdvanceResponse> AdvanceLevel();
        Task<SessionCheckResponse> CheckSessionStatus();
        Task<ResetResponse> ResetGame(bool isFullReset);
        Task<float> AddBalance(float amount);
    }
}