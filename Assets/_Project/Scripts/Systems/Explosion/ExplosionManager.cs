using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI.Serialization;
using Unity.Mathematics;
using Eldemarkki.VoxelTerrain.VoxelData;
using Eldemarkki.VoxelTerrain.World.Chunks;
using Eldemarkki.VoxelTerrain.World;
using Blast.ECS;
using Blast.ECS.DefaultComponents;
using Blast.Collections;

public enum ExplosionType {
    Normal,
    Predicted,
    ServerCallback
}

public class ExplosionManager : MonoBehaviour {

    [Header("References")]
    public GameObject explosionPrefab;
    public GameObject rockBreakPrefab;
    public GameObject stepPrefab;
    public GameObject hitPrefab;
    public VoxelDataStore voxelDataStore;

    private static ExplosionManager inst;

    private ParticleSystem explosionInstance;
    private ParticleSystem rockBreakInstance;
    private ParticleSystem stepInstance;
    private ParticleSystem hitInstance;
    private Cinemachine.CinemachineImpulseSource impulseInstance;
    private LimitedQueue<ExplosionVisualisation> lastestVisualization;

    void Awake () {
        inst = this;

        lastestVisualization = new LimitedQueue<ExplosionVisualisation>(16);
        explosionInstance = Instantiate(explosionPrefab, transform).GetComponent<ParticleSystem>();
        rockBreakInstance = Instantiate(rockBreakPrefab, transform).GetComponent<ParticleSystem>();
        stepInstance = Instantiate(stepPrefab, transform).GetComponent<ParticleSystem>();
        hitInstance = Instantiate(hitPrefab, transform).GetComponent<ParticleSystem>();
        impulseInstance = explosionInstance.GetComponent<Cinemachine.CinemachineImpulseSource>();
    }

    private void Start () {
        rockBreakInstance.GetComponent<ParticleSystemRenderer>().material = LobbyWorldInterface.inst.terrainStyle.terrainMaterial;
        explosionInstance.GetComponent<ParticleSystemRenderer>().material = LobbyWorldInterface.inst.terrainStyle.terrainMaterial;
    }


    #region Public Functions
    public static void TriggerExplosionAt (float3 position, ExplosionType type, int claimantId = -1) {
        if(inst == null) {
            throw new Exception("No Explosion Manager instance was found. Maybe the function was called to early or there isn't any Explosion Manager in the scene.");
        }

        inst.Internal_TriggerExplosionAt(position, math.up(), type, claimantId);
    }

    public static void TriggerRockBreakAt (float3 position) {
        if(inst == null) {
            throw new Exception("No Explosion Manager instance was found. Maybe the function was called to early or there isn't any Explosion Manager in the scene.");
        }

        inst.Internal_RockBreakAt(position);
    }

    public static void TriggerSpawnCarve (float3 position) {
        if(inst == null) {
            throw new Exception("No Explosion Manager instance was found. Maybe the function was called to early or there isn't any Explosion Manager in the scene.");
        }

        inst.Internal_SpawnCarveAt(position);
    }

    public static void SpawnStepAt (float3 position) {
        inst.stepInstance.transform.position = position;
        inst.stepInstance.Play(true);
    }

    public static void SpawnHitAt (float3 position, float3 direction) {
        inst.hitInstance.transform.forward = direction;
        inst.hitInstance.transform.position = position;
        inst.hitInstance.Play(true);
    }
    #endregion


