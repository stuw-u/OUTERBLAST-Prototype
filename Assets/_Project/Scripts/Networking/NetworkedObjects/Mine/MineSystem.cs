using System.Collections.Generic;
using Unity.Jobs;
using MLAPI.Serialization;
using Unity.Mathematics;
using Unity.Collections;
using Blast.ObjectPooling;
using Blast.ECS;
using Blast.ECS.DefaultComponents;
using Blast.NetworkedEntities;

public struct MineComponent {
    public short timer;
    public bool isAnchored;
}

public class MineEntities : NetworkedEntitySystemBase {

    public override int SyncInterval => 4;
    public override int GetVisualTypeCount => 1;


    public void Spawn (int spawnFrame, byte owner, byte copy, float3 position, float3 direction) {
        Entity entity = GetEntity(spawnFrame, owner, copy, out bool didEntityExist);

        entity.Get<MineComponent>() = new MineComponent() {
            timer = 0
        };
        quaternion initRotation = UnityEngine.Quaternion.LookRotation(direction);
        entity.Get<Position>().value = position;
        entity.Get<Rotation>().value = initRotation;
        entity.Get<Velocity>().value = direction * AssetsManager.inst.mineThrowSpeed;
        entity.Get<InterpolatedPositionEntity>().oldPosition = position;
        entity.Get<InterpolatedRotationEntity>().oldRotation = initRotation;
    }

    protected override void SetupComponents (Entity entity) {
        entity.Get<MineComponent>();
        entity.Get<Position>();
        entity.Get<Rotation>();
        entity.Get<Velocity>();
        entity.Get<InterpolatedPositionEntity>() = new InterpolatedPositionEntity() { offsetLerpSmooth = 0.05f, offsetLerpSpeed = 30f };
        entity.Get<InterpolatedRotationEntity>() = new InterpolatedRotationEntity() { offsetSlerpSmooth = 0.2f, offsetSlerpSpeed = 50f };
        entity.Get<LocalToWorld>();
    }

    protected override void OnUpdateEntity (Entity entity, float deltaTime) {

        ref var mine = ref entity.Get<MineComponent>();
        ref var networkedEntity = ref entity.Get<NetworkedEntityComponent>();
        ref var position = ref entity.Get<Position>();
        ref var velocity = ref entity.Get<Velocity>();
        ref var priority = ref entity.Get<NetworkedEntityPriority>();
        ref var localToWorld = ref entity.Get<LocalToWorld>();
        ref var rotation = ref entity.Get<Rotation>();

        // Raycast from current to next position to check for ground (might want to try voxel traversal later for perf.)
        float3 normalizeVel = math.normalizesafe(velocity.value);
        float velLength = math.length(velocity.value) * deltaTime;
        float maxDistance = velLength + 0.1f;

        #region Collision
        if(UnityEngine.Physics.Raycast(position.value - normalizeVel * 0.1f, normalizeVel, out UnityEngine.RaycastHit hit, maxDistance, 1 << 9)) {
            mine.isAnchored = true;
            rotation.value = UnityEngine.Quaternion.LookRotation(hit.normal);
            velocity.value = -hit.normal * 2f;
        } else {
            mine.isAnchored = false;
        }
        #endregion

        ulong projId = networkedEntity.id;
        float3 projPos = position.value;
        int owner = networkedEntity.owner;
        bool isAnchor = mine.isAnchored;
        bool timerAboveLimit = mine.timer > 60;

        SimulationManager.inst.spatialHasherSystem.GetNearEntity(position.value, (e) => {
            if(!e.Has<PlayerComponent>())
                return;
            ref var player = ref e.Get<PlayerComponent>();
            ref var playerPos = ref e.Get<Position>();
            ref var playerRot = ref e.Get<Rotation>();
            ref var playerLocalToWorld = ref e.Get<LocalToWorld>();
            if(e.Has<Ghost>())
                return;
            float3 center = playerPos.value;
            float dist = math.lengthsq(projPos - center);
            float minDist = 2f * 2f;

            if(dist < minDist && ((int)player.clientId != owner || isAnchor || timerAboveLimit)) {
                SimulationManager.inst.SummonTerrainEffect(0, projPos, owner);
                DisableEntity(projId);
            }
        });

        if(mine.isAnchored) {
            return;
        }
        
        if(math.any(normalizeVel == float3.zero))
            rotation.value = UnityEngine.Quaternion.LookRotation(normalizeVel);

        #region Movement
        // Move by velocity
        velocity.value += GravitySystem.GetGravityVector(position.value) * 0.5f * deltaTime;
        position.value += velocity.value * deltaTime;
        #endregion

        #region Timer
        // Tick expiration timer
        mine.timer = (short)math.min(short.MaxValue, mine.timer + 1);
        if(mine.timer > AssetsManager.inst.mineMaxFrames) {
            DisableEntity(networkedEntity.id);
            return;
        }
        #endregion

        position.value = NetUtils.CompressFloat3ToRangePos(position.value);
        velocity.value = NetUtils.CompressFloat3ToRangeVel(velocity.value);
    }
    

