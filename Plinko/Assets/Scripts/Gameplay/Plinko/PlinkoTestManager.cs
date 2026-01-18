using Data;
using UnityEngine;

namespace Gameplay
{
    public class PlinkoTestManager : MonoBehaviour
    {
        [SerializeField] private PlinkoBoard board;
        [SerializeField] private PlinkoBallSpawner spawner;
        [SerializeField] private InputHandler inputHandler;
        
        private float[] testMultipliers = new float[] 
            { 0.5f, 0.5f, 1.0f, 1.5f, 2.0f, 3.0f, 2.0f, 1.5f, 1.0f, 0.5f, 0.5f };
        
        private int _ballsLanded = 0;

        private void Start()
        {
            // Initialize board
            board.InitializeBoard();
            spawner.SetSpawnPosition(board.GetSpawnPosition());
            
            // Configure with test multipliers
            LevelConfig testConfig = new LevelConfig(0, testMultipliers);
            Debug.Log(testConfig.bucketMultipliers.Length);
            board.ConfigureBucketsFromServer(testConfig);
            
            // Setup input
            inputHandler.OnSpawnPressed += spawner.StartSpawning;
            inputHandler.OnSpawnReleased += spawner.StopSpawning;
            
            // Setup ball landing event
            spawner.OnBallLanded += HandleBallLanded;
            
            Debug.Log("Test Plinko initialized! Click and hold to spawn balls.");
        }

        private void OnDestroy()
        {
            if (inputHandler != null)
            {
                inputHandler.OnSpawnPressed -= spawner.StartSpawning;
                inputHandler.OnSpawnReleased -= spawner.StopSpawning;
            }
            
            if (spawner != null)
            {
                spawner.OnBallLanded -= HandleBallLanded;
            }
        }

        private void HandleBallLanded(int bucketIndex)
        {
            _ballsLanded++;
            float multiplier = testMultipliers[bucketIndex];
            Debug.Log($"Ball #{_ballsLanded} landed in bucket {bucketIndex} (x{multiplier})");
        }
    }
}