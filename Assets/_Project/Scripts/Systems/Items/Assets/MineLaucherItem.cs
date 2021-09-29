using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using MLAPI.Serialization;
using Blast.ECS;
using Blast.ECS.DefaultComponents;

[CreateAssetMenu(fileName = "MineLaucher", menuName = "Custom/Item/Mine Launcher")]
public class MineLauncherItem : ItemLogicOneUse {
    
    // Parameters
    public override void OnActivated (ref Entity entity) {

        ref var pos = ref entity.Get<Position>();
        ref var input = ref entity.Get<InputControlledComponent>();
        ref var player = ref entity.Get<PlayerComponent>();
        ref var _localToWorld = ref entity.Get<LocalToWorld>();

        PlayerUtils.GetPlayerRay(ref entity, out float3 rayDirection, out float3 rayOrigin, new float2(0f, 0f));
        
        SimulationManager.inst.mineEntities.Spawn(
            input.inputFrame, (byte)player.clientId, 0,
            rayOrigin + rayDirection * 2,
            rayDirection
        );

        if(!SimulationManager.inst.IsReplayingFrame) {
            AudioManager.PlayClientEnvironmentSoundAt((int)player.clientId, rayOrigin, EnvironmentSound.Launch);
        }
    }
}