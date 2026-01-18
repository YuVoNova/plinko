using System.Collections.Generic;
using System.Linq;
using Core;
using UnityEngine;

namespace Utils
{
    public class ObjectPool<T> where T : Component, IPoolable
    {
        private readonly T _prefab;
        private readonly Transform _parent;
        private readonly int _initialSize;
        private readonly int _maxSize;
        
        private readonly Queue<T> _availableObjects;
        private readonly List<T> _activeObjects;
        
        private int _totalCreated;
        
        public ObjectPool(T prefab, Transform parent, int initialSize, int maxSize = 500)
        {
            _prefab = prefab;
            _parent = parent;
            _initialSize = initialSize;
            _maxSize = maxSize;
            
            _availableObjects = new Queue<T>(initialSize);
            _activeObjects = new List<T>(initialSize);
            
            PreWarm();
        }
        
        public T Get()
        {
            T obj;
            
            // Try to get from available pool
            if (_availableObjects.Count > 0)
            {
                obj = _availableObjects.Dequeue();
            }
            else
            {
                // Create new object if under limit
                obj = CreateNewObject();
                
                if (obj == null)
                {
                    // Pool exhausted, reclaim oldest
                    obj = ReclaimOldest();
                }
            }
            
            if (obj != null)
            {
                obj.OnSpawn();
                _activeObjects.Add(obj);
            }
            
            return obj;
        }
        
        public void Return(T obj)
        {
            if (obj == null) return;
            
            obj.OnDespawn();
            _activeObjects.Remove(obj);
            _availableObjects.Enqueue(obj);
        }
        
        public void ReturnAll()
        {
            // Copy to avoid modification during iteration
            List<T> toReturn = new List<T>(_activeObjects);
            
            foreach (T obj in toReturn)
            {
                Return(obj);
            }
        }
        
        public void Clear()
        {
            foreach (T obj in _activeObjects.Where(obj => obj != null))
            {
                Object.Destroy(obj.gameObject);
            }
            
            foreach (T obj in _availableObjects.Where(obj => obj != null))
            {
                Object.Destroy(obj.gameObject);
            }
            
            _activeObjects.Clear();
            _availableObjects.Clear();
            _totalCreated = 0;
        }
        
        private void PreWarm()
        {
            for (int i = 0; i < _initialSize; i++)
            {
                CreateNewObject();
            }
        }
        
        private T CreateNewObject()
        {
            if (_totalCreated >= _maxSize)
            {
                Debug.LogWarning($"ObjectPool<{typeof(T).Name}> at max capacity: {_maxSize}");
                return null;
            }
            
            T obj = Object.Instantiate(_prefab, _parent);
            obj.gameObject.SetActive(false);
            obj.name = $"{typeof(T).Name}_{_totalCreated}";
            
            _availableObjects.Enqueue(obj);
            _totalCreated++;
            
            return obj;
        }
        
        private T ReclaimOldest()
        {
            if (_activeObjects.Count == 0) return null;
            
            T oldest = _activeObjects[0];
            _activeObjects.RemoveAt(0);
            oldest.OnDespawn();
            
            return oldest;
        }
    }
}