using System.Collections.Generic;
using Unity.Jobs;
using MLAPI.Serialization;
using Unity.Mathematics;
using Unity.Collections;
using Blast.ObjectPooling;
using Blast.ECS;
using Blast.ECS.DefaultComponents;
using Blast.NetworkedEntities;

public class ProjectileReplica : NetworkedEntitySystemBase {
    private const float turnSpeed = 180f;

    public override int SyncInterval => 4;
    public override int GetVisualTypeCount => AssetsManager.inst.projectileAssets.Length;


    public void Spawn (int spawnFrame, byte owner, byte copy, byte type, float3 position, float3 direction) {
        Entity entity = GetEntity(spawnFrame, owner, copy, out bool didEntityExist);

        entity.Get<ProjectileComponent>() = new ProjectileComponent() {
            type = type,
            timer = 0
        };
        if(!didEntityExist) {
            entity.Get<ProjectileComponent>().alignDirection = direction;
        }
        quaternion initRotation = UnityEngine.Quaternion.LookRotation(direction);
        entity.Get<Position>().value = position;
        entity.Get<Rotation>().value = initRotation;
        entity.Get<Velocity>().value = direction * AssetsManager.inst.projectileAssets[type].initialSpeed;
        entity.Get<InterpolatedPositionEntity>().oldPosition = position;
        entity.Get<InterpolatedRotationEntity>().oldRotation = initRotation;
    }

    protected override void SetupComponents (Entity entity) {
        entity.Get<ProjectileComponent>();
        entity.Get<Position>();
        entity.Get<Rotation>();
        entity.Get<Velocity>();
        entity.Get<InterpolatedPositionEntity>() = new InterpolatedPositionEntity() { offsetLerpSmooth = 0.05f, offsetLerpSpeed = 30f };
        entity.Get<InterpolatedRotationEntity>() = new InterpolatedRotationEntity() { offsetSlerpSmooth = 0.2f, offsetSlerpSpeed = 50f };
        entity.Get<LocalToWorld>();
    }

