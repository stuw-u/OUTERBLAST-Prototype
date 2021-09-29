using Unity.Burst;
using Unity.Mathematics;
using Unity.Collections;
using Blast.ECS;
using Blast.ECS.DefaultComponents;


public struct InterpolatedPositionEntity {
    public float offsetLerpSmooth;
    public float offsetLerpSpeed;
    
    public float3 posOffset;

    public float3 lerpPosition;
    public float3 oldPosition;

    public bool doChargeCorrectiveIntepolation;
    public float3 correctiveInterpolationFrom;
}  

public struct InterpolatedRotationEntity {
    public float offsetSlerpSmooth;
    public float offsetSlerpSpeed;
    
    public quaternion rotOffset;

    public quaternion slerpRotation;
    public quaternion oldRotation;

    public bool doChargeCorrectiveIntepolation;
    public quaternion correctiveInterpolationFrom;
}


public class InterpolationSystem : IECSSystem {

    private readonly EcsFilter<InterpolatedPositionEntity, Position> positionEntity = null;
    private readonly EcsFilter<InterpolatedRotationEntity, Rotation> rotationEntity = null;

    public InterpolationSystem () {
    }

    // Gather position data (Must be run before all other sim. systems!)
    public void Run () {
        foreach(var entityIndex in positionEntity) {

            // Get actual entity reference
            ref var entity = ref positionEntity.GetEntity(entityIndex);

            // Ref components
            ref var interp = ref entity.Get<InterpolatedPositionEntity>();
            ref var pos = ref entity.Get<Position>();

            // Get position
            interp.oldPosition = pos.value;
        }

        foreach(var entityIndex in rotationEntity) {

            // Get actual entity reference
            ref var entity = ref rotationEntity.GetEntity(entityIndex);

            // Ref components
            ref var interp = ref entity.Get<InterpolatedRotationEntity>();
            ref var rot = ref entity.Get<Rotation>();

            // Get rotation
            interp.oldRotation = rot.value;
        }
    }

    // Finds interpolation position data (Must be run in the update loop! Do not change pos. in update though!)
    public void Apply () {

        // Init time stuff
        float deltaTime = UnityEngine.Time.deltaTime;
        float factor = SimulationManager.InterpolationFactor;

        // Apply lerp
        foreach(var entityIndex in positionEntity) {

            // Get references
            ref var entity = ref positionEntity.GetEntity(entityIndex);
            ref var interp = ref entity.Get<InterpolatedPositionEntity>();
            ref var pos = ref entity.Get<Position>();

            // Smoothing offsets
            float lerpBlend = 1f - math.pow(1f - interp.offsetLerpSmooth, deltaTime * interp.offsetLerpSpeed);
            interp.posOffset = math.lerp(interp.posOffset, float3.zero, lerpBlend);
            //interp.posOffset = UnityEngine.Vector3.MoveTowards(interp.posOffset, float3.zero, interp.offsetLerpSmooth);

            // Create interpolation
            interp.lerpPosition = math.lerp(
                interp.oldPosition,
                pos.value,
                factor) + interp.posOffset;

            if(float.IsInfinity(interp.lerpPosition.x) || float.IsInfinity(interp.lerpPosition.y) || float.IsInfinity(interp.lerpPosition.z)) {
                //UnityEngine.Debug.LogError($"Something went wrong: {factor}, {interp.oldPosition}, {pos.value}");
            }
        }

        // Apply slerp
        foreach(var entityIndex in rotationEntity) {

            // Get references
            ref var entity = ref rotationEntity.GetEntity(entityIndex);
            ref var interp = ref entity.Get<InterpolatedRotationEntity>();
            ref var rot = ref entity.Get<Rotation>();

            // Smoothing offsets
            float slerpBlend = 1f - math.pow(1f - interp.offsetSlerpSmooth, deltaTime * interp.offsetSlerpSpeed);
            interp.rotOffset = math.slerp(interp.rotOffset, quaternion.identity, slerpBlend);

            // Create interpolation
            interp.slerpRotation = math.mul(interp.rotOffset, math.slerp(
                interp.oldRotation,
                rot.value,
                factor));
        }
    }

    // Copies position to calculate offset later
    public void CopyPreOffset () {
        
        foreach(var entityIndex in positionEntity) {

            // Get references
            ref var entity = ref positionEntity.GetEntity(entityIndex);
            ref var interp = ref entity.Get<InterpolatedPositionEntity>();
            ref var pos = ref entity.Get<Position>();

            interp.doChargeCorrectiveIntepolation = true;
            interp.correctiveInterpolationFrom = pos.value;
        }

        foreach(var entityIndex in rotationEntity) {

            // Get references
            ref var entity = ref positionEntity.GetEntity(entityIndex);
            ref var interp = ref entity.Get<InterpolatedRotationEntity>();
            ref var rot = ref entity.Get<Rotation>();

            interp.doChargeCorrectiveIntepolation = true;
            interp.correctiveInterpolationFrom = rot.value;
        }
    }

    const float maxBeforeSnap = 64f;
    // This is meant to be executed after loading and resimulating server data. Then the old and new position can be compared, and an offset can be calculated 
    public void ApplyCalculatedOffset () {
        
        foreach(var entityIndex in positionEntity) {

            // Get references
            ref var entity = ref positionEntity.GetEntity(entityIndex);
            ref var interp = ref entity.Get<InterpolatedPositionEntity>();
            ref var pos = ref entity.Get<Position>();

            if(interp.doChargeCorrectiveIntepolation) {
                interp.doChargeCorrectiveIntepolation = false;

                if(math.lengthsq(interp.correctiveInterpolationFrom - pos.value) < maxBeforeSnap * maxBeforeSnap) {
                    interp.posOffset += interp.correctiveInterpolationFrom - pos.value;
                }
            }
        }

        foreach(var entityIndex in rotationEntity) {

            // Get references
            ref var entity = ref positionEntity.GetEntity(entityIndex);
            ref var interp = ref entity.Get<InterpolatedRotationEntity>();
            ref var rot = ref entity.Get<Rotation>();

            if(interp.doChargeCorrectiveIntepolation) {
                interp.doChargeCorrectiveIntepolation = false;

                interp.rotOffset = math.mul(interp.rotOffset, math.mul(math.inverse(rot.value), interp.correctiveInterpolationFrom));
            }
        }
    }
}
