using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Blast.ECS;
using Blast.ObjectPooling;
using MLAPI.Serialization;

namespace Blast.NetworkedEntities {

    public struct NetworkedStaticEntityTask {
        public ulong id;
        public bool doSpawn;
    }

    public abstract class NetworkedStaticEntitySystemBase : IECSSystem, IECSInitSystem {

        private ObjectPool[] gameObjectBanks;                   // The container with all gameobject pool for all different entity
        private int[] totalTypeCount;                           // The container used for evaluating the number of entities for each type (and/or the index of the pools)
        public Dictionary<ulong, Entity> entities;              // Stores all entities in the simulation for a given system. The key is the entity's per-type id (there is no global ids)
        public byte entityTypeId { get; internal set; }         // The global entity type id for all entities managed by this system.
        private Queue<NetworkedStaticEntityTask> networkSyncQueue;

        private const int maxDisabledFrame = 60;                // The amount of frames an entity can stay disabled before being destroyed


        /// <summary>
        /// The code that needs to be run once at the start
        /// </summary>
        protected abstract void OnServerInitSystem ();


        /// <summary>
        /// Initiates the pools of gameObjects and creates the entity collection.
        /// </summary>
        public void Init () {
            networkSyncQueue = new Queue<NetworkedStaticEntityTask>();
            entities = new Dictionary<ulong, Entity>(64);
            gameObjectBanks = new ObjectPool[GetVisualTypeCount];
            totalTypeCount = new int[GetVisualTypeCount];
            for(int i = 0; i < GetVisualTypeCount; i++) {
                gameObjectBanks[i] = new ObjectPool(GetVisualPrefab(i), GameManager.inst.objectParent, 8);
            }
            if(NetAssist.IsServer)
                OnServerInitSystem();
        }



        #region Updates
        /// <summary>
        /// The code an entity must run for every frame of the simulation. Keep this as optimized as possible.
        /// </summary>
        protected abstract void OnServerUpdateEntity (Entity entity, float deltaTime);

        /// <summary>
        /// The code that needs to be run once a simulation frame
        /// </summary>
        protected abstract void OnServerUpdateSystem ();


        /// <summary>
        /// Runs the simulation of all enabled entities
        /// </summary>
        internal void ServerUpdateSimulation (float deltaTime) {
            OnServerUpdateSystem();
            foreach(KeyValuePair<ulong, Entity> kvp in entities) {
                ref var networkedEntityComponent = ref kvp.Value.Get<NetworkedEntityComponent>();
                if(networkedEntityComponent.disabledTimer == 0)
                    OnServerUpdateEntity(kvp.Value, deltaTime);
            }
            foreach(KeyValuePair<ulong, Entity> kvp in entities) {
                ref var networkedEntityComponent = ref kvp.Value.Get<NetworkedEntityComponent>();
                if(networkedEntityComponent.disabledTimer > 0) {
                    DestroyEntity(kvp.Key);
                    return;
                }
            }
        }
        #endregion



        #region Spawning / Destruction
        /// <summary>
        /// Applies all Blast.ECS components an entity needs to follow the system's archetype
        /// </summary>
        protected abstract void SetupComponents (Entity entity);


        /// <summary>
        /// Either creates or reenable an entity whose ID is made from give parameters
        /// </summary>
        protected Entity GetEntity (int spawnFrame, byte owner, byte copy, out bool didEntityExist) {
            ulong entityId = NetworkedEntityComponent.GenerateID(spawnFrame, owner, copy);
            if(entities.TryGetValue(entityId, out Entity entity)) {
                ref var networkedEntityComponent = ref entity.Get<NetworkedEntityComponent>();
                networkedEntityComponent.disabledTimer = 0;
                didEntityExist = true;
                return entity;
            }

            entity = SimulationManager.inst.simulationWorld.NewEntity();
            entity.Get<NetworkedEntityComponent>().SetValues(spawnFrame, owner, copy);
            SetupComponents(entity);
            entities.Add(entityId, entity);
            didEntityExist = false;
            return entity;
        }


