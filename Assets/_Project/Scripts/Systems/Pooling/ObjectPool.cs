using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Blast.ObjectPooling {

    public interface IPoolableObjectCollection {
        void ReturnInstance (PoolableObject poolableObject);
    }

    public class ObjectPool : IPoolableObjectCollection {
        
        /// <summary>
        /// A dictionairy containing all active objects, with the key being their ids.
        /// </summary>
        public Dictionary<ulong, PoolableObject> objectMemory { private set; get; }
        private Stack<PoolableObject> objectPool;

        private uint recentId;
        private PoolableObject prefab;
        private Transform parent;

        /// <summary>
        /// Creates a new pool of objects and already creates new prewamed instances.
        /// </summary>
        /// <param name="prefab">The prefab that will be used when creating new instances in the pool</param>
        /// <param name="parent">The parent the new instance will be attached to while in the pool</param>
        /// <param name="prewarm">How many instances should be create upon initializing the pool</param>
        public ObjectPool (PoolableObject prefab, Transform parent, int prewarm = 8) {

            this.prefab = prefab;
            this.parent = parent;
            objectPool = new Stack<PoolableObject>(prewarm);
            objectMemory = new Dictionary<ulong, PoolableObject>(prewarm);

            if(prewarm > 0) {
                for(uint i = 0; i < prewarm; i++) {
                    InstantiateNewObjectInPool();
                }
            }
        }

        /// <summary>
        /// Adds a new object instance to the object pool.
        /// </summary>
        private void InstantiateNewObjectInPool () {
            PoolableObject poolObject = Object.Instantiate(prefab, parent);
            poolObject.OnInstantiation();

            poolObject.gameObject.SetActive(false);
            poolObject.isInPool = true;
            poolObject.hideFlags = HideFlags.HideInHierarchy;
            poolObject.transform.parent = parent;

            poolObject.ownerCollection = this;
            poolObject.id = recentId;
            recentId++;

            objectPool.Push(poolObject);
        }

        /// <summary>
        /// Either gets an instance with a given id or generate a new one with the given id
        /// </summary>
        public PoolableObject GetInstanceWithId (ulong id) {
            if(!objectMemory.TryGetValue(id, out PoolableObject value)) {
                value = GetNewInstance(false);
                value.id = id;
                value.isInMemory = true;
                objectMemory.Add(id, value);
            }
            return value;
        }

        /// <summary>
        /// Either gets an instance with a given id or generate a new one with the given id
        /// </summary>
        public PoolableObject GetInstanceWithId (ulong id, out bool didCreateNewInstance) {
            didCreateNewInstance = false;
            if(!objectMemory.TryGetValue(id, out PoolableObject value)) {
                didCreateNewInstance = true;
                value = GetNewInstance(false);
                value.id = id;
                value.isInMemory = true;
                objectMemory.Add(id, value);
            }
            return value;
        }
        /// <summary>
        /// Adds a given poolableObject to this pool's object memory, and makes sure there isn't two objects with the same id
        /// </summary>
        public void AddInstanceToMemory (PoolableObject poolableObject) {
            if(objectMemory.ContainsKey(poolableObject.id)) {
                poolableObject.id = recentId;
                recentId++;
                poolableObject.isInMemory = true;

                objectMemory.Add(poolableObject.id, poolableObject);
            } else {
                poolableObject.isInMemory = true;
                objectMemory.Add(poolableObject.id, poolableObject);
            }
        }

        /// <summary>
        /// Retrieves a new object from the pool and prepares it to be used normally. 
        /// This might instantiate new objects if there isn't enough in the pool.
        /// </summary>
        /// <returns>A poolable object ready for use</returns>
        public PoolableObject GetNewInstance (bool addToMemory = true) {
            if(objectPool.Count <= 0) {
                InstantiateNewObjectInPool();
            }

            PoolableObject poolObject = objectPool.Pop();
            if(poolObject == null) {
                return null;
            }
            poolObject.gameObject.SetActive(true);
            poolObject.isInPool = false;
            poolObject.hideFlags = HideFlags.None;
            poolObject.transform.parent = parent;

            poolObject.isInMemory = addToMemory;
            if(addToMemory) {
                if(objectMemory.ContainsKey(poolObject.id)) {
                    poolObject.id = recentId;
                    recentId++;
                }
                objectMemory.Add(poolObject.id, poolObject);
            } else {
                poolObject.id = 0;
            }

            return poolObject;
        }

        /// <summary>
        /// Takes care of returning an object in the pool and preparing it to be put to sleep.
        /// </summary>
        /// <param name="poolObject">The poolable object to return to the pool.</param>
        public void ReturnInstance (PoolableObject poolObject) {
            poolObject.gameObject.SetActive(false);
            poolObject.isInPool = true;
            poolObject.hideFlags = HideFlags.HideInHierarchy;
            if(poolObject.isInMemory) {
                objectMemory.Remove(poolObject.id);
            }
            poolObject.isInMemory = false;

            objectPool.Push(poolObject);
        }
    }

    public class ObjectBankPool : IPoolableObjectCollection {

        /// <summary>
        /// A dictionairy containing all active objects, with the key being their ids.
        /// </summary>
        public List<PoolableObject> objectMemory {
            private set; get;
        }
        private Stack<PoolableObject> objectPool;

        private PoolableObject prefab;
        private Transform parent;

        /// <summary>
        /// Creates a new pool of objects and already creates new prewamed instances.
        /// </summary>
        /// <param name="prefab">The prefab that will be used when creating new instances in the pool</param>
        /// <param name="parent">The parent the new instance will be attached to while in the pool</param>
        /// <param name="prewarm">How many instances should be create upon initializing the pool</param>
        public ObjectBankPool (PoolableObject prefab, Transform parent, int prewarm = 8) {

            this.prefab = prefab;
            this.parent = parent;
            objectPool = new Stack<PoolableObject>(prewarm);
            objectMemory = new List<PoolableObject>(prewarm);

            if(prewarm > 0) {
                for(uint i = 0; i < prewarm; i++) {
                    InstantiateNewObjectInPool();
                }
            }
        }

        /// <summary>
        /// Adds a new object instance to the object pool.
        /// </summary>
        private void InstantiateNewObjectInPool () {
            PoolableObject poolObject = Object.Instantiate(prefab, parent);
            poolObject.OnInstantiation();

            poolObject.gameObject.SetActive(false);
            poolObject.isInPool = true;
            poolObject.hideFlags = HideFlags.HideInHierarchy;
            poolObject.transform.parent = parent;

            poolObject.ownerCollection = this;

            objectPool.Push(poolObject);
        }

        /// <summary>
        /// Takes care of returning an object in the pool and preparing it to be put to sleep.
        /// </summary>
        /// <param name="poolObject">The poolable object to return to the pool.</param>
        public void ReturnInstance (PoolableObject poolObject) {
            poolObject.gameObject.SetActive(false);
            poolObject.isInPool = true;
            poolObject.hideFlags = HideFlags.HideInHierarchy;
            if(poolObject.isInMemory) {
                objectMemory.RemoveAt((int)poolObject.id);
            }
            poolObject.isInMemory = false;

            objectPool.Push(poolObject);
        }

        /// <summary>
        /// Prepares instances from the pool and puts them in memory
        /// </summary>
        public void AddNewInstanceToMemory () {
            if(objectPool.Count <= 0) {
                InstantiateNewObjectInPool();
            }

            PoolableObject poolObject = objectPool.Pop();
            poolObject.gameObject.SetActive(true);
            poolObject.isInPool = false;
            poolObject.hideFlags = HideFlags.None;
            poolObject.isInMemory = true;
            poolObject.id = (uint)(objectMemory.Count);

            objectMemory.Add(poolObject);
        }

        /// <summary>
        /// Will either generate new objects or hide unused to achieve a required object count in memory
        /// </summary>
        public void GenerateFixedCount (int count) {

            // Dumps extra object in memory to the pool
            if(objectMemory.Count > count) {
                while(objectMemory.Count > count) {
                    objectMemory[objectMemory.Count - 1].ReturnToPool();
                }
            }

            // Put object from the pool to memory until we have enough
            while(objectMemory.Count < count) {
                AddNewInstanceToMemory();
            }
        }
    }
}
