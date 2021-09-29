using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Blast.ECS;
using Blast.ECS.DefaultComponents;

[CreateAssetMenu(fileName = "EffectApplier", menuName = "Custom/Item/Effect Applier")]
public class EffectApplier : ItemLogicOneUse {

    // Parameters
    public byte effect;
    public byte level;
    public ushort timer;


    public override void OnActivated (ref Entity entity) {
        
        ref var player = ref entity.Get<PlayerComponent>();
        ref var effects = ref entity.Get<PlayerEffects>();
        effects.SetEffect((byte)player.clientId, effect, level, timer);
    }
}