    #region Internal Functions
    private void Internal_TriggerExplosionAt (Vector3 position, Vector3 direction, ExplosionType type, int claimantId) {

        // Particles
        if((!NetAssist.IsServer || NetAssist.IsClient) && !SimulationManager.inst.IsReplayingFrame) {
            if(type != ExplosionType.ServerCallback || !CheckForSimilarVisualization(position)) {
                explosionInstance.transform.position = position;
                explosionInstance.Play(true);
                impulseInstance.GenerateImpulse(Vector3.up);
                AudioManager.PlayEnvironmentSoundAt(position, EnvironmentSound.Explosion);

                AddVisualization(position);
            }
        }

        // Forces
        if(type != ExplosionType.ServerCallback) {
            Internal_ApplyExplosionForce(position, claimantId);
        }

        // Terrain Effect
        if((type == ExplosionType.Normal || type == ExplosionType.ServerCallback) && !SimulationManager.inst.IsReplayingFrame) {
            voxelDataStore.GroupedVoxelEdits(
                position,
                new VoxelEditParameters(VoxelEditType.Increment, 2.5f, -0.25f, 0),
                new VoxelEditParameters(VoxelEditType.Max, 7f, 2f, 1),
                new VoxelEditParameters(VoxelEditType.Max, 7f, 1f, 2)
            );
        } else {
            voxelDataStore.GroupedVoxelEdits(
                position,
                new VoxelEditParameters(VoxelEditType.Max, 7f, 2f, 1),
                new VoxelEditParameters(VoxelEditType.Max, 7f, 1f, 2)
            );
        }

        // Blast Probes (Grass Effects)
        if((!NetAssist.IsServer || NetAssist.IsClient) && type != ExplosionType.ServerCallback && !SimulationManager.inst.IsReplayingFrame) {
            voxelDataStore.GetChunksInRegion(
            position - Vector3.one * 24f,
            position + Vector3.one * 24f,
            (c) => {
                c.AddNewBlastProbe(new BlastProbeData(
                    position,
                    10f,
                    Time.time
                ));
            });
        }
    }
    
    private void Internal_ApplyExplosionForce (Vector3 position, int claimantId, float radius = 9f) {
        SimulationManager.inst.explosionSystem.TriggerExplosion(position, radius, 28f, claimantId);
    }

    private void Internal_RockBreakAt (Vector3 position) {
        rockBreakInstance.transform.position = position;
        rockBreakInstance.Play(true);
        AudioManager.PlayEnvironmentSoundAt(position, EnvironmentSound.Dig);

        VoxelWorld.inst.VoxelDataStore.GroupedVoxelEdits(position,
            new VoxelEditParameters(VoxelEditType.Increment, 2f, -0.4f, 0),
            new VoxelEditParameters(VoxelEditType.Min, 3f, 1f, 1),
            new VoxelEditParameters(VoxelEditType.Max, 4f, 1.1f, 2)
        );

        /*if(NetworkAssistant.IsServer && UnityEngine.Random.Range(0, 30) == 0)
            SimulationManager.inst.itemCrateEntities.Spawn(SimulationManager.inst.currentFrame, 0, 1, position);*/
    }

    private void Internal_SpawnCarveAt (Vector3 position) {
        //VoxelWorld.inst.VoxelDataStore.SpawnIslandGeneration((float3)position + math.down() * 1.5f, 4f, 1f);
        VoxelWorld.inst.VoxelDataStore.GroupedVoxelEdits(position,
            new VoxelEditParameters(VoxelEditType.Max, 5f, 1f, 0)
        );
    }
    #endregion


    #region Visualization Checks
    const int frameRange = 60;
    const float distRange = 0.2f;
    private bool CheckForSimilarVisualization (float3 pos) {
        for(int i = 0; i < lastestVisualization.Count; i++) {
            ExplosionVisualisation ev = lastestVisualization.ReserseGet(i);

            if(ev.frameIndex >= SimulationManager.inst.currentFrame - frameRange && ev.frameIndex <= SimulationManager.inst.currentFrame + frameRange) {
                if(math.lengthsq(ev.pos - pos) <= distRange * distRange) {
                    return true;
                }
            }
        }
        return false;
    }


    private void AddVisualization (float3 position) {
        lastestVisualization.Enqueue(new ExplosionVisualisation() { frameIndex = SimulationManager.inst.currentFrame, pos = position });
    }
    #endregion
}

