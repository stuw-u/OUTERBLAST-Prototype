using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using MLAPI.Serialization;
using Blast.ECS;
using Blast.ECS.DefaultComponents;

[CreateAssetMenu(fileName = "Delta", menuName = "Custom/Item/Delta")]
public class Delta : ItemLogicLimitedHoldRecovery {

    // Parameters
    public float accelerationXZ = 1f;
    public float maxSpeedXZ = 1f;
    public float dragY = 1f;
    public float minVelY = 0f;

    public override void OnActivated (ref Entity entity) {
        
        ref var _localToWorld = ref entity.Get<LocalToWorld>();
        ref var rot = ref entity.Get<Rotation>();
        ref var velocity = ref entity.Get<Velocity>();
        ref var input = ref entity.Get<InputControlledComponent>();
        
        quaternion lookRelativeRot = math.mul(rot.value, quaternion.Euler(0f, math.radians(input.inputSnapshot.lookAxis.x), 0f));
        float3 rawDirection = math.mul(lookRelativeRot, math.forward());
        velocity.value = mathUtils.accelerateVelocity(
            velocity.value,
            rawDirection,
            accelerationXZ,
            maxSpeedXZ);

        float3 invVel = math.transform(math.inverse(_localToWorld.rotationValue), velocity.value);
        invVel = new float3(invVel.x, math.max(invVel.y * dragY, minVelY), invVel.z);
        velocity.value = math.transform(_localToWorld.rotationValue, invVel);

    }
}
