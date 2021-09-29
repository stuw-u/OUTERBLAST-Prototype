using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using MLAPI.Serialization;
using Blast.ECS;
using Blast.ECS.DefaultComponents;

[CreateAssetMenu(fileName = "Jet", menuName = "Custom/Item/Jet")]
public class Jet : ItemLogicLimitedHoldRecovery {

    // Parameters
    public float accelerationY = 1f;
    public float maxSpeedY = 1f;
    public float dragXZ = 1f;
    
    public override void OnActivated (ref Entity entity) {

        ref var _localToWorld = ref entity.Get<LocalToWorld>();
        ref var rot = ref entity.Get<Rotation>();
        ref var velocity = ref entity.Get<Velocity>();
        ref var input = ref entity.Get<InputControlledComponent>();

        float3 invVel = math.transform(math.inverse(_localToWorld.rotationValue), velocity.value);
        invVel = new float3(invVel.x * dragXZ, invVel.y, invVel.z * dragXZ);
        if(invVel.y < maxSpeedY) {
            invVel.y = math.min(invVel.y + accelerationY * Time.fixedDeltaTime, maxSpeedY);
        }
        velocity.value = math.transform(_localToWorld.rotationValue, invVel);

    }
}
