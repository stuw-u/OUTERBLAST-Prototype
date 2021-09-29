using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Blast.ECS;
using Blast.ObjectPooling;
using MLAPI.Serialization;

namespace Blast.NetworkedEntities {

    public abstract class NetworkedEntitySystemBase : IECSSystem, IECSInitSystem {
        
        private ObjectPool[] gameObjectBanks;                   // The container with all gameobject pool for all different entity
        private int[] totalTypeCount;                           // The container used for evaluating the number of entities for each type (and/or the index of the pools)
        public Dictionary<ulong, Entity> entities;              // Stores all entities in the simulation for a given system. The key is the entity's per-type id (there is no global ids)
        public byte entityTypeId { get; internal set; }         // The global entity type id for all entities managed by this system.
        public virtual int SyncInterval { get { return 4; } }   // The amount of frames it take for the server to send an entity again. (Except for special action such as velocity change)

        private const int maxDisabledFrame = 60;                // The amount of frames an entity can stay disabled before being destroyed
        private int _cachedValidEntityCount = -1;
        private int _cachedValidNetworkEntityCount = -1;


        /// <summary>
        /// Initiates the pools of gameObjects and creates the entity collection.
        /// </summary>
        public void Init () {
            entities = new Dictionary<ulong, Entity>(64);
            gameObjectBanks = new ObjectPool[GetVisualTypeCount];
            totalTypeCount = new int[GetVisualTypeCount];
            for(int i = 0; i < GetVisualTypeCount; i++) {
                gameObjectBanks[i] = new ObjectPool(GetVisualPrefab(i), GameManager.inst.objectParent, 8);
            }
        }



        #region Updates
        /// <summary>
        /// The code an entity must run for every frame of the simulation. Keep this as optimized as possible.
        /// </summary>
        protected abstract void OnUpdateEntity (Entity entity, float deltaTime);
        

        /// <summary>
        /// Runs the simulation of all enabled entities
        /// </summary>
        internal void UpdateSimulation (float deltaTime) {
            foreach(KeyValuePair<ulong, Entity> kvp in entities) {
                ref var networkedEntityComponent = ref kvp.Value.Get<NetworkedEntityComponent>();
                if(networkedEntityComponent.disabledTimer == 0) {
                    OnUpdateEntity(kvp.Value, deltaTime);
                }
            }
        }


        /// <summary>
        /// Updates the sync clock for all entities and force them to sync if the limit was reached.
        /// Destroyes entities that have been disabled for too long.
        /// </summary>
        internal void UpdateNetworkSyncPriorities () {
            foreach(KeyValuePair<ulong, Entity> kvp in entities) {
                ref var networkedEntityComponent = ref kvp.Value.Get<NetworkedEntityComponent>();
                ref var prioritiesComponent = ref kvp.Value.Get<NetworkedEntityPriority>();

                if(networkedEntityComponent.disabledTimer > 0) {
                    if(networkedEntityComponent.disabledTimer == maxDisabledFrame) {
                        DestroyEntity(networkedEntityComponent.id);
                        return;
                    } else {
                        networkedEntityComponent.disabledTimer++;
                    }
                }

                if(prioritiesComponent.shouldBeSync)
                    continue;

                prioritiesComponent.syncClock++;
                if(prioritiesComponent.syncClock == SyncInterval || true) {
                    prioritiesComponent.syncClock = 0;
                    prioritiesComponent.shouldBeSync = true;
                }
            }
        }
        #endregion



        #region Frame Serialization
        /// <summary>
        /// The function that will be called when the systems needs to serialize to a stream a given entity.
        /// </summary>
        protected abstract void OnSerializeEntityFrame (Entity entity, NetworkWriter writer);


        /// <summary>
        /// The function that will be called when the systems needs to deserialize from a stream a given entity.
        /// </summary>
        protected abstract void OnDeserializeEntityFrame (Entity entity, NetworkReader reader);
        
        
        /// <summary>
        /// Tells the systems manager whenever this system needs to be serialized or not
        /// </summary>
        internal bool DoFrameSerialize () {
            if(entities.Count == 0) {
                _cachedValidEntityCount = 0;
                return false;
            }
            int count = 0;
            foreach(KeyValuePair<ulong, Entity> kvp in entities) {
                ref var networkedEntity = ref kvp.Value.Get<NetworkedEntityComponent>();
                if(networkedEntity.disabledTimer == 0) {
                    count++;
                }
            }
            _cachedValidEntityCount = count;
            return _cachedValidEntityCount > 0;
        }


        /// <summary>
        /// Serializes all entities to the stream to be restored when rolling back to a frame
        /// </summary>
        internal void SerializeEntitiesFrame (NetworkWriter writer) {
            writer.WriteUInt16((ushort)_cachedValidEntityCount);
            _cachedValidEntityCount = -1;
            foreach(KeyValuePair<ulong, Entity> kvp in entities) {
                ref var networkedEntity = ref kvp.Value.Get<NetworkedEntityComponent>();
                if(networkedEntity.disabledTimer == 0) {
                    writer.WriteInt32(networkedEntity.spawnFrame);
                    writer.WriteByte(networkedEntity.owner);
                    writer.WriteBits(networkedEntity.copy, 4);
                    OnSerializeEntityFrame(kvp.Value, writer);
                }
            }
        }


