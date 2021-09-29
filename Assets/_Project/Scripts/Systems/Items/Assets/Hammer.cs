using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using MLAPI.Serialization;
using Blast.ECS;
using Blast.ECS.DefaultComponents;

[CreateAssetMenu(fileName = "Hammer", menuName = "Custom/Item/Hammer")]
public class Hammer : ItemLogicChargeableAction {

    // Parameters
    public float hitSphereRange = 2f;
    public float hitSphereRadius = 1f;
    public float hitAngleRange = 45f;
    public float minVectorForce = 10f;
    public float maxVectorForce = 20f;
    

    public override void OnActivated (ref Entity entity, float chargedValue) {
        ref var pos = ref entity.Get<Position>();
        ref var input = ref entity.Get<InputControlledComponent>();
        ref var player = ref entity.Get<PlayerComponent>();
        ref var _localToWorld = ref entity.Get<LocalToWorld>();

        PlayerUtils.GetPlayerRay(ref entity, out float3 rayDirection, out float3 rayOrigin);
        float3 hitPos = rayOrigin + rayDirection * hitSphereRange;

        if(!SimulationManager.inst.IsReplayingFrame) {
            // Set off the animation trigger here!!
            AudioManager.PlayClientEnvironmentSoundAt((int)player.clientId, hitPos, EnvironmentSound.Hitting);
            if(LobbyWorldInterface.TryGetLocalPlayer(player.clientId, out ILocalPlayer localPlayer)) {
                localPlayer.SetAnimationTrigger((byte)id);
            }
        }
        if(SimulationManager.inst.explosionSystem.Hit(
            player.clientId,
            hitPos,
            hitSphereRadius,
            rayDirection * math.lerp(minVectorForce, maxVectorForce, chargedValue),
            chargedValue)) {

            // Display hitting effects + sound
            if(!SimulationManager.inst.IsReplayingFrame) {
                ExplosionManager.SpawnHitAt(hitPos, rayDirection);
                AudioManager.PlayClientEnvironmentSoundAt((int)player.clientId, hitPos, EnvironmentSound.PlayerTick);
            }
        }
    }
}
 