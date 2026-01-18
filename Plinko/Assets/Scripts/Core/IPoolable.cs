namespace Core
{
    /// <summary>
    /// Interface for poolable objects
    /// </summary>
    public interface IPoolable
    {
        public void OnSpawn();
        public void OnDespawn();
    }
}