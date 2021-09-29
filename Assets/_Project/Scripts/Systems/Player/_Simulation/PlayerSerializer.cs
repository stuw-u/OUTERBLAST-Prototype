using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;
using Blast.ECS;
using MLAPI.Serialization;
using MLAPI.Serialization.Pooled;
using Blast.ECS.DefaultComponents;

public static class PlayerSerializer {
    #region Frame Serialization
    
    /// <summary>
    /// Creates a native hash map with clientIDs as keys and player state data as type.
    /// This functions essentially extracts usful components in entities into a PlayerStateData struct.
    /// </summary>
    public static NativeHashMap<byte, PlayerStateData> SerializeFrame (PlayerSystem system) {

        NativeHashMap<byte, PlayerStateData> playerData = new NativeHashMap<byte, PlayerStateData>(system.PlayerCount, Allocator.Persistent);
        system.RunOnAllPlayers((playerEntity) => {
            // Ref components
            ref var player = ref playerEntity.Get<PlayerComponent>();
            ref var translate = ref playerEntity.Get<Position>();
            ref var rotation = ref playerEntity.Get<Rotation>();
            ref var velocity = ref playerEntity.Get<Velocity>();

            playerData[(byte)player.clientId] = new PlayerStateData() {
                position = translate.value,
                rotation = rotation.value,
                velocity = velocity.value,
                jumpTimer = player.jumpTimer,
                sliperyTimer = player.sliperyTimer,
                damage = player.damage,
                inAirByJump = player.inAirByJump,
                anchoredClient = player.anchoredClient,
                clientId = (byte)player.clientId,
                coyoteTimer = player.coyoteTimer,
                motorState = player.motorState
            };
        });
        return playerData;
    }



    /// <summary>
    /// Deserialize a native hashmap of players back into a player system
    /// </summary>
    public static void DeserializeFrame (PlayerSystem system, NativeHashMap<byte, PlayerStateData> playerDatas) {
        
        system.RunOnAllPlayers((playerEntity) => {
            // Ref components
            ref var player = ref playerEntity.Get<PlayerComponent>();
            ref var translate = ref playerEntity.Get<Position>();
            ref var rotation = ref playerEntity.Get<Rotation>();
            ref var velocity = ref playerEntity.Get<Velocity>();

            if(player.behaviour == PlayerEntityBehaviour.ReplicaSimulated) {
                return;
            }
            if(!playerDatas.ContainsKey((byte)player.clientId)) {
                return;
            }
            var playerData = playerDatas[(byte)player.clientId];

            translate.value = playerData.position;
            rotation.value = playerData.rotation;
            velocity.value = playerData.velocity;
            player.sliperyTimer = playerData.sliperyTimer;
            player.damage = playerData.damage;
            player.jumpTimer = playerData.jumpTimer;
            player.coyoteTimer = playerData.coyoteTimer;
            player.inAirByJump = playerData.inAirByJump;
            player.anchoredClient = playerData.anchoredClient;
            player.motorState = playerData.motorState;
        });
    }



    public static void SerializeItem (PlayerSystem system, NetworkWriter writer) {
        int count = 0;
        foreach(KeyValuePair<ulong, LocalItemManager> kvp in system.itemManagers) {
            if(system.playerEntities.ContainsKey(kvp.Key)) {
                count++;
            }
        }
        writer.WriteByte((byte)count);
        foreach(KeyValuePair<ulong, LocalItemManager> kvp in system.itemManagers) {
            if(system.playerEntities.ContainsKey(kvp.Key)) {
                writer.WriteUInt64(kvp.Key);
                kvp.Value.SerializeFrame(ref system.playerEntities[kvp.Key].Get<ItemUserComponent>(), writer);
            }
        }
    }

    public static void DeserializeItem (PlayerSystem system, NetworkReader reader) {
        int count = reader.ReadByte();
        for(int i = 0; i < count; i++) {
            ulong key = reader.ReadUInt64();
            system.itemManagers[key].DeserializeFrame(ref system.playerEntities[key].Get<ItemUserComponent>(), reader);
        }
    }
    #endregion



