using System.Collections.Generic;
using System.Linq;
using Data;
using UnityEngine;

namespace Gameplay
{
    public class PlinkoBoard : MonoBehaviour
    {
        // Peg/Ball Size Ratio: 5/8 (0.625)
        // X-Y Distance Between Pegs: Ball Size x 2
        private const float BALL_SIZE = 0.2f;
        private const int INITIAL_PEG_AMOUNT = 3;
        private const int PEG_ROWS = 10;
        
        [Header("Prefab References")]
        [SerializeField] private Transform pegParent;
        [SerializeField] private Transform bucketParent;
        [SerializeField] private GameObject pegPrefab;
        [SerializeField] private GameObject bucketPrefab;
        
        [Header("Bucket Colors")]
        [SerializeField] private Color lowMultiplierColor;
        [SerializeField] private Color midMultiplierColor;
        [SerializeField] private Color highMultiplierColor;

        private float _pegDistanceX;
        private float _pegDistanceY;
        private Vector2 _boardOffset;
        private float _finalPegStartX;
        private float _finalPegRowY;
        
        private int _bucketCount;
        
        private readonly List<GameObject> _pegs = new List<GameObject>();
        private readonly List<PlinkoBucket> _buckets = new List<PlinkoBucket>();
        
        private void OnDestroy()
        {
            ClearBoard();
        }
        
        public void InitializeBoard()
        {
            SetOffsets();
            ClearBoard();
            CreatePegs();
            CreateBuckets();
        }
        
        public void ConfigureBucketsFromServer(LevelConfig config)
        {
            if (config.bucketMultipliers.Length != _buckets.Count)
            {
                Debug.LogError($"Config bucket count {config.bucketMultipliers.Length} doesn't match board {_buckets.Count}");
                return;
            }
            
            for (int i = 0; i < _buckets.Count; i++)
            {
                float mult = config.bucketMultipliers[i];
                
                _buckets[i].UpdateDisplay(mult);
                
                Color bucketColor = GetColorForMultiplier(mult);
                _buckets[i].SetColor(bucketColor);
            }
        }
        
        public PlinkoBucket GetBucket(int index)
        {
            if (index >= 0 && index < _buckets.Count)
                return _buckets[index];
            
            return null;
        }
        
        public Vector2 GetSpawnPosition()
        {
            return new Vector2(0, _boardOffset.y + 1);
        }

        private void SetOffsets()
        {
            _pegDistanceX = BALL_SIZE * 2;
            _pegDistanceY = _pegDistanceX * Mathf.Sqrt(3) / 2;
            _boardOffset = new Vector2(0f, _pegDistanceY * (PEG_ROWS + 1) / 2);
            
            int finalRowPegCount = PEG_ROWS + INITIAL_PEG_AMOUNT - 1;
            _bucketCount = finalRowPegCount - 1;
        }
        
        private void CreatePegs()
        {
            for (int row = 0; row < PEG_ROWS; row++)
            {
                int pegsInRow = row + INITIAL_PEG_AMOUNT;
                float rowWidth = (pegsInRow - 1) * _pegDistanceX;
                float startX = -rowWidth / 2f;
                float yPos = _boardOffset.y - (row * _pegDistanceY);
                _finalPegStartX  = startX;
                _finalPegRowY = yPos;
                
                for (int col = 0; col < pegsInRow; col++)
                {
                    float xPos = startX + (col * _pegDistanceX);
                    
                    Vector2 pegPos = new Vector2(xPos, yPos);
                    GameObject peg = Instantiate(pegPrefab, pegPos, Quaternion.identity, pegParent);
                    peg.name = $"Peg_R{row}_C{col}";
                    
                    _pegs.Add(peg);
                }
            }
        }
        
        private void CreateBuckets()
        {
            float startX = _finalPegStartX + _pegDistanceX / 2f;
            float yPos = _finalPegRowY - _pegDistanceY;
            
            for (int i = 0; i < _bucketCount; i++)
            {
                float xPos = startX + i * _pegDistanceX;
                Vector2 bucketPos = new Vector2(xPos,yPos);
                
                PlinkoBucket bucket = Instantiate(bucketPrefab, bucketPos, Quaternion.identity, bucketParent).GetComponentInChildren<PlinkoBucket>();
                bucket.Initialize(i);
                
                _buckets.Add(bucket);
            }
        }
        
        private Color GetColorForMultiplier(float multiplier)
        {
            return multiplier switch
            {
                < 1.0f => lowMultiplierColor,
                < 3.0f => midMultiplierColor,
                _ => highMultiplierColor
            };
        }
        
        private void ClearBoard()
        {
            foreach (GameObject peg in _pegs.Where(peg => peg != null))
            {
                Destroy(peg);
            }
            _pegs.Clear();
            
            foreach (PlinkoBucket bucket in _buckets.Where(bucket => bucket != null))
            {
                Destroy(bucket.gameObject);
            }
            _buckets.Clear();
        }
    }
}