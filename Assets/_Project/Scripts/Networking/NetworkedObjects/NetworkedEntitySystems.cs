using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Blast.ECS;
using Blast.ObjectPooling;
using MLAPI.Serialization;

namespace Blast.NetworkedEntities {

    /// <summary>
    /// A class regrouping all NetworkedEntitySystemBase in a simulation. Make sure to call ClearSystems before adding new Networked Entity Systems when creating a simulation.
    /// </summary>
    public sealed class NetworkedEntitySystems {

        private static NetworkedEntitySystems inst;                                     // The current singleton instance
        private List<NetworkedEntitySystemBase> networkedEntitySystems;                 // A list containing all managed systems
        private List<NetworkedStaticEntitySystemBase> networkedStaticEntitySystems;     // A list containing all managed systems
        private byte _currentEntityTypeIndex;


        /// <summary>
        /// Adds a new system to the list of systems and initiates it.
        /// </summary>
        public static void AddSystem (NetworkedEntitySystemBase system) {
            if(inst == null) {
                inst = new NetworkedEntitySystems();
                inst.networkedEntitySystems = new List<NetworkedEntitySystemBase>();
                inst.networkedStaticEntitySystems = new List<NetworkedStaticEntitySystemBase>();
            }
            inst.networkedEntitySystems.Add(system);
            system.entityTypeId = inst._currentEntityTypeIndex;
            system.Init();
            inst._currentEntityTypeIndex++;
        }


        /// <summary>
        /// Adds a new system to the list of systems and initiates it.
        /// </summary>
        public static void AddSystem (NetworkedStaticEntitySystemBase system) {
            if(inst == null) {
                inst = new NetworkedEntitySystems();
                inst.networkedEntitySystems = new List<NetworkedEntitySystemBase>();
                inst.networkedStaticEntitySystems = new List<NetworkedStaticEntitySystemBase>();
            }
            inst.networkedStaticEntitySystems.Add(system);
            system.entityTypeId = inst._currentEntityTypeIndex;
            system.Init();
            inst._currentEntityTypeIndex++;
        }


        /// <summary>
        /// Clears all registered systems.
        /// </summary>
        public static void ClearSystems () {
            if(inst == null)
                return;
            inst.networkedEntitySystems.Clear();
            inst.networkedStaticEntitySystems.Clear();
            inst._currentEntityTypeIndex = 0;
        }


        /// <summary>
        /// Ticks the simulation by one frame for all active entities
        /// </summary>
        public static void UpdateSimulation (float deltaTime) {
            if(inst == null)
                return;
            if(!SimulationManager.inst.IsReplayingFrame) {
                foreach(var system in inst.networkedEntitySystems) {
                    system.UpdateNetworkSyncPriorities();
                }
                if(NetAssist.IsServer) {
                    foreach(var system in inst.networkedStaticEntitySystems) {
                        system.ServerUpdateSimulation(deltaTime);
                    }
                }
            }
            foreach(var system in inst.networkedEntitySystems) {
                system.UpdateSimulation(deltaTime);
            }
        }


        /// <summary>
        /// Must be called every frame. Applies entities onto their corresponding visual object for all systems.
        /// </summary>
        public static void UpdateAllVisuals () {
            if(inst == null)
                return;
            foreach(var system in inst.networkedEntitySystems) {
                system.UpdateVisuals();
            }
            foreach(var system in inst.networkedStaticEntitySystems) {
                system.UpdateVisuals();
            }
        }


        /// <summary>
        /// Serializes all entity to be restored when reapplying a certain frame
        /// </summary>
        public static void SerializeAllEntitiesFrame (NetworkWriter writer) {
            if(inst == null) {
                writer.WriteByte(0);
                return;
            }

            // Count the amount of systems worth serializing
            byte totalSerializedSystems = 0;
            foreach(var system in inst.networkedEntitySystems) {
                if(system.DoFrameSerialize())
                    totalSerializedSystems++;
            }

            // Writes the amount of serializable system and serializes every one of them.
            writer.WriteByte(totalSerializedSystems);
            foreach(var system in inst.networkedEntitySystems) {
                if(!system.DoFrameSerialize())
                    continue;
                writer.WriteByte(system.entityTypeId);
                system.SerializeEntitiesFrame(writer);
            }
        }


