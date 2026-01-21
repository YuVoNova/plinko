namespace Core
{
    public enum GameState
    {
        Initializing,       // Loading initial data from server
        Ready,              // Idle, ready to play
        Playing,            // Actively spawning balls
        LevelTransition,    // Transitioning between levels
        Resetting           // Reset in progress
    }
}