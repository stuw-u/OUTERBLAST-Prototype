using System.Collections.Generic;
using Unity.Jobs;
using MLAPI.Serialization;
using Unity.Mathematics;
using Unity.Collections;
using Blast.ObjectPooling;
using Blast.ECS;
using Blast.ECS.DefaultComponents;

public struct ProjectileStateData {
    public byte type;
    public short timer;
    public float3 position;
    public float3 velocity;
    public float3 alignDirection;

    public int creationFrame;
    public byte owner;
}

public struct ProjectileComponent {
    public byte type;
    public short timer;
    public float3 alignDirection;

    public int creationFrame;
    public byte owner;
}


public class ProjectileSystem : IECSRunSystem {

    private readonly EcsFilter<ProjectileComponent> projectileFilter = null;
    private ObjectBankPool[] gameObjectBanks;
    public Dictionary<ulong, Entity> projectileEntities;
    const float turnSpeed = 180f;
    const int disabledExpiringTime = 240;
    
    public ProjectileSystem (int maxCount) {
        projectileEntities = new Dictionary<ulong, Entity>(maxCount);
        gameObjectBanks = new ObjectBankPool[AssetsManager.inst.projectileAssets.Length];
        for(int i = 0; i < AssetsManager.inst.projectileAssets.Length; i++) {
            gameObjectBanks[i] = new ObjectBankPool(AssetsManager.inst.projectileAssets[i].prefab, GameManager.inst.objectParent, 8);
        }
    }



