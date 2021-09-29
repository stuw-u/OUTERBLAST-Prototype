using System.Collections;
using System.Collections.Generic;
using Blast.ECS;
using Blast.NetworkedEntities;
using UnityEngine;

public class MineVisual : PoolableVisualObject {

    new public Renderer renderer;
    public Renderer renderer2;

    public override void OnSpawn (Entity entity) {
        ref var netEntity = ref entity.Get<NetworkedEntityComponent>();
        if(LobbyWorldInterface.TryGetLocalPlayer(netEntity.owner, out ILocalPlayer localPlayer)) {
            renderer.SetPropertyBlock(localPlayer.ColoredMaterialPropertyBlock);
            renderer2.SetPropertyBlock(localPlayer.ColoredMaterialPropertyBlock);
        }

        renderer.enabled = true;
        renderer2.enabled = true;
    }

    public override void OnVisualUpdate (Entity entity) {
        ref var mine = ref entity.Get<MineComponent>();
        /*if(mine.isAnchored) {
            renderer.enabled = false;
            renderer2.enabled = false;
            return;
        }*/

        ref var position = ref entity.Get<InterpolatedPositionEntity>();
        ref var rotation = ref entity.Get<InterpolatedRotationEntity>();
        transform.position = position.lerpPosition;
        transform.rotation = rotation.slerpRotation;
    }
}
