using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;

namespace Blast.Collections {

    // Very naive, do not use in jobs, don't forget to dispose, not threadSafe.
    public class NativeArrayPool<T> : IDisposable where T : struct {

        private Queue<PoolableNativeArrayWrapper<T>> pool;
        private Dictionary<ulong, PoolableNativeArrayWrapper<T>> used;
        private int arraySize;
        private NativeArrayOptions nativeArrayOptions;
        private ulong lastIndex;

        public static NativeArrayPool<T> Create (int arraySize, NativeArrayOptions nativeArrayOptions) {
            return new NativeArrayPool<T>() {
                pool = new Queue<PoolableNativeArrayWrapper<T>>(),
                used = new Dictionary<ulong, PoolableNativeArrayWrapper<T>>(),
                arraySize = arraySize
            };
        }

        public PoolableNativeArrayWrapper<T> Get () {
            if(pool.Count > 0) {
                PoolableNativeArrayWrapper<T> pooledArray = pool.Dequeue();
                pooledArray.index = lastIndex;
                used.Add(lastIndex, pooledArray);
                lastIndex++;
                return pooledArray;
            } else {
                PoolableNativeArrayWrapper<T> pooledArray = PoolableNativeArrayWrapper<T>.Create(this, arraySize);
                pooledArray.index = lastIndex;
                used.Add(lastIndex, pooledArray);
                lastIndex++;
                return pooledArray;
            }
        }

        internal void Return (PoolableNativeArrayWrapper<T> pooledArray) {
            used.Remove(pooledArray.index);
            pool.Enqueue(pooledArray);
        }

        public void Dispose () {
            foreach(PoolableNativeArrayWrapper<T> pooledArray in pool) {
                pooledArray.Dispose();
            }
            foreach(KeyValuePair<ulong, PoolableNativeArrayWrapper<T>> pooledArray in used) {
                pooledArray.Value.Dispose();
            }
        }
    }



    public class PoolableNativeArrayWrapper<T> : IDisposable where T : struct {

        public ulong index { get; internal set; }
        public NativeArrayPool<T> owner { get; private set; }
        public NativeArray<T> nativeArray { get; private set; }

        public bool IsCreate => nativeArray.IsCreated;

        public static PoolableNativeArrayWrapper<T> Create (NativeArrayPool<T> owner, int arraySize) {
            return new PoolableNativeArrayWrapper<T>() {
                nativeArray = new NativeArray<T>(arraySize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory),
                owner = owner
            };
        }

        public void Dispose () {
            nativeArray.Dispose();
        }

        public void Return () {
            owner.Return(this);
        }
    }
}