public class BlastSystem : IECSSystem {
    private readonly EcsFilter<Position, Velocity> rigidbodyFilter = null;

    public BlastSystem () {
    }

    public void TriggerExplosion (float3 position, float radius, float force, int claimantId) {
        foreach(var entityIndex in rigidbodyFilter) {

            // Get actual entity reference
            ref var entity = ref rigidbodyFilter.GetEntity(entityIndex);

            if(entity.Has<Ghost>())
                continue;

            // Ref components
            ref var vel = ref entity.Get<Velocity>();
            ref var pos = ref entity.Get<Position>();

            if(entity.Has<Blast.NetworkedEntities.NetworkedEntityComponent>() ) {
                ref var networkedEntity = ref entity.Get<Blast.NetworkedEntities.NetworkedEntityComponent>();
                if(networkedEntity.disabledTimer > 0) {
                    continue;
                }
            }

            float3 diff;
            if(entity.Has<PlayerComponent>()) {
                diff = (pos.value + math.up()) - position;
            } else {
                diff = pos.value - position;
            }

            float len = math.length(diff);
            float3 normal = diff / len;
            float distance = 1f - math.saturate(math.unlerp(2f, radius + 2f, len));
            distance = 1f - ((1f - distance) * (1f - distance) * (1f - distance));
            float3 impulse = normal * distance * force;
            float3 preVel = vel.value;
            vel.value += impulse;

            if(entity.Has<Blast.NetworkedEntities.NetworkedEntityPriority>() && distance > 0f) {
                ref var priority = ref entity.Get<Blast.NetworkedEntities.NetworkedEntityPriority>();
                priority.shouldBeSync = true;
            }
            
            if(entity.Has<PlayerComponent>() && distance > 0f) {
                ref var player = ref entity.Get<PlayerComponent>();

                float damage = distance * 5f;
                if(PlayerUtils.DoDamageShield(ref entity)) {
                    ref var playerLocalToWorld = ref entity.Get<LocalToWorld>();
                    ref var playerInput = ref entity.Get<InputControlledComponent>();
                    float3 hitDir = math.mul(
                        math.mul(new quaternion(playerLocalToWorld.rotationValue), quaternion.Euler(
                            math.radians(playerInput.inputSnapshot.lookAxis.y),
                            math.radians(playerInput.inputSnapshot.lookAxis.x),
                            0f
                        )), math.forward());

                    if(math.dot(normal, -hitDir) > PortableShield.angleConstant) {
                        PlayerUtils.DamageShield(ref entity, damage);
                        vel.value -= impulse;
                        continue;
                    }
                }
                PlayerUtils.ApplyDamage(ref player, damage);
                player.sliperyTimer = 1f;
                vel.value += impulse * (PlayerUtils.DamageToKnockbackMultiplayer(ref player, (ulong)claimantId == player.clientId) - 1f);
                if(claimantId != -1 && (ulong)claimantId != player.clientId) {
                    TaggingSystem.TagEntity(entity, (ulong)claimantId);
                }
            }
        }
    }

    #region Hammer Hits / Apply Forced Velocity
    public bool Hit (ulong hitterId, float3 position, float radius, float3 hitVector, float charge) {
        bool entityHit = false;

        foreach(var entityIndex in rigidbodyFilter) {

            // Get actual entity reference
            ref var entity = ref rigidbodyFilter.GetEntity(entityIndex);
            ref var velC = ref entity.Get<Velocity>();
            ref var posC = ref entity.Get<Position>();

            if(entity.Has<Ghost>())
                continue;

            if(entity.Has<Blast.NetworkedEntities.NetworkedEntityComponent>()) {
                if(entity.Get<Blast.NetworkedEntities.NetworkedEntityComponent>().disabledTimer > 0)
                    continue;
            }

            float3 pos = posC.value;
            float3 vel = velC.value;

            entityHit |= HitOnEntity(entity, ref pos, ref vel, hitterId, position, radius, hitVector, charge);

            posC.value = pos;
            velC.value = vel;
        }

        return entityHit;
    }

