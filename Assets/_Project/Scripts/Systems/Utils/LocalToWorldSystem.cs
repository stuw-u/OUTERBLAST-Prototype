using Unity.Mathematics;
using Blast.ECS;
using Blast.ECS.DefaultComponents;

public class LocalToWorldSystem : IECSRunSystem {

    private readonly EcsFilter<LocalToWorld, Position, Rotation> localToWorldEntities = null;


    public void Run () {

        foreach(var entityIndex in localToWorldEntities) {

            // Get actual entity reference
            ref var entity = ref localToWorldEntities.GetEntity(entityIndex);

            // Ref components
            ref var localToWorld = ref entity.Get<LocalToWorld>();
            ref var translation = ref entity.Get<Position>();
            ref var rotation = ref entity.Get<Rotation>();

            localToWorld.rotationValue = float4x4.TRS(float3.zero, rotation.value, math.float3(1f, 1f, 1f));
            localToWorld.value = float4x4.TRS(translation.value, rotation.value, math.float3(1f, 1f, 1f));
        }
    }
}
