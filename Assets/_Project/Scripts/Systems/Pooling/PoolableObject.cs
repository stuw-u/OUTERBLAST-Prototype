using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Blast.ObjectPooling {

    public class PoolableObject : MonoBehaviour {
        public ulong id { internal set; get; }
        public bool isInPool { internal set; get; }
        public bool isInMemory { internal set; get; }
        internal IPoolableObjectCollection ownerCollection { set; get; }

        virtual internal void OnInstantiation () {

        }

        public void ReturnToPool () {
            isInPool = true;
            hideFlags = HideFlags.HideInHierarchy;
            ownerCollection.ReturnInstance(this);
        }
    }
}