    protected override void OnUpdateEntity (Entity entity, float deltaTime) {
        ref var networkedEntity = ref entity.Get<NetworkedEntityComponent>();
        ref var priority = ref entity.Get<NetworkedEntityPriority>();
        ref var projectile = ref entity.Get<ProjectileComponent>();
        ref var localToWorld = ref entity.Get<LocalToWorld>();
        ref var position = ref entity.Get<Position>();
        ref var rotation = ref entity.Get<Rotation>();
        ref var velocity = ref entity.Get<Velocity>();

        // Raycast from current to next position to check for ground (might want to try voxel traversal later for perf.)
        float3 normalizeVel = math.normalizesafe(velocity.value);
        float velLength = math.length(velocity.value) * deltaTime;
        float maxDistance = velLength + 1.6f;

        #region Collision
        // Rocket/player collision
        bool hasCollided = false;
        int owner = networkedEntity.owner;
        float3 copyPos = position.value;
        float3 copyVel = velocity.value;
        ulong projectileId = networkedEntity.id;

        bool doResync = false;

        SimulationManager.inst.spatialHasherSystem.GetNearEntity(copyPos + normalizeVel * velLength + 1f, (e) => {
            if(!e.Has<PlayerComponent>())
                return;
            ref var player = ref e.Get<PlayerComponent>();
            ref var playerPos = ref e.Get<Position>();
            ref var playerRot = ref e.Get<Rotation>();
            ref var playerLocalToWorld = ref e.Get<LocalToWorld>();
            ref var playerInput = ref e.Get<InputControlledComponent>();
            if(e.Has<Ghost>())
                return;
            float3 center = math.mul(playerRot.value, playerLocalToWorld.Up * 0.5f);

            if(PlayerUtils.DoDamageShield(ref e)) {
                if(mathUtils.raySphereIntersection(copyPos, normalizeVel, playerPos.value + center, 2f, out float len2, out float3 point2)) {
                    float3 hitDir = math.mul(
                        math.mul(new quaternion(playerLocalToWorld.rotationValue), quaternion.Euler(
                            math.radians(playerInput.inputSnapshot.lookAxis.y),
                            math.radians(playerInput.inputSnapshot.lookAxis.x),
                            0f
                        )), math.forward());

                    if(len2 < velLength && math.dot(normalizeVel, -hitDir) > PortableShield.angleConstant) {
                        PlayerUtils.DamageShield(ref e, 15f);
                        copyVel *= -0.8f;
                        doResync = true;
                        return;
                    }
                }
            }
            if(mathUtils.raySphereIntersection(copyPos, normalizeVel, playerPos.value + center, 1f, out float len, out float3 point)) {
                if(len < velLength) {
                    player.anchoredClient = 255;
                    SimulationManager.inst.SummonTerrainEffect(0, point + normalizeVel * -1.2f, owner);
                    DisableEntity(projectileId);
                    hasCollided = true;
                }
            }/* else if((ulong)owner != player.clientId) {
                float3 diff = playerPos.value - copyPos;
                float trueDist = math.length(diff);
                float3 normalizedDiff = diff / math.max(0.01f, trueDist);
                float dist = math.saturate(1f - (trueDist * 0.142857f));
                copyVel += diff * deltaTime * dist * 160f;
                doResync = true;
            }*/
        });

        if(doResync) {
            priority.shouldBeSync = true;
            priority.syncClock = 0;
        }
        velocity.value = copyVel;
        if(hasCollided) {
            return;
        }
        if(TerrainSync.IsTerrainAtPoint(position.value)) {

            SimulationManager.inst.SummonTerrainEffect(0, position.value, owner);
            DisableEntity(networkedEntity.id);

            return;
        } else if(UnityEngine.Physics.Raycast(position.value - normalizeVel * 0.95f, normalizeVel, out UnityEngine.RaycastHit hit, maxDistance, 1 << 9)) {

            SimulationManager.inst.SummonTerrainEffect(0, position.value + normalizeVel * hit.distance, owner);
            DisableEntity(networkedEntity.id);

            return;
        }

        #endregion

        #region Rotation
        // Align to velocity
        if(!SimulationManager.inst.IsReplayingFrame) {
            float3 previousDir = projectile.alignDirection;
            projectile.alignDirection = UnityEngine.Vector3.RotateTowards(projectile.alignDirection, math.normalizesafe(velocity.value), math.radians(turnSpeed * deltaTime), 1f);
            if(math.all(projectile.alignDirection != math.up()) && math.all(projectile.alignDirection != math.down())) {
                rotation.value = UnityEngine.Quaternion.LookRotation(projectile.alignDirection);
            }
        }
        #endregion

        #region Movement
        // Move by velocity
        velocity.value += GravitySystem.GetGravityVector(position.value) * 0.5f * deltaTime;
        position.value += velocity.value * deltaTime;
        #endregion

        #region Timer
        // Tick expiration timer
        projectile.timer = (short)math.min(short.MaxValue, projectile.timer + 1);
        if(projectile.timer > AssetsManager.inst.projectileAssets[projectile.type].initTime) {
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
        ref var projectile = ref entity.Get<ProjectileComponent>();
        
        writer.WriteByte(projectile.type);
        writer.WriteInt16(projectile.timer);
        writer.WriteVector3(position.value);
        writer.WriteVector3(velocity.value);
        writer.WriteVector3(projectile.alignDirection);
    }

    protected override void OnDeserializeEntityFrame (Entity entity, NetworkReader reader) {
        ref var position = ref entity.Get<Position>();
        ref var velocity = ref entity.Get<Velocity>();
        ref var projectile = ref entity.Get<ProjectileComponent>();

        projectile.type = (byte)reader.ReadByte();
        projectile.timer = reader.ReadInt16();
        position.value = reader.ReadVector3();
        velocity.value = reader.ReadVector3();
        projectile.alignDirection = reader.ReadVector3();
    }

    protected override void OnSerializeEntityNetwork (Entity entity, NetworkWriter writer) {
        ref var projectile = ref entity.Get<ProjectileComponent>();
        ref var position = ref entity.Get<Position>();
        ref var velocity = ref entity.Get<Velocity>();
        
        writer.WriteByte(projectile.type);
        writer.WriteUInt16Packed(NetUtils.RangedFloatToUint16Pos(position.value.x));
        writer.WriteUInt16Packed(NetUtils.RangedFloatToUint16Pos(position.value.y));
        writer.WriteUInt16Packed(NetUtils.RangedFloatToUint16Pos(position.value.z));
        writer.WriteUInt16Packed(NetUtils.RangedFloatToUint16Vel(velocity.value.x));
        writer.WriteUInt16Packed(NetUtils.RangedFloatToUint16Vel(velocity.value.y));
        writer.WriteUInt16Packed(NetUtils.RangedFloatToUint16Vel(velocity.value.z));
    }

    protected override void OnDeserializeEntityNetwork (Entity entity, NetworkReader reader, bool didEntityExist) {
        ref var position = ref entity.Get<Position>();
        ref var interpPosition = ref entity.Get<InterpolatedPositionEntity>();
        ref var velocity = ref entity.Get<Velocity>();
        ref var projectile = ref entity.Get<ProjectileComponent>();

        byte type = (byte)reader.ReadByte();
        float3 pos = new float3(
            NetUtils.Uint16ToRangedFloatPos(reader.ReadUInt16Packed()),
            NetUtils.Uint16ToRangedFloatPos(reader.ReadUInt16Packed()),
            NetUtils.Uint16ToRangedFloatPos(reader.ReadUInt16Packed()));
        float3 vel = new float3(
            NetUtils.Uint16ToRangedFloatVel(reader.ReadUInt16Packed()),
            NetUtils.Uint16ToRangedFloatVel(reader.ReadUInt16Packed()),
            NetUtils.Uint16ToRangedFloatVel(reader.ReadUInt16Packed()));
        
        position.value = pos;
        velocity.value = vel;
        projectile.type = type;

        if(!didEntityExist) {
            projectile.alignDirection = math.normalizesafe(vel);
            projectile.timer = 0;
            interpPosition.lerpPosition = pos;
            interpPosition.oldPosition = pos;
            interpPosition.doChargeCorrectiveIntepolation = true;
            interpPosition.correctiveInterpolationFrom = pos;
            quaternion initRot = UnityEngine.Quaternion.LookRotation(projectile.alignDirection);
            entity.Get<Rotation>().value = initRot;
            entity.Get<InterpolatedRotationEntity>().oldRotation = initRot;
        }
    }


    protected override PoolableObject GetVisualPrefab (int typeId) {
        return AssetsManager.inst.projectileAssets[typeId].prefab;
    }

    protected override int GetVisualTypeFromEntity (Entity entity) {
        ref var projectile = ref entity.Get<ProjectileComponent>();
        return projectile.type;
    }
}