    #region Network Serialization
    public static void SerializeNetwork (PlayerSystem system, NetworkWriter writer) {

        writer.WriteByte((byte)system.PlayerCount);
        system.RunOnAllPlayers((entity) => {
            ref var player = ref entity.Get<PlayerComponent>();
            ref var input = ref entity.Get<InputControlledComponent>();
            ref var itemUser = ref entity.Get<ItemUserComponent>();
            ref var translate = ref entity.Get<Position>();
            ref var rotation = ref entity.Get<Rotation>();
            ref var velocity = ref entity.Get<Velocity>();
            bool ghost = entity.Has<Ghost>();
            bool isPlayerValid = NetUtils.IsPlayerValid(translate.value, ghost);

            // Write compressed player data required by the client
            writer.WriteByte((byte)player.clientId);
            writer.WriteBit(isPlayerValid);
            if(!isPlayerValid) {
                return;
            }

            writer.WriteUInt16Packed(NetUtils.RangedFloatToUint16Pos(translate.value.x));
            writer.WriteUInt16Packed(NetUtils.RangedFloatToUint16Pos(translate.value.y));
            writer.WriteUInt16Packed(NetUtils.RangedFloatToUint16Pos(translate.value.z));

            writer.WriteUInt16Packed(NetUtils.RangedFloatToUint16Vel(velocity.value.x));
            writer.WriteUInt16Packed(NetUtils.RangedFloatToUint16Vel(velocity.value.y));
            writer.WriteUInt16Packed(NetUtils.RangedFloatToUint16Vel(velocity.value.z));

            writer.WriteCompressedRotation(rotation.value);
            writer.WriteByte((byte)math.floor(math.unlerp(0f, 360f, input.inputSnapshot.lookAxis.x) * 255f));
            writer.WriteByte((byte)math.floor(math.unlerp(-90f, 90f, input.inputSnapshot.lookAxis.y) * 255f));
            writer.WriteByte((byte)player.damage);

            system.itemManagers[player.clientId].SerializeNetwork(ref itemUser, writer);

            writer.WriteBit(player.anchoredClient != 255);
            if(player.anchoredClient != 255) {
                writer.WriteByte(player.anchoredClient);
            }
        });
    }