    // The logic that projectiles will execute every tick of the game's simulation
    #region Simulation
    public void Run () {

        float deltaTime = UnityEngine.Time.fixedDeltaTime;
        foreach(var entityIndex in projectileFilter) {
            
            // Get a valid entity and all its required components
            ref var entity = ref projectileFilter.GetEntity(entityIndex);
            ref var enableState = ref entity.Get<EnableState>();
            ref var projectile = ref entity.Get<ProjectileComponent>();
            ref var localToWorld = ref entity.Get<LocalToWorld>();
            ref var position = ref entity.Get<Position>();
            ref var rotation = ref entity.Get<Rotation>();
            ref var velocity = ref entity.Get<Velocity>();
            if(!enableState.isEnabled) {
                if(!SimulationManager.inst.IsReplayingFrame) {
                    enableState.timer++;
                    if(enableState.timer > disabledExpiringTime) {
                        DestroyProjectile(GenerateId(projectile));
                    }
                }
                continue;
            }
            
            // Raycast from current to next position to check for ground (might want to try voxel traversal later for perf.)
            float3 normalizeVel = math.normalizesafe(velocity.value);
            float velLength = math.length(velocity.value) * deltaTime;
            float maxDistance = velLength + 1.6f;

            #region Collision
            // Rocket/player collision
            bool hasCollided = false;
            int owner = projectile.owner;
            ulong projectileId = GenerateId(projectile);
            float3 copyPos = position.value;
            float3 copyVel = velocity.value;
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
                            return;
                        }
                    }
                }
                if(mathUtils.raySphereIntersection(copyPos, normalizeVel, playerPos.value + center, 1f, out float len, out float3 point)) {
                    if(len < velLength) {

                        SimulationManager.inst.SummonTerrainEffect(0, point + normalizeVel * -1.2f, owner);
                        DisableProjectile(projectileId);
                        hasCollided = true;
                    }
                } else if((ulong)owner != player.clientId) {
                    float3 diff = playerPos.value - copyPos;
                    float trueDist = math.length(diff);
                    float3 normalizedDiff = diff / math.max(0.01f, trueDist);
                    float dist = math.saturate(1f - (trueDist * 0.142857f));
                    copyVel += diff * deltaTime * dist * 160f;
                }
            });
            velocity.value = copyVel;
            if(hasCollided) {
                continue;
            }
            if(TerrainSync.IsTerrainAtPoint(position.value)) {

                SimulationManager.inst.SummonTerrainEffect(0, position.value, projectile.owner);
                DisableProjectile(GenerateId(projectile));

                continue;
            } else if(UnityEngine.Physics.Raycast(position.value - normalizeVel * 0.95f, normalizeVel, out UnityEngine.RaycastHit hit, maxDistance, 1 << 9)) {

                SimulationManager.inst.SummonTerrainEffect(0, position.value + normalizeVel * hit.distance, projectile.owner);
                DisableProjectile(GenerateId(projectile));

                continue;
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
                DisableProjectile(GenerateId(projectile));
                continue;
            }
            #endregion

            position.value = NetUtils.CompressFloat3ToRangePos(position.value);
            velocity.value = NetUtils.CompressFloat3ToRangeVel(velocity.value);
        }
    }
    #endregion


    // The system used to copy entities into gameobjects for visualization
    #region Visualisation
    public void ApplyComponentsOnGameObject () {


        // Prepares enough gameObjects for all projectiles of all types
        NativeArray<int> totalTypeCount = new NativeArray<int>(AssetsManager.inst.projectileAssets.Length, Allocator.Temp);
        foreach(var entityIndex in projectileFilter) {


            // Get a valid entity and all its required components
            ref var entity = ref projectileFilter.GetEntity(entityIndex);
            ref var enableState = ref entity.Get<EnableState>();
            if(!enableState.isEnabled)
                continue;
            ref var projectile = ref entity.Get<ProjectileComponent>();


            // Count how many projectiles of all types are valid
            totalTypeCount[projectile.type]++;

        }


        // Prepares the required amout of projectiles of all type
        for(int i = 0; i < totalTypeCount.Length; i++)
            gameObjectBanks[i].GenerateFixedCount(totalTypeCount[i]);
        totalTypeCount.Dispose();


        // Apply components on projectiles
        NativeArray<int> typeIndex = new NativeArray<int>(AssetsManager.inst.projectileAssets.Length, Allocator.Temp);
        foreach(var entityIndex in projectileFilter) {


            // Get a valid entity and all its required components
            ref var entity = ref projectileFilter.GetEntity(entityIndex);
            ref var enableState = ref entity.Get<EnableState>();
            if(!enableState.isEnabled)
                continue;
            ref var projectile = ref entity.Get<ProjectileComponent>();
            ref var position = ref entity.Get<InterpolatedPositionEntity>();
            ref var rotation = ref entity.Get<InterpolatedRotationEntity>();


            // Apply the projectile entity's state on the object
            var pooledObject = gameObjectBanks[projectile.type].objectMemory[typeIndex[projectile.type]];
            pooledObject.transform.position = position.lerpPosition;
            pooledObject.transform.rotation = rotation.slerpRotation;
            typeIndex[projectile.type]++;

        }
        typeIndex.Dispose();

    }
    #endregion


    // The logic needed to create and remove projectiles
    #region Spawning
    /// <summary>
    /// Spawns a projectile and returns a code that can be used to despawn the projectile later.
    /// The code is simulation-dependant, meaning it should be the same if the inputs are played
    /// deterministicly on the server.
    /// 
    /// CODE BYTE INDEXES:
    /// 0-2: Unused (Might be used later to allow multiple proj. per frame to be spawned)
    /// 3  : Spawner clientID (There shouldn't be 255 player joining a game ever (especially not at the same time, but nor one after tehe other))
    /// 4-7: Creation Frame Index
    /// </summary>
    public ulong SpawnProjectileByLocal (float3 position, float3 direction, byte type, byte owner, int creationFrame) {

        ulong projectileId = GenerateId(owner, creationFrame);
        bool didProjectileExist = projectileEntities.ContainsKey(projectileId);
        Entity entity = SpawnOrEnableProjectile(projectileId);

        entity.Get<ProjectileComponent>() = new ProjectileComponent() {
            creationFrame = creationFrame,
            owner = owner,
            type = type,
            timer = 0
        };
        if(!didProjectileExist) {
            entity.Get<ProjectileComponent>().alignDirection = direction;
        }
        quaternion initRotation = UnityEngine.Quaternion.LookRotation(direction);
        entity.Get<Position>() = new Position() { value = position };
        entity.Get<Rotation>() = new Rotation() { value = initRotation };
        entity.Get<Velocity>() = new Velocity() { value = direction * AssetsManager.inst.projectileAssets[type].initialSpeed };
        entity.Get<InterpolatedPositionEntity>() = new InterpolatedPositionEntity() { offsetLerpSmooth = 0.05f, offsetLerpSpeed = 30f, oldPosition = position};
        entity.Get<InterpolatedRotationEntity>() = new InterpolatedRotationEntity() { offsetSlerpSmooth = 0.2f, offsetSlerpSpeed = 50f, oldRotation = initRotation };

        return projectileId;
    }

    /// <summary>
    /// Spawn an empty projectile that isn't altered by default, and adds it to the dictionnairy
    /// </summary>
    public Entity SpawnOrEnableProjectile (ulong projectileId) {
        if(projectileEntities.TryGetValue(projectileId, out Entity entity)) {
            ref var enableState = ref entity.Get<EnableState>();
            enableState.isEnabled = true;
            enableState.timer = 0;
            return entity;
        }

        entity = SimulationManager.inst.simulationWorld.NewEntity();
        entity.Get<ProjectileComponent>() = new ProjectileComponent();
        entity.Get<EnableState>() = new EnableState() { isEnabled = true };
        entity.Get<Position>();
        entity.Get<Velocity>();
        entity.Get<InterpolatedPositionEntity>();
        entity.Get<InterpolatedRotationEntity>() = new InterpolatedRotationEntity() { offsetSlerpSmooth = 0.2f, offsetSlerpSpeed = 50f, slerpRotation = quaternion.identity };
        //entity.Get<AffectedByGravity>();
        entity.Get<LocalToWorld>();

        projectileEntities.Add(projectileId, entity);

        return entity;
    }

    /// <summary>
    /// Removes from the entity world a given projectileID, forever. Warning: do not destroy enabled projectiles please! They might still be revived by the server!
    /// </summary>
    public void DestroyProjectile (ulong projectileID) {
        if(projectileEntities.TryGetValue(projectileID, out Entity entity)) {
            projectileEntities.Remove(projectileID);
            entity.Destroy();
        }
    }

    /// <summary>
    /// Removes from the entity world a given projectileID. (Update: Now the projectiles aren't truely deleted, they are just deactivated temporarly)
    /// so interpolation still function as expected)
    /// </summary>
    public void DisableProjectile (ulong projectileID) {
        if(projectileEntities.TryGetValue(projectileID, out Entity entity)) {

            ref var enableState = ref entity.Get<EnableState>();
            enableState.isEnabled = false;
        }
    }

    /// <summary>
    /// Despawn projectile spawned after a certain frame
    /// </summary>
    public void DisableProjectileAfterFrame (int frameIndex) {
        foreach(var entityIndex in projectileFilter) {
            
            ref var entity = ref projectileFilter.GetEntity(entityIndex);
            ref var projectile = ref entity.Get<ProjectileComponent>();
            ref var enableState = ref entity.Get<EnableState>();
            if(!enableState.isEnabled)
                continue;

            if(projectile.creationFrame >= frameIndex) {
                DisableProjectile(GenerateId(projectile.owner, projectile.creationFrame));
            }
        }
    }
    #endregion


    // The system responsable to keeping in memory previous world state for server reconcilitation
    #region Frame Serialization
    public NativeArray<ProjectileStateData> SerializeFrame () {

        // Found how many projectiles need to be serialized
        int validProjectileCount = 0;
        foreach(var entityIndex in projectileFilter) {

            ref var entity = ref projectileFilter.GetEntity(entityIndex);
            ref var enableState = ref entity.Get<EnableState>();
            if(!enableState.isEnabled)
                continue;
            ref var projectile = ref entity.Get<ProjectileComponent>();

            validProjectileCount++;
            if(validProjectileCount == 255)
                break;
        }

        var projectileStateDataLocal = new NativeArray<ProjectileStateData>(validProjectileCount, Allocator.Persistent);

        int i = 0;
        foreach(var entityIndex in projectileFilter) {

            ref var entity = ref projectileFilter.GetEntity(entityIndex);
            ref var enableState = ref entity.Get<EnableState>();
            if(!enableState.isEnabled)
                continue;
            ref var projectile = ref entity.Get<ProjectileComponent>();
            ref var position = ref entity.Get<Position>();
            ref var velocity = ref entity.Get<Velocity>();

            projectileStateDataLocal[i] = new ProjectileStateData() {
                position = position.value,
                velocity = velocity.value,
                alignDirection = projectile.alignDirection,
                timer = projectile.timer,
                type = projectile.type,
                creationFrame = projectile.creationFrame,
                owner = projectile.owner
            };

            i++;
        }
        return projectileStateDataLocal;
    }

    public void DeserializeFrame (int dataFrameIndex, NativeArray<ProjectileStateData> projectileStateDatas) {
        DisableProjectileAfterFrame(dataFrameIndex);
        foreach(ProjectileStateData stateData in projectileStateDatas) {
            ulong projectileId = GenerateId(stateData.owner, stateData.creationFrame);

            Entity entity;
            if(!projectileEntities.TryGetValue(projectileId, out entity)) {

                //Spawn a new one
                entity = SpawnOrEnableProjectile(projectileId);
            }

            // Ref components
            ref var projectile = ref entity.Get<ProjectileComponent>();
            ref var position = ref entity.Get<Position>();
            ref var velocity = ref entity.Get<Velocity>();
            ref var enableState = ref entity.Get<EnableState>();

            // Apply components
            position.value = stateData.position;
            velocity.value = stateData.velocity;
            //projectile.alignDirection = stateData.alignDirection;
            projectile.creationFrame = stateData.creationFrame;
            projectile.owner = stateData.owner;
            projectile.timer = stateData.timer;
            projectile.type = stateData.type;
        }
    }
    #endregion


    // The system responsable for making sure the client projectiles are the same as server projectiles
    #region Network Serialization
    public void SerializeNetwork (NetworkWriter writer) {

        // Found how many projectiles need to be updated
        int validProjectileCount = 0;
        foreach(var entityIndex in projectileFilter) {

            ref var entity = ref projectileFilter.GetEntity(entityIndex);
            ref var enableState = ref entity.Get<EnableState>();
            if(!enableState.isEnabled)
                continue;
            ref var projectile = ref entity.Get<ProjectileComponent>();

            validProjectileCount++;
            if(validProjectileCount == 255)
                break;
        }


        // Write the count of valid projectiles to serialize
        writer.WriteByte((byte)validProjectileCount);


        // Serialize all projectiles
        foreach(var entityIndex in projectileFilter) {
            
            ref var entity = ref projectileFilter.GetEntity(entityIndex);
            ref var enableState = ref entity.Get<EnableState>();
            if(!enableState.isEnabled)
                continue;
            ref var projectile = ref entity.Get<ProjectileComponent>();
            ref var position = ref entity.Get<Position>();
            ref var velocity = ref entity.Get<Velocity>();

            writer.WriteByte(projectile.owner);
            writer.WriteInt32Packed(projectile.creationFrame);
            writer.WriteByte(projectile.type);
            writer.WriteUInt16Packed(NetUtils.RangedFloatToUint16Pos(position.value.x));
            writer.WriteUInt16Packed(NetUtils.RangedFloatToUint16Pos(position.value.y));
            writer.WriteUInt16Packed(NetUtils.RangedFloatToUint16Pos(position.value.z));
            writer.WriteUInt16Packed(NetUtils.RangedFloatToUint16Vel(velocity.value.x));
            writer.WriteUInt16Packed(NetUtils.RangedFloatToUint16Vel(velocity.value.y));
            writer.WriteUInt16Packed(NetUtils.RangedFloatToUint16Vel(velocity.value.z));
            validProjectileCount--;
            if(validProjectileCount == 0)
                break;
        }
    }

    public void DeserializeNetwork (NetworkReader reader) {
        int projectileCount = reader.ReadByte();

        for(int i = 0; i < projectileCount; i++) {
            byte owner = (byte)reader.ReadByte();
            int creationFrame = reader.ReadInt32Packed();
            ulong id = GenerateId(owner, creationFrame);

            byte type = (byte)reader.ReadByte();
            float3 pos = new float3(
                NetUtils.Uint16ToRangedFloatPos(reader.ReadUInt16Packed()),
                NetUtils.Uint16ToRangedFloatPos(reader.ReadUInt16Packed()),
                NetUtils.Uint16ToRangedFloatPos(reader.ReadUInt16Packed()));
            float3 vel = new float3(
                NetUtils.Uint16ToRangedFloatVel(reader.ReadUInt16Packed()),
                NetUtils.Uint16ToRangedFloatVel(reader.ReadUInt16Packed()),
                NetUtils.Uint16ToRangedFloatVel(reader.ReadUInt16Packed()));

            Entity entity;
            if(projectileEntities.TryGetValue(id, out entity)) {
                ref var enableState = ref entity.Get<EnableState>();
                ref var projectile = ref entity.Get<ProjectileComponent>();
                enableState.isEnabled = true;
                entity.Get<Position>().value = pos;
                entity.Get<Velocity>().value = vel;
                continue;
            }
            entity = SpawnOrEnableProjectile(id);
            entity.Get<Position>().value = pos;
            entity.Get<Velocity>().value = vel;
            entity.Get<ProjectileComponent>() = new ProjectileComponent() {
                alignDirection = math.normalizesafe(vel),
                creationFrame = creationFrame,
                owner = owner,
                type = type,
                timer = 1200
            };
            entity.Get<InterpolatedPositionEntity>() = new InterpolatedPositionEntity() {
                offsetLerpSmooth = 0.05f,
                offsetLerpSpeed = 30f,
                lerpPosition = pos,
                oldPosition = pos,
                doChargeCorrectiveIntepolation = true,
                correctiveInterpolationFrom = pos
            };
            quaternion initRotation = UnityEngine.Quaternion.LookRotation(math.normalizesafe(vel));
            entity.Get<Rotation>() = new Rotation() { value = initRotation };
            entity.Get<InterpolatedRotationEntity>() = new InterpolatedRotationEntity() { offsetSlerpSmooth = 0.2f, offsetSlerpSpeed = 50f, oldRotation = initRotation };
        }
    }
    #endregion



    public static ulong GenerateId (byte owner, int creationFrame) {
        uint ua = (uint)creationFrame;
        ulong ub = (ulong)owner;
        return ub << 32 | ua;
    }

    public static ulong GenerateId (ProjectileComponent projectile) {
        return GenerateId(projectile.owner, projectile.creationFrame);
    }
}
