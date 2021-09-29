using System.Runtime.CompilerServices;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Collections;
using Blast.ECS;
using Blast.ECS.DefaultComponents;


public enum GravityFieldType {
    DirectionAligned,
    Spherical
}

public struct GravityField {
    public GravityFieldType gravityFieldType;
    public float3 gravityPositionDirection;
    public float force;
}

public struct AffectedByGravity {
    public bool disable;
}

public struct AlignToGravity {
}



public class GravitySystem : IECSRunSystem, IECSInitSystem {

    private static GravitySystem inst;
    private readonly EcsFilter<AffectedByGravity> entitiesAffectedByGravity = null;
    private readonly EcsFilter<AlignToGravity> entitiesAlignedToGravity = null;

    GravityField mainGravityField = new GravityField() {
        gravityFieldType = GravityFieldType.DirectionAligned,
        gravityPositionDirection = new float3(0f, -1f, 0f),
        force = 29.43f
    };

    public void Init () {
        inst = this;
        
        if(LobbyWorldInterface.inst.terrainType.gravityFieldType == GravityFieldType.DirectionAligned) {
            mainGravityField = new GravityField() {
                gravityFieldType = GravityFieldType.DirectionAligned,
                gravityPositionDirection = new float3(0f, -1f, 0f),
                force = 29.43f
            };
        } else if(LobbyWorldInterface.inst.terrainType.gravityFieldType == GravityFieldType.Spherical) {
            mainGravityField = new GravityField() {
                gravityFieldType = GravityFieldType.Spherical,
                gravityPositionDirection = new float3(0f, 0f, 0f),
                force = 29.43f
            };
        }
    }

    public void Run () {
        
        float deltaTime = UnityEngine.Time.fixedDeltaTime;


        #region Apply Gravity
        foreach(var entityIndex in entitiesAffectedByGravity) {
            
            // Get actual entity reference
            ref var entity = ref entitiesAffectedByGravity.GetEntity(entityIndex);

            // Ref components
            ref var _velocity = ref entity.Get<Velocity>();
            ref var translation = ref entity.Get<Position>();
            ref var gravityEntity = ref entity.Get<AffectedByGravity>();
            
            if(!gravityEntity.disable) {
                _velocity.value += math.select(
                    math.normalizesafe(mainGravityField.gravityPositionDirection - translation.value),
                    mainGravityField.gravityPositionDirection,
                    mainGravityField.gravityFieldType == GravityFieldType.DirectionAligned
                ) * deltaTime * mainGravityField.force;
            }
        }
        #endregion

        #region Align to Gravity
        if(mainGravityField.gravityFieldType == GravityFieldType.Spherical) {
            foreach(var entityIndex in entitiesAlignedToGravity) {
                // Get actual entity reference
                ref var playerEntity = ref entitiesAlignedToGravity.GetEntity(entityIndex);

                // Ref components
                ref var _velocity = ref playerEntity.Get<Velocity>();
                ref var translation = ref playerEntity.Get<Position>();
                ref var rotation = ref playerEntity.Get<Rotation>();
                ref var localToWorld = ref playerEntity.Get<LocalToWorld>();

                // Finds the difference between the entity's up vector and the globalUp vector at its position, create a correction quaternion and apply it.
                quaternion rot = rotation.value;
                float3 globalDown = math.select(
                    math.normalizesafe(mainGravityField.gravityPositionDirection - translation.value),
                    mainGravityField.gravityPositionDirection,
                    mainGravityField.gravityFieldType == GravityFieldType.DirectionAligned
                );
                quaternion corr = UnityEngine.Quaternion.FromToRotation(localToWorld.Up, -globalDown);
                rot = math.mul(corr, rot);

                rotation.value = rot;
            }
        }
        #endregion
    }

    public static float3 GetGravityVector (float3 position) {
        return math.select(
                math.normalizesafe(inst.mainGravityField.gravityPositionDirection - position),
                inst.mainGravityField.gravityPositionDirection,
                inst.mainGravityField.gravityFieldType == GravityFieldType.DirectionAligned
        ) * inst.mainGravityField.force;
    }

    public static float3 GetGravityDirection (float3 position) {
        return math.select(
                math.normalizesafe(inst.mainGravityField.gravityPositionDirection - position),
                inst.mainGravityField.gravityPositionDirection,
                inst.mainGravityField.gravityFieldType == GravityFieldType.DirectionAligned
        );
    }

    public static GravityFieldType GetGravityFieldType () {
        if(LobbyWorldInterface.inst == null)
            return GravityFieldType.DirectionAligned;
        if(LobbyWorldInterface.inst.matchTerrainInfo == null)
            return GravityFieldType.DirectionAligned;
        return LobbyWorldInterface.inst.terrainType.gravityFieldType;
    }
}