    public bool HitOnEntity (Entity entity, ref float3 pos, ref float3 vel, ulong hitterId, float3 hitPos, float radius, float3 hitVector, float charge) {

        if(!Internal_CheckHit(hitVector, pos, hitPos, radius)) {
            return false;
        }
        
        if(entity.Has<PlayerComponent>()) {
            ref var player = ref entity.Get<PlayerComponent>();
            if(player.clientId == hitterId) {
                return false;
            } else {
                float damage = math.lerp(3f, 15f, charge);
                if(PlayerUtils.DoDamageShield(ref entity)) {
                    ref var playerLocalToWorld = ref entity.Get<LocalToWorld>();
                    ref var playerInput = ref entity.Get<InputControlledComponent>();
                    float3 hitDir = math.mul(
                        math.mul(new quaternion(playerLocalToWorld.rotationValue), quaternion.Euler(
                            math.radians(playerInput.inputSnapshot.lookAxis.y),
                            math.radians(playerInput.inputSnapshot.lookAxis.x),
                            0f
                        )), math.forward());
                    
                    if(math.dot(hitVector, -hitDir) > PortableShield.angleConstant) {
                        PlayerUtils.DamageShield(ref entity, damage);
                        return false;
                    }
                }
                TaggingSystem.TagEntity(entity, hitterId);
                vel = hitVector * PlayerUtils.DamageToKnockbackMultiplayer(ref player);
                player.sliperyTimer = 1f;
                PlayerUtils.ApplyDamage(ref player, damage);

                // If you hit the player above in a stacked pile
                if(hitterId == player.anchoredClient) {
                    player.anchoredClient = 255;
                }
                // If you hit the player bellow in a stacked pile
                else if(SimulationManager.inst.playerSystem.playerEntities.TryGetValue(hitterId, out Entity hitterEntity)) {
                    ref var hitterPlayer = ref hitterEntity.Get<PlayerComponent>();
                    if(player.clientId == hitterPlayer.anchoredClient) {
                        hitterPlayer.anchoredClient = 255;
                    }
                }

            }
        } else if(entity.Has<ProjectileComponent>()) {
            ref var projectile = ref entity.Get<ProjectileComponent>();
            ref var priority = ref entity.Get<Blast.NetworkedEntities.NetworkedEntityPriority>();
            priority.shouldBeSync = true;
            vel = hitVector;
        }
            
        return true;
        
    }
    
    private bool Internal_CheckHit (float3 hitVector, float3 entityPos, float3 hitPos, float radius) {
        bool doHit = math.lengthsq(entityPos - hitPos) < radius * radius;
        return doHit;
    }
    #endregion
}

public struct TerrainEffectData {
    public byte type;
    public int3 position;

    public static TerrainEffectData ConvertValueToState (byte type, float3 position) {
        TerrainEffectData newState = new TerrainEffectData() {
            type = type,
            position = (int3)math.round(position)
        };

        return newState;
    }

    public void WriteToStream (NetworkWriter writer) {
        writer.WriteByte(type);
        writer.WriteInt16Packed((short)position.x);
        writer.WriteInt16Packed((short)position.y);
        writer.WriteInt16Packed((short)position.z);
    }

    public static TerrainEffectData ReadFromStream (NetworkReader reader) {
        TerrainEffectData newState = new TerrainEffectData() {
            type = (byte)reader.ReadByte(),
            position = new int3(
                reader.ReadInt16Packed(),
                reader.ReadInt16Packed(),
                reader.ReadInt16Packed()
            )
        };

        return newState;
    }
}

public struct ExplosionVisualisation {
    public float3 pos;
    public int frameIndex;
}