    public static void DeserializeNetwork (PlayerSystem system, float localReceivingTime, NetworkReader reader) {

        int entityCount = reader.ReadByte();
        for(int i = 0; i < entityCount; i++) {
            ulong playerId = System.Convert.ToUInt64(reader.ReadByte());

            #region Player Missing
            if(!system.playerEntities.ContainsKey(playerId)) {
                Debug.LogError("Error! Player unknown! We may have had a bad read!");
                if(reader.ReadBit()) {

                    // Fake Read
                    reader.ReadUInt16Packed();
                    reader.ReadUInt16Packed();
                    reader.ReadUInt16Packed();
                    reader.ReadUInt16Packed();
                    reader.ReadUInt16Packed();
                    reader.ReadUInt16Packed();
                    reader.ReadCompressedRotation();
                    reader.ReadByte();
                    reader.ReadByte();
                    reader.ReadByte();
                    reader.ReadByte();
                    reader.ReadBit();
                    system.itemManagers[playerId].DeserializeNetworkFake(reader);
                }
                continue;
            }
            #endregion

            if(reader.ReadBit()) {
                PlayerUtils.SetGhostStatus(playerId, false);

                var entity = system.playerEntities[playerId];
                ref var player = ref entity.Get<PlayerComponent>();
                ref var input = ref entity.Get<InputControlledComponent>();
                ref var itemUser = ref entity.Get<ItemUserComponent>();
                ref var translate = ref entity.Get<Position>();
                ref var rotation = ref entity.Get<Rotation>();
                ref var velocity = ref entity.Get<Velocity>();

                float3 pos = new float3(
                    NetUtils.Uint16ToRangedFloatPos(reader.ReadUInt16Packed()),
                    NetUtils.Uint16ToRangedFloatPos(reader.ReadUInt16Packed()),
                    NetUtils.Uint16ToRangedFloatPos(reader.ReadUInt16Packed()));
                float3 vel = new float3(
                    NetUtils.Uint16ToRangedFloatVel(reader.ReadUInt16Packed()),
                    NetUtils.Uint16ToRangedFloatVel(reader.ReadUInt16Packed()),
                    NetUtils.Uint16ToRangedFloatVel(reader.ReadUInt16Packed()));
                quaternion rot = reader.ReadCompressedRotation();
                float2 lookAxis = new float2(math.lerp(0f, 360f, reader.ReadByte() / 255f), math.lerp(-90f, 90f, reader.ReadByte() / 255f));
                player.damage = reader.ReadByte();

                system.itemManagers[player.clientId].DeserializeNetwork(SimulationManager.inst.localPlayers[player.clientId].IsOwnedByInstance, ref itemUser, reader);

                if(reader.ReadBit()) {
                    player.anchoredClient = (byte)reader.ReadByte();
                } else {
                    player.anchoredClient = 255;
                }

                if(!SimulationManager.inst.localPlayers[player.clientId].IsOwnedByInstance) {
                    translate.value = pos;
                    velocity.value = vel;
                    rotation.value = rot;

                    input.inputSnapshot.lookAxis = lookAxis;
                    input.inputSnapshot.SetMoveAxisRaw(127, 127);
                    SimulationManager.inst.localPlayers[player.clientId].PlayerGameObject.SetInterpolatedAxis(input.inputSnapshot.lookAxis, localReceivingTime);
                }
            } else {
                PlayerUtils.SetGhostStatus(playerId, true);
            }
        }
    }


    public static void SerializeSelfPlayer (PlayerSystem system, NetworkWriter writer, ulong playerId) {

        Entity selfPlayer = system.playerEntities[playerId];
        ref var player = ref selfPlayer.Get<PlayerComponent>();
        ref var translate = ref selfPlayer.Get<Position>();
        ref var velocity = ref selfPlayer.Get<Velocity>();
        ref var rotation = ref selfPlayer.Get<Rotation>();
        ref var itemUser = ref selfPlayer.Get<ItemUserComponent>();
        bool isGhost = selfPlayer.Has<Ghost>();

        writer.WriteBit(!isGhost);
        if(isGhost)
            return;

        writer.WriteSinglePacked(translate.value.x);
        writer.WriteSinglePacked(translate.value.y);
        writer.WriteSinglePacked(translate.value.z);
        writer.WriteSinglePacked(velocity.value.x);
        writer.WriteSinglePacked(velocity.value.y);
        writer.WriteSinglePacked(velocity.value.z);
        writer.WriteRotation(rotation.value);
        system.itemManagers[playerId].SerializeFrame(ref itemUser, writer);
        writer.WriteBit(player.anchoredClient != 255);
    }


    public static void DeserializeSelfPlayer (PlayerSystem system, NetworkReader reader, ulong playerId) {

        Entity selfPlayer = system.playerEntities[playerId];
        ref var player = ref selfPlayer.Get<PlayerComponent>();
        ref var translate = ref selfPlayer.Get<Position>();
        ref var velocity = ref selfPlayer.Get<Velocity>();
        ref var rotation = ref selfPlayer.Get<Rotation>();
        ref var itemUser = ref selfPlayer.Get<ItemUserComponent>();

        if(reader.ReadBit()) {
            translate.value = new float3(
                reader.ReadSinglePacked(),
                reader.ReadSinglePacked(),
                reader.ReadSinglePacked()
            );
            velocity.value = new float3(
                reader.ReadSinglePacked(),
                reader.ReadSinglePacked(),
                reader.ReadSinglePacked()
            );
            rotation.value = reader.ReadRotation();
            system.itemManagers[playerId].DeserializeFrame(ref itemUser, reader);
        }
    }
    #endregion
}
