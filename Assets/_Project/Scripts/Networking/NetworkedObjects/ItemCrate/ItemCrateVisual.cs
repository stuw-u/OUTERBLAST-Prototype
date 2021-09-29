using System.Collections;
using System.Collections.Generic;
using Blast.ECS;
using Blast.ECS.DefaultComponents;
using UnityEngine;

public class ItemCrateVisual : PoolableVisualObject {

    public SpriteRenderer itemIcon;

    public override void OnSpawn (Entity entity) {
        ref var itemCrate = ref entity.Get<ItemCrateComponent>();
        ref var position = ref entity.Get<Position>();
        ref var orientation = ref entity.Get<Oritentation>();

        itemIcon.sprite = AssetsManager.inst.itemAssets.items[itemCrate.itemId].icon;
        transform.position = position.value;
        transform.rotation = Quaternion.AngleAxis(Random.value * 360f, orientation.up);
    }

    public override void OnVisualUpdate (Entity entity) {
    }
}