        /// <summary>
        /// Removes an entity from the simulation
        /// </summary>
        protected void DestroyEntity (ulong id) {
            if(!entities.TryGetValue(id, out Entity entity))
                return;

            ref var networkedEntityComponent = ref entity.Get<NetworkedEntityComponent>();
            int type = GetVisualTypeFromEntity(entity);
            if(gameObjectBanks[type].objectMemory.TryGetValue(networkedEntityComponent.id, out PoolableObject value) && !SimulationManager.inst.IsReplayingFrame) {
                value.ReturnToPool();
            }

            entities.Remove(id);
            entity.Destroy();
        }
        #endregion



        #region Network Serialization
        /// <summary>
        /// The function that will be called when the systems needs to serialize to a stream a given entity that needs to be sync with other clients.
        /// </summary>
        protected abstract void OnSerializeNewEntityNetwork (Entity entity, NetworkWriter writer);


        /// <summary>
        /// The function that will be called when the systems needs to deserialize from a stream a given entity that needed to be sync with this client.
        /// </summary>
        protected abstract void OnDeserializeNewEntityNetwork (Entity entity, NetworkReader reader, bool didEntityExist);


        /// <summary>
        /// Tells the systems manager whenever any entity in this system need to be sync or not.
        /// </summary>
        internal bool DoNetworkSerialize () {
            return networkSyncQueue.Count > 0;
        }


        /// <summary>
        /// Serializes all entities to the stream that needed to be sync.
        /// </summary>
        internal void SerializeEntitiesNetwork (NetworkWriter writer) {
            writer.WriteUInt16Packed((ushort)networkSyncQueue.Count);
            while(networkSyncQueue.Count > 0) {
                var task = networkSyncQueue.Dequeue();
                var networkedEntity = entities[task.id].Get<NetworkedEntityComponent>();

                writer.WriteInt32(networkedEntity.spawnFrame);
                writer.WriteByte(networkedEntity.owner);
                writer.WriteBits(networkedEntity.copy, 4);
                writer.WriteBit(task.doSpawn);
                if(task.doSpawn) {
                    OnSerializeNewEntityNetwork(entities[task.id], writer);
                }
            }
        }

        /// <summary>
        /// Deserializes all entities written by SerializeEntitiesNetwork
        /// </summary>
        internal void DeserializeEntitiesNetwork (NetworkReader reader) {
            ushort entityCount = reader.ReadUInt16Packed();
            for(int i = 0; i < entityCount; i++) {

                int spawnFrame = reader.ReadInt32();
                byte owner = (byte)reader.ReadByte();
                byte copy = (byte)reader.ReadBits(4);
                ulong key = NetworkedEntityComponent.GenerateID(spawnFrame, owner, copy);

                if(reader.ReadBit()) {
                    bool didEntityExist = true;
                    if(entities.TryGetValue(key, out Entity entity)) {
                        ref var networkedEntity = ref entity.Get<NetworkedEntityComponent>();
                        networkedEntity.disabledTimer = 0;
                    } else {
                        entity = GetEntity(spawnFrame, owner, copy, out didEntityExist);
                    }
                    OnDeserializeNewEntityNetwork(entity, reader, didEntityExist);
                } else {
                    if(entities.ContainsKey(key)) {
                        DestroyEntity(key);
                    }
                }
            }
        }
        #endregion



        #region Visuals
        public abstract int GetVisualTypeCount { get; }
        protected abstract PoolableObject GetVisualPrefab (int type);
        protected abstract int GetVisualTypeFromEntity (Entity entity);

        public void UpdateVisuals () {
            foreach(KeyValuePair<ulong, Entity> kvp in entities) {
                ref var networkedEntity = ref kvp.Value.Get<NetworkedEntityComponent>();
                if(networkedEntity.disabledTimer > 0)
                    continue;
                int type = GetVisualTypeFromEntity(kvp.Value);

                var pooledObject = gameObjectBanks[type].GetInstanceWithId(networkedEntity.id, out bool didCreateNewInstance);
                if(didCreateNewInstance) {
                    ((PoolableVisualObject)pooledObject).OnSpawn(kvp.Value);
                }
                ((PoolableVisualObject)pooledObject).OnVisualUpdate(kvp.Value);
            }
        }
        #endregion
    }
}