        /// <summary>
        /// Deserializes content written by SerializeAllEntitiesFrame
        /// </summary>
        public static bool DeserializeAllEntitiesFrame (NetworkReader reader, int frameIndex) {
            if(inst == null)
                return false;

            // Clears all entities that got spawn after a certain frame to completetly reset the simulation to a later state.
            foreach(var system in inst.networkedEntitySystems) {
                system.ClearEntitiesBeforeFrame(frameIndex);
            }

            // Deserialize systems that worth serializing
            byte systemCount = (byte)reader.ReadByte();
            for(int i = 0; i < systemCount; i++) {
                byte entityTypeId = (byte)reader.ReadByte();
                inst.networkedEntitySystems[entityTypeId].DeserializeEntitiesFrame(reader);
            }
            return true;
        }


        /// <summary>
        /// Serializes all entity that clients might need to get updated.
        /// </summary>
        public static void SerializeAllEntitiesNetwork (NetworkWriter writer) {
            if(inst == null) {
                writer.WriteByte(0);
                return;
            }

            // Count the amout of systems worth serializing
            byte totalSerializedSystems = 0;
            foreach(var system in inst.networkedEntitySystems) {
                if(system.DoNetworkSerialize())
                    totalSerializedSystems++;
            }

            // Writes the amount of serializable system and serializes every one of them.
            writer.WriteByte(totalSerializedSystems);
            foreach(var system in inst.networkedEntitySystems) {
                if(system.DoNetworkSerialize()) {
                    writer.WriteByte(system.entityTypeId);
                    system.SerializeEntitiesNetwork(writer);
                }
            }
        }


        /// <summary>
        /// Deserializes content written by SerializeAllEntitiesFrame
        /// </summary>
        public static bool DeserializeAllEntitiesNetwork (NetworkReader reader) {
            if(inst == null)
                return false;

            // Deserialize systems that worth serializing
            byte systemCount = (byte)reader.ReadByte();
            for(int i = 0; i < systemCount; i++) {
                byte entityId = (byte)reader.ReadByte();
                inst.networkedEntitySystems[entityId].DeserializeEntitiesNetwork(reader);
            }
            return true;
        }


        /// <summary>
        /// Returns true if there's static entities to serialize
        /// </summary>
        /// <returns></returns>
        public static bool DoSerializeStaticEntities () {
            if(inst == null)
                return false;

            for(int i = 0; i < inst.networkedStaticEntitySystems.Count; i++) {
                if(inst.networkedStaticEntitySystems[i].DoNetworkSerialize())
                    return true;

            }
            return false;
        }


        /// <summary>
        /// Serializes all entity that clients might need to get updated.
        /// </summary>
        public static void SerializeAllStaticEntities (NetworkWriter writer) {
            if(inst == null) {
                writer.WriteByte(0);
                return;
            }

            // Count the amout of systems worth serializing
            byte totalSerializedSystems = 0;
            foreach(var system in inst.networkedEntitySystems) {
                if(system.DoNetworkSerialize())
                    totalSerializedSystems++;
            }

            // Writes the amount of serializable system and serializes every one of them.
            writer.WriteByte(totalSerializedSystems);
            foreach(var system in inst.networkedEntitySystems) {
                if(system.DoNetworkSerialize()) {
                    writer.WriteByte(system.entityTypeId);
                    system.SerializeEntitiesNetwork(writer);
                }
            }
        }


        /// <summary>
        /// Deserializes content written by SerializeAllEntitiesFrame
        /// </summary>
        public static bool DeserializeAllStaticEntities (NetworkReader reader) {
            if(inst == null)
                return false;

            // Deserialize systems that worth serializing
            byte systemCount = (byte)reader.ReadByte();
            for(int i = 0; i < systemCount; i++) {
                byte entityId = (byte)reader.ReadByte();
                inst.networkedEntitySystems[entityId].DeserializeEntitiesNetwork(reader);
            }
            return true;
        }
    }
}
