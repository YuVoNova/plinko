using UnityEngine;
using Utils;

namespace Gameplay
{
    public class PlinkoBallPool : MonoBehaviour
    {
        [SerializeField] private PlinkoBall ballPrefab;
        [SerializeField] private Transform ballParent;
        
        private int initialPoolSize = 50;
        private int maxPoolSize = 100;
        
        private ObjectPool<PlinkoBall> _pool;
        
        private void Awake()
        {
            _pool = new ObjectPool<PlinkoBall>(
                ballPrefab,
                ballParent,
                initialPoolSize,
                maxPoolSize
            );
        }
        
        private void OnDestroy()
        {
            _pool?.Clear();
        }
        
        public PlinkoBall GetBall(Vector2 position)
        {
            PlinkoBall ball = _pool.Get();
            
            if (ball)
                ball.transform.position = position;
            
            return ball;
        }
        
        public void ReturnBall(PlinkoBall ball)
        {
            _pool.Return(ball);
        }
        
        public void ReturnAllBalls()
        {
            _pool.ReturnAll();
        }
    }
}