        /// <summary>
        /// Deserializes all entities written by SerializeEntitiesFrame
        /// </summary>
        internal void DeserializeEntitiesFrame (NetworkReader reader) {
            ushort entityCount = reader.ReadUInt16();
            for(int i = 0; i < entityCount; i++) {

                int spawnFrame = reader.ReadInt32();
                byte owner = (byte)reader.ReadByte();
                byte copy = (byte)reader.ReadBits(4);
                ulong key = NetworkedEntityComponent.GenerateID(spawnFrame, owner, copy);

                if(entities.TryGetValue(key, out Entity entity)) {
                    ref var networkedEntity = ref entity.Get<NetworkedEntityComponent>();
                    networkedEntity.disabledTimer = 0;
                } else {
                    entity = GetEntity(spawnFrame, owner, copy, out bool didEntityExist);
                }
                OnDeserializeEntityFrame(entity, reader);
            }
        }


        /// <summary>
        /// Disables all entities spawned after or during a certain frame to restore the simulation how it was at this very frame
        /// </summary>
        internal void ClearEntitiesBeforeFrame (int frameIndex) {
            foreach(KeyValuePair<ulong, Entity> kvp in entities) {
                ref var networkedEntity = ref kvp.Value.Get<NetworkedEntityComponent>();
                if(networkedEntity.spawnFrame >= frameIndex) {
                    DisableEntity(kvp.Key);
                }
            }
        }
        #endregion



        #region Network Serialization
        /// <summary>
        /// The function that will be called when the systems needs to serialize to a stream a given entity that needs to be sync with other clients.
        /// </summary>
        protected abstract void OnSerializeEntityNetwork (Entity entity, NetworkWriter writer);


        /// <summary>
        /// The function that will be called when the systems needs to deserialize from a stream a given entity that needed to be sync with this client.
        /// </summary>
        protected abstract void OnDeserializeEntityNetwork (Entity entity, NetworkReader reader, bool didEntityExist);


        /// <summary>
        /// Tells the systems manager whenever any entity in this system need to be sync or not.
        /// </summary>
        internal bool DoNetworkSerialize () {
            if(entities.Count == 0) {
                _cachedValidNetworkEntityCount = 0;
                return false;
            }
            int count = 0;
            foreach(KeyValuePair<ulong, Entity> kvp in entities) {
                ref var networkedEntity = ref kvp.Value.Get<NetworkedEntityComponent>();
                ref var networkedEntityPriority = ref kvp.Value.Get<NetworkedEntityPriority>();
                if(networkedEntity.disabledTimer == 0 && networkedEntityPriority.shouldBeSync) {
                    count++;
                }
            }
            _cachedValidNetworkEntityCount = count;
            return _cachedValidNetworkEntityCount > 0;
        }


        /// <summary>
        /// Serializes all entities to the stream that needed to be sync.
        /// </summary>
        internal void SerializeEntitiesNetwork (NetworkWriter writer) {
            writer.WriteUInt16Packed((ushort)_cachedValidNetworkEntityCount);
            _cachedValidNetworkEntityCount = -1;
            foreach(KeyValuePair<ulong, Entity> kvp in entities) {
                ref var networkedEntity = ref kvp.Value.Get<NetworkedEntityComponent>();
                ref var networkedEntityPriority = ref kvp.Value.Get<NetworkedEntityPriority>();
                if(networkedEntity.disabledTimer == 0 && networkedEntityPriority.shouldBeSync) {
                    networkedEntityPriority.shouldBeSync = false;
                    networkedEntityPriority.syncClock = 0;

                    writer.WriteInt32(networkedEntity.spawnFrame);
                    writer.WriteByte(networkedEntity.owner);
                    writer.WriteBits(networkedEntity.copy, 4);
                    OnSerializeEntityNetwork(kvp.Value, writer);
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

                bool didEntityExist = true;
                if(entities.TryGetValue(key, out Entity entity)) {
                    ref var networkedEntity = ref entity.Get<NetworkedEntityComponent>();
                    networkedEntity.disabledTimer = 0;
                } else {
                    entity = GetEntity(spawnFrame, owner, copy, out didEntityExist);
                }
                OnDeserializeEntityNetwork(entity, reader, didEntityExist);
            }
        }
        #endregion



        #region Spawning / Disableing / Destruction
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
                entity.Get<NetworkedEntityPriority>().shouldBeSync = true;
                networkedEntityComponent.disabledTimer = 0;
                didEntityExist = true;
                return entity;
            }

            entity = SimulationManager.inst.simulationWorld.NewEntity();
            entity.Get<NetworkedEntityComponent>().SetValues(spawnFrame, owner, copy);
            entity.Get<NetworkedEntityPriority>().shouldBeSync = true;
            SetupComponents(entity);
            entities.Add(entityId, entity);
            didEntityExist = false;
            return entity;
        }


        /// <summary>
        /// Disables an entity without actually destroying it and starts a timer to actually destroy it.
        /// </summary>
        protected void DisableEntity (ulong id) {
            if(!entities.TryGetValue(id, out Entity entity))
                return;

            ref var networkedEntityComponent = ref entity.Get<NetworkedEntityComponent>();
            networkedEntityComponent.disabledTimer = 1;

            // Need a way to temp. disable visuals
            int type = GetVisualTypeFromEntity(entity);
            if(gameObjectBanks[type].objectMemory.TryGetValue(networkedEntityComponent.id, out PoolableObject value) && !SimulationManager.inst.IsReplayingFrame) {
                value.ReturnToPool();
            }
        }


        /// <summary>
        /// Removes an entity from the simulation
        /// </summary>
        private void DestroyEntity (ulong id) {
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



        #region Visuals
        public abstract int GetVisualTypeCount { get; }
        protected abstract PoolableObject GetVisualPrefab (int type);
        protected abstract int GetVisualTypeFromEntity(Entity entity);

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
