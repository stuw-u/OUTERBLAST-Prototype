using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Blast.ECS;
using Blast.ECS.DefaultComponents;

[CreateAssetMenu(fileName = "Pickaxe", menuName = "Custom/Item/Pickaxe")]
public class PickaxeItem : ItemLogicOneFrameAction {

    // Parameters
    public float reachDistance;


    public override void OnActivated (ref Entity entity) {

        ref var pos = ref entity.Get<Position>();
        ref var input = ref entity.Get<InputControlledComponent>();
        ref var player = ref entity.Get<PlayerComponent>();
        ref var _localToWorld = ref entity.Get<LocalToWorld>();


        PlayerUtils.GetPlayerRay(ref entity, out float3 rayDirection, out float3 rayOrigin);

        if(Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hitInfo, reachDistance, 1 << 9)) {
            SimulationManager.inst.SummonTerrainEffect(1, hitInfo.point);
        } else {
            float3 castPos = (rayOrigin) + (rayDirection * reachDistance * 0.5f);
            if(Physics.CheckSphere(castPos, reachDistance * 0.2f, 1 << 9)) {
                SimulationManager.inst.SummonTerrainEffect(1, castPos);
            }
        }
    }
}
