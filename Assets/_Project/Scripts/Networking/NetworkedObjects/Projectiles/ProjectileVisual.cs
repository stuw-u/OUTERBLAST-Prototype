using System.Collections;
using System.Collections.Generic;
using Blast.ECS;
using Blast.ECS.DefaultComponents;
using UnityEngine;

public class ProjectileVisual : PoolableVisualObject {

    new public Renderer renderer;
    public TrailRenderer trail;

    public override void OnSpawn (Entity entity) {
        //trail?.Clear();
        ref var projectile = ref entity.Get<Blast.NetworkedEntities.NetworkedEntityComponent>();
        if(LobbyWorldInterface.TryGetLocalPlayer(projectile.owner, out ILocalPlayer localPlayer)) {
            renderer.SetPropertyBlock(localPlayer.ColoredMaterialPropertyBlock);
        }
    }

    public override void OnVisualUpdate (Entity entity) {
        ref var projectile = ref entity.Get<ProjectileComponent>();
        ref var position = ref entity.Get<InterpolatedPositionEntity>();
        ref var rotation = ref entity.Get<InterpolatedRotationEntity>();
        transform.position = position.lerpPosition;
        transform.rotation = rotation.slerpRotation;

        if(projectile.timer < 2) {
            trail?.Clear();
        }
    }
}
