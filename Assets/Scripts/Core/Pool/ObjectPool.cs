using System;
using System.Collections.Generic;
using UnityEngine;

namespace TowerGame.Core.Pool
{
    /// <summary>
    /// Generic object pool for efficient object reuse
    /// Reduces GC pressure from frequent instantiation/destruction
    /// </summary>
    /// <typeparam name="T">Type of objects to pool (must be Component)</typeparam>
    public class ObjectPool<T> where T : Component
    {
        private readonly T prefab;
        private readonly Transform parent;
        private readonly Queue<T> available = new();
        private readonly HashSet<T> inUse = new();
        private readonly int maxSize;
        private readonly Action<T> onGet;
        private readonly Action<T> onRelease;

        /// <summary>
        /// Number of available objects in the pool
        /// </summary>
        public int AvailableCount => available.Count;

        /// <summary>
        /// Number of objects currently in use
        /// </summary>
        public int InUseCount => inUse.Count;

        /// <summary>
        /// Total number of objects created by this pool
        /// </summary>
        public int TotalCount => available.Count + inUse.Count;

        /// <summary>
        /// Create a new object pool
        /// </summary>
        /// <param name="prefab">Prefab to instantiate</param>
        /// <param name="initialSize">Initial pool size</param>
        /// <param name="maxSize">Maximum pool size (0 = unlimited)</param>
        /// <param name="parent">Parent transform for pooled objects</param>
        /// <param name="onGet">Action called when object is retrieved</param>
        /// <param name="onRelease">Action called when object is returned</param>
        public ObjectPool(
            T prefab,
            int initialSize = 10,
            int maxSize = 100,
            Transform parent = null,
            Action<T> onGet = null,
            Action<T> onRelease = null)
        {
            this.prefab = prefab;
            this.maxSize = maxSize;
            this.parent = parent;
            this.onGet = onGet;
            this.onRelease = onRelease;

            // Pre-populate pool
            for (int i = 0; i < initialSize; i++)
            {
                var obj = CreateNew();
                obj.gameObject.SetActive(false);
                available.Enqueue(obj);
            }

            Debug.Log($"[ObjectPool<{typeof(T).Name}>] Created with {initialSize} initial objects");
        }

        /// <summary>
        /// Get an object from the pool
        /// </summary>
        /// <returns>An object from the pool, or new if pool is empty</returns>
        public T Get()
        {
            T obj;

            if (available.Count > 0)
            {
                obj = available.Dequeue();
            }
            else if (maxSize == 0 || TotalCount < maxSize)
            {
                obj = CreateNew();
            }
            else
            {
                Debug.LogWarning($"[ObjectPool<{typeof(T).Name}>] Pool exhausted (max: {maxSize})");
                return null;
            }

            obj.gameObject.SetActive(true);
            inUse.Add(obj);
            onGet?.Invoke(obj);

            return obj;
        }

        /// <summary>
        /// Get an object at a specific position
        /// </summary>
        public T Get(Vector3 position)
        {
            var obj = Get();
            if (obj != null)
            {
                obj.transform.position = position;
            }
            return obj;
        }

        /// <summary>
        /// Get an object with position and rotation
        /// </summary>
        public T Get(Vector3 position, Quaternion rotation)
        {
            var obj = Get();
            if (obj != null)
            {
                obj.transform.SetPositionAndRotation(position, rotation);
            }
            return obj;
        }

        /// <summary>
        /// Return an object to the pool
        /// </summary>
        /// <param name="obj">Object to return</param>
        public void Release(T obj)
        {
            if (obj == null)
            {
                Debug.LogWarning($"[ObjectPool<{typeof(T).Name}>] Attempted to release null object");
                return;
            }

            if (!inUse.Contains(obj))
            {
                Debug.LogWarning($"[ObjectPool<{typeof(T).Name}>] Attempted to release object not from this pool");
                return;
            }

            inUse.Remove(obj);
            onRelease?.Invoke(obj);
            obj.gameObject.SetActive(false);

            if (parent != null)
            {
                obj.transform.SetParent(parent);
            }

            available.Enqueue(obj);
        }

        /// <summary>
        /// Release all objects back to the pool
        /// </summary>
        public void ReleaseAll()
        {
            // Create a copy to avoid modification during iteration
            var inUseCopy = new List<T>(inUse);
            foreach (var obj in inUseCopy)
            {
                Release(obj);
            }
        }

        /// <summary>
        /// Clear the pool and destroy all objects
        /// </summary>
        public void Clear()
        {
            foreach (var obj in available)
            {
                if (obj != null)
                {
                    UnityEngine.Object.Destroy(obj.gameObject);
                }
            }

            foreach (var obj in inUse)
            {
                if (obj != null)
                {
                    UnityEngine.Object.Destroy(obj.gameObject);
                }
            }

            available.Clear();
            inUse.Clear();

            Debug.Log($"[ObjectPool<{typeof(T).Name}>] Pool cleared");
        }

        /// <summary>
        /// Prewarm the pool with additional objects
        /// </summary>
        /// <param name="count">Number of objects to add</param>
        public void Prewarm(int count)
        {
            for (int i = 0; i < count && (maxSize == 0 || TotalCount < maxSize); i++)
            {
                var obj = CreateNew();
                obj.gameObject.SetActive(false);
                available.Enqueue(obj);
            }
        }

        private T CreateNew()
        {
            var obj = UnityEngine.Object.Instantiate(prefab, parent);
            obj.gameObject.name = $"{prefab.name}_Pooled_{TotalCount}";
            return obj;
        }
    }

    /// <summary>
    /// Pool manager for managing multiple object pools
    /// </summary>
    public class PoolManager : MonoBehaviour
    {
        public static PoolManager Instance { get; private set; }

        private readonly Dictionary<Type, object> pools = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// Register a new pool
        /// </summary>
        public void RegisterPool<T>(T prefab, int initialSize = 10, int maxSize = 100) where T : Component
        {
            var type = typeof(T);
            if (pools.ContainsKey(type))
            {
                Debug.LogWarning($"[PoolManager] Pool for {type.Name} already exists");
                return;
            }

            var pool = new ObjectPool<T>(prefab, initialSize, maxSize, transform);
            pools[type] = pool;
        }

        /// <summary>
        /// Get a pool for a specific type
        /// </summary>
        public ObjectPool<T> GetPool<T>() where T : Component
        {
            var type = typeof(T);
            if (pools.TryGetValue(type, out var pool))
            {
                return pool as ObjectPool<T>;
            }

            Debug.LogError($"[PoolManager] No pool registered for {type.Name}");
            return null;
        }

        /// <summary>
        /// Get an object from a pool
        /// </summary>
        public T Get<T>() where T : Component
        {
            return GetPool<T>()?.Get();
        }

        /// <summary>
        /// Release an object back to its pool
        /// </summary>
        public void Release<T>(T obj) where T : Component
        {
            GetPool<T>()?.Release(obj);
        }

        /// <summary>
        /// Clear all pools
        /// </summary>
        public void ClearAllPools()
        {
            foreach (var pool in pools.Values)
            {
                var clearMethod = pool.GetType().GetMethod("Clear");
                clearMethod?.Invoke(pool, null);
            }
            pools.Clear();
        }

        private void OnDestroy()
        {
            ClearAllPools();
        }
    }
}