    protected override void OnSerializeEntityFrame (Entity entity, NetworkWriter writer) {
        ref var position = ref entity.Get<Position>();
        ref var velocity = ref entity.Get<Velocity>();
        ref var mine = ref entity.Get<MineComponent>();
        
        writer.WriteInt16(mine.timer);
        writer.WriteVector3(position.value);
        writer.WriteBool(mine.isAnchored);
        if(!mine.isAnchored) {
            writer.WriteVector3(velocity.value);
        }
    }

    protected override void OnDeserializeEntityFrame (Entity entity, NetworkReader reader) {
        ref var position = ref entity.Get<Position>();
        ref var velocity = ref entity.Get<Velocity>();
        ref var mine = ref entity.Get<MineComponent>();

        mine.timer = reader.ReadInt16();
        position.value = reader.ReadVector3();
        mine.isAnchored = reader.ReadBool();
        if(!mine.isAnchored) {
            velocity.value = reader.ReadVector3();
        }
    }

    protected override void OnSerializeEntityNetwork (Entity entity, NetworkWriter writer) {
        ref var mine = ref entity.Get<MineComponent>();
        ref var position = ref entity.Get<Position>();
        ref var velocity = ref entity.Get<Velocity>();
        
        writer.WriteUInt16Packed(NetUtils.RangedFloatToUint16Pos(position.value.x));
        writer.WriteUInt16Packed(NetUtils.RangedFloatToUint16Pos(position.value.y));
        writer.WriteUInt16Packed(NetUtils.RangedFloatToUint16Pos(position.value.z));
        writer.WriteBool(mine.isAnchored);
        if(!mine.isAnchored) {
            writer.WriteUInt16Packed(NetUtils.RangedFloatToUint16Vel(velocity.value.x));
            writer.WriteUInt16Packed(NetUtils.RangedFloatToUint16Vel(velocity.value.y));
            writer.WriteUInt16Packed(NetUtils.RangedFloatToUint16Vel(velocity.value.z));
        }
    }

    protected override void OnDeserializeEntityNetwork (Entity entity, NetworkReader reader, bool didEntityExist) {
        ref var position = ref entity.Get<Position>();
        ref var interpPosition = ref entity.Get<InterpolatedPositionEntity>();
        ref var velocity = ref entity.Get<Velocity>();
        ref var mine = ref entity.Get<MineComponent>();
        
        float3 pos = new float3(
            NetUtils.Uint16ToRangedFloatPos(reader.ReadUInt16Packed()),
            NetUtils.Uint16ToRangedFloatPos(reader.ReadUInt16Packed()),
            NetUtils.Uint16ToRangedFloatPos(reader.ReadUInt16Packed()));
        position.value = pos;
        mine.isAnchored = reader.ReadBool();
        if(!mine.isAnchored) {
            float3 vel = new float3(
            NetUtils.Uint16ToRangedFloatVel(reader.ReadUInt16Packed()),
            NetUtils.Uint16ToRangedFloatVel(reader.ReadUInt16Packed()),
            NetUtils.Uint16ToRangedFloatVel(reader.ReadUInt16Packed()));
            velocity.value = vel;
        }
        

        if(!didEntityExist) {
            mine.timer = 0;
            interpPosition.lerpPosition = pos;
            interpPosition.oldPosition = pos;
            interpPosition.doChargeCorrectiveIntepolation = true;
            interpPosition.correctiveInterpolationFrom = pos;
            //quaternion initRot = UnityEngine.Quaternion.LookRotation(GravitySystem.GetGravityDirection(pos));
            //entity.Get<Rotation>().value = initRot;
            //entity.Get<InterpolatedRotationEntity>().oldRotation = initRot;
        }
    }


    protected override PoolableObject GetVisualPrefab (int typeId) {
        return AssetsManager.inst.minePrefab;
    }

    protected override int GetVisualTypeFromEntity (Entity entity) {
        return 0;
    }
}
