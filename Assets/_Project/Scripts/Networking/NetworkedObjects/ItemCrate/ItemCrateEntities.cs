using System.Collections.Generic;
using Unity.Jobs;
using MLAPI.Serialization;
using Unity.Mathematics;
using Unity.Collections;
using Blast.ObjectPooling;
using Blast.ECS;
using Blast.ECS.DefaultComponents;
using Blast.NetworkedEntities;

struct DelayedItemCrateSpawn {
    public int spawnFrame;
    public byte type;
    public byte itemId;
    public float3 position;
}

struct ItemCrateComponent {
    public byte itemId;
    public ushort timer;
}

struct Oritentation {
    public float3 up;
}

public class ItemCrateEntities : NetworkedStaticEntitySystemBase {

    private const float maxTime = 18000;
    public override int GetVisualTypeCount => 1;
    private byte copy;
    private Queue<DelayedItemCrateSpawn> delayedItemCrateSpawns;

    public void Spawn (int spawnFrame, byte type, byte itemId, float3 position) {
        if(!NetAssist.IsServer)
            return;

        delayedItemCrateSpawns.Enqueue(new DelayedItemCrateSpawn() {
            spawnFrame = spawnFrame,
            type = type,
            itemId = itemId,
            position = position
        });
    }

    protected override void OnServerInitSystem () {
        delayedItemCrateSpawns = new Queue<DelayedItemCrateSpawn>();
    }

    protected override void OnServerUpdateSystem () {
        while(delayedItemCrateSpawns.Count > 0) {
            var spawnInfo = delayedItemCrateSpawns.Dequeue();
            Internal_Spawn(spawnInfo.spawnFrame, spawnInfo.type, spawnInfo.itemId, spawnInfo.position);
        }
    }

    private void Internal_Spawn (int spawnFrame, byte type, byte itemId, float3 position) {
        if(!NetAssist.IsServer)
            return;
        
        Entity entity = GetEntity(spawnFrame, 0, copy, out bool didEntityExist);
        copy++;

        entity.Get<ItemCrateComponent>() = new ItemCrateComponent() {
            itemId = itemId,
            timer = 0
        };

        float3 downVector = GravitySystem.GetGravityDirection(position);
        if(UnityEngine.Physics.Raycast(position, downVector, out UnityEngine.RaycastHit hitInfo, 5f, 1 << 9)) {
            entity.Get<Position>().value = hitInfo.point + hitInfo.normal * 0.5f;
            entity.Get<Oritentation>().up = hitInfo.normal;
        } else {
            entity.Get<Position>().value = position;
            entity.Get<Oritentation>().up = -downVector;
        }
    }

    protected override void SetupComponents (Entity entity) {
        entity.Get<ItemCrateComponent>();
        entity.Get<Position>();
        entity.Get<Oritentation>();
    }

    protected override void OnServerUpdateEntity (Entity entity, float deltaTime) {
        ref var position = ref entity.Get<Position>();
        ref var itemCrate = ref entity.Get<ItemCrateComponent>();
        ref var networkedEntity = ref entity.Get<NetworkedEntityComponent>();
        float3 pos = position.value;

        itemCrate.timer++;
        if(itemCrate.timer > maxTime) {
            networkedEntity.disabledTimer = 1;
        }

        bool doCollectSelf = false;
        ulong collector = 0;
        if(itemCrate.timer > 10) {
            SimulationManager.inst.spatialHasherSystem.GetNearEntity(position.value, (e) => {
                if(!e.Has<PlayerComponent>())
                    return;
                ref var player = ref e.Get<PlayerComponent>();
                ref var playerPos = ref e.Get<Position>();
                if(e.Has<Ghost>())
                    return;

                if(math.lengthsq(playerPos.value - pos) < 2f * 2f) {
                    AudioManager.PlayClientEnvironmentSoundAt(-1, pos, EnvironmentSound.Stacking);
                    collector = player.clientId;
                    doCollectSelf = true;
                }
            });

            if(doCollectSelf) {
                networkedEntity.disabledTimer = 1;
            }
        }
    }
    

    protected override PoolableObject GetVisualPrefab (int typeId) {
        return AssetsManager.inst.itemCrate;
    }

    protected override int GetVisualTypeFromEntity (Entity entity) {
        return 0;
    }

    protected override void OnSerializeNewEntityNetwork (Entity entity, NetworkWriter writer) {
        ref var position = ref entity.Get<Position>();
        ref var orientation = ref entity.Get<Oritentation>();
        ref var itemCrate = ref entity.Get<ItemCrateComponent>();

        writer.WriteByte(itemCrate.itemId);
        writer.WriteVector3(position.value);
        writer.WriteVector3(orientation.up);
    }

    protected override void OnDeserializeNewEntityNetwork (Entity entity, NetworkReader reader, bool didEntityExist) {
        ref var position = ref entity.Get<Position>();
        ref var orientation = ref entity.Get<Oritentation>();
        ref var itemCrate = ref entity.Get<ItemCrateComponent>();

        itemCrate.itemId = (byte)reader.ReadByte();
        position.value = reader.ReadVector3();
        orientation.up = reader.ReadVector3();
    }
}
