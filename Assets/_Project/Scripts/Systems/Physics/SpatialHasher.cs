using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Collections;
using Blast.ECS;
using Blast.ECS.DefaultComponents;

public struct HashedEntity {

}

public class SpatialHasherSystem : IECSRunSystem, IECSInitSystem, IECSDestroySystem {

    private static SpatialHasherSystem inst;
    private readonly EcsFilter<Position> positionEntity = null;
    private NativeMultiHashMap<int, int> hashMap;
    private bool isHashMapAllocated;
    public const int boxSize = 4;
    public const ulong boxCount = 4096;
    public const int halfBoxCount = 2048;

    public void Init () {
        inst = this;
    }

    public void Run () {
        hashMap = new NativeMultiHashMap<int, int>(positionEntity.GetEntitiesCount(), Allocator.Persistent);
        isHashMapAllocated = true;

        float deltaTime = UnityEngine.Time.fixedDeltaTime;
        foreach(var entityIndex in positionEntity) {

            ref var entity = ref positionEntity.GetEntity(entityIndex);
            ref var position = ref entity.Get<Position>();
            int hash = BoxToHash(PositionToBox(position.value));

            hashMap.Add(hash, entity.GetInternalId());
        }
    }

    public void Clear () {
        hashMap.Dispose();
        isHashMapAllocated = false;
    }

    public void Dispose () {
        if(isHashMapAllocated)
            hashMap.Dispose();
    }

    public void GetNearEntity (float3 position, EntityGetter getter) {
        if(hashMap.Count() == 0)
            return;

        int3 box = PositionToBox(position);
        for(int x = box.x - 1; x < box.x + 2; x++) {
            for(int y = box.y - 1; y < box.y + 2; y++) {
                for(int z = box.z - 1; z < box.z + 2; z++) {
                    int hash = BoxToHash(new int3(x, y, z));
                    if(hashMap.TryGetFirstValue(hash, out int foundValue, out var iterator)) {
                        do {
                            Entity entity = SimulationManager.inst.simulationWorld.RestoreEntityFromInternalId(foundValue);
                            getter(entity);
                        } while(hashMap.TryGetNextValue(out foundValue, ref iterator));
                    }
                }
            }
        }
    }

    public static int3 PositionToBox (float3 position) {
        return (int3)math.floor(position / boxSize);
    }

    public static int BoxToHash (int3 box) {
        return (int)math.hash(box);
    }

    public delegate void EntityGetter (Entity entity);
}