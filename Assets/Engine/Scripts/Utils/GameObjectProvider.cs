using System;
using Assets.Engine.Scripts.Common;
using UnityEngine;

namespace Assets.Engine.Scripts.Provider
{
    [AddComponentMenu("Voxe/Singleton/GameObjectProvider")]
    public class GameObjectProvider: MonoSingleton<GameObjectProvider>
    {
        private GameObject m_go;
        public ObjectPoolEntry [] ObjectPools = new ObjectPoolEntry [0];

        // Called after the singleton instance is created
        private void Awake()
        {
            m_go = new GameObject("GameObjects");
            m_go.transform.parent = gameObject.transform;

            // Iterate pool entries and create a pool of prefabs for each of them
            foreach (ObjectPoolEntry pool in Instance.ObjectPools)
            {
                if (pool.Prefab==null)
                {
                    Debug.LogError("No prefab specified in one of the object pool's entries");
                    continue;
                }

                pool.Go = m_go;

                BuildPool(pool, pool.PreloadCount);
            }
        }

        // Returns a pool of a given name if it exists
        public static ObjectPoolEntry GetPool(string poolName)
        {
            foreach (ObjectPoolEntry pool in Instance.ObjectPools)
            {
                if (pool.Name==poolName)
                    return pool;
            }
            return null;
        }

        private static void BuildPool(ObjectPoolEntry pool, int requestedSize)
        {
            pool.Cache = new GameObject[pool.PreloadCount];

            for (int i = 0; i<pool.PreloadCount; i++)
            {
                GameObject go = Instantiate(pool.Prefab);
                go.name = pool.Name;
                go.SetActive(false);

                PushObject(pool.Name, go);
            }
        }

        public static void PushObject(string poolName, GameObject go)
        {
            if (go==null)
                throw new ArgumentNullException(string.Format("Trying to pool a null game object in pool {0}", poolName));

            ObjectPoolEntry pool = GetPool(poolName);
            if (pool==null)
                throw new InvalidOperationException(string.Format("Object pool {0} does not exist", poolName));

            pool.Push(go);
        }

        public static GameObject PopObject(string poolName)
        {
            ObjectPoolEntry pool = GetPool(poolName);
            if (pool == null)
                throw new InvalidOperationException(string.Format("Object pool {0} does not exist", poolName));

            return pool.Pop();
        }

        [Serializable]
        public class ObjectPoolEntry
        {
            public string Name;
            public GameObject Prefab;

            public int PreloadCount = 2000;

            [HideInInspector] public int PolledCount;
            [HideInInspector] public GameObject[] Cache;
            [HideInInspector] public GameObject Go;

            private ObjectPoolEntry()
            {
            } // Do not allow default contructor

            public ObjectPoolEntry(GameObject go, int size)
            {
                Go = go;
                PreloadCount = size;
            }

            public void Push(GameObject go)
            {
                // There is a limit to how much objects we can hold
                if (PolledCount>=Cache.Length)
                    throw new InvalidOperationException(string.Format("{0}: Object pool is full", ToString()));

                // Deactive object, reset its transform and physics data
                go.SetActive(false);
                go.transform.parent = Go.transform; // Make this object a parent of the pooled object
                //if (go.rigidbody != null)
                //	go.rigidbody.velocity = Vector3.zero;

                // Place a pointer to our object to the back of our cache list
                Cache[PolledCount++] = go;
            }

            public GameObject Pop()
            {
                GameObject go;

                if (PolledCount>0)
                {
                    // Return an object being located on the back of our cache list
                    go = Cache[--PolledCount];

                    // Reset transform and active it
                    //go.transform.parent = null;
                    go.SetActive(true);
                }
                else
                {
                    // No space left in pool, instantiate a new object
                    //go = (GameObject)Instantiate (pool.Prefab);
                    throw new InvalidOperationException(string.Format("{0}: object pool is empty", ToString()));
                }

                return go;
            }
        }
    }
}