using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using MLAPI.Serialization;
using Blast.ECS;
using Blast.ECS.DefaultComponents;

[CreateAssetMenu(fileName = "Grappler", menuName = "Custom/Item/Grappler")]
public class Grappler : ItemLogicOneFrameAction {

    // Parameters
    public float maxDistance;
    public float impulseXZ;
    public float impulseY;
    public float arcExtension;
    public float initialDownVelocity = -10f;
    public float downVel = 40f;


    public override bool OnActivatedRecovery (ref Entity entity) {
        ref var player = ref entity.Get<PlayerComponent>();
        ref var pos = ref entity.Get<Position>();
        ref var input = ref entity.Get<InputControlledComponent>();
        ref var velocity = ref entity.Get<Velocity>();
        ref var _localToWorld = ref entity.Get<LocalToWorld>();

        float4x4 worldToLocal = math.inverse(_localToWorld.rotationValue);
        float3 invVel = math.transform(worldToLocal, velocity.value);

        float3 camPos = pos.value + _localToWorld.Up * 0.5f;
        float3 hitDir = math.mul(
            math.mul(new quaternion(_localToWorld.rotationValue), quaternion.Euler(
                math.radians(input.inputSnapshot.lookAxis.y),
                math.radians(input.inputSnapshot.lookAxis.x),
                0f
            )), math.forward());

        if(Physics.Raycast(camPos, hitDir, out RaycastHit hitInfo, maxDistance, 1 << 9)) {
            if(!SimulationManager.inst.IsReplayingFrame) {
                AudioManager.PlayClientEnvironmentSoundAt((int)player.clientId, hitInfo.point, EnvironmentSound.Launch);
                AudioManager.PlayClientEnvironmentSoundAt((int)player.clientId, hitInfo.point, EnvironmentSound.Dig);
                if(LobbyWorldInterface.TryGetLocalPlayer(player.clientId, out ILocalPlayer localPlayer)) {
                    localPlayer.SetAnimationTriggerPosition((byte)id, hitInfo.point);
                }
            }

            float3 diff = math.transform(worldToLocal, (float3)hitInfo.point - pos.value);
            float gravity = math.length(GravitySystem.GetGravityVector(pos.value));
            float maximumHeightOfArc = diff.y + arcExtension;

            if(maximumHeightOfArc < 0) {
                float3 normalDiff = math.normalizesafe(diff) * downVel;
                velocity.value = math.transform(_localToWorld.rotationValue, normalDiff);
            } else {
                float velocityY = math.sqrt(2 * gravity * maximumHeightOfArc);
                float time = math.max(0.5f, (velocityY - invVel.y) / gravity);
                float2 velocityXZ = diff.xz / time;
                velocity.value = math.transform(_localToWorld.rotationValue, new float3(velocityXZ.x * impulseXZ, velocityY * impulseY, velocityXZ.y * impulseXZ));
            }
            return true;
        }
        return false;
    }

    public override bool IsUsageConditionMet (ref Entity entity, BaseItem_Data data) {
        ref var player = ref entity.Get<PlayerComponent>();
        ref var pos = ref entity.Get<Position>();
        ref var input = ref entity.Get<InputControlledComponent>();
        ref var velocity = ref entity.Get<Velocity>();
        ref var _localToWorld = ref entity.Get<LocalToWorld>();

        float4x4 worldToLocal = math.inverse(_localToWorld.rotationValue);
        float3 invVel = math.transform(worldToLocal, velocity.value);

        float3 camPos = pos.value + _localToWorld.Up * 0.5f;
        float3 hitDir = math.mul(
            math.mul(new quaternion(_localToWorld.rotationValue), quaternion.Euler(
                math.radians(input.inputSnapshot.lookAxis.y),
                math.radians(input.inputSnapshot.lookAxis.x),
                0f
            )), math.forward());

        return Physics.Raycast(camPos, hitDir, out RaycastHit hitInfo, maxDistance, 1 << 9);
    }
}
