using Unity.Collections;
using MLAPI.Serialization;
using MLAPI.Serialization.Pooled;
using Blast.ECS;
using Blast.NetworkedEntities;

public class FrameData {

    public int index;
    public NativeHashMap<byte, PlayerStateData> playersData;
    //public NativeArray<ProjectileStateData> projectilesData;
    public InputSnapshot clientInputs;
    public PooledFrameStream itemEntityData;

    public FrameData (int index) {
        this.index = index;
    }

    public void Record (SimulationManager simulation, InputSnapshot clientInputs) {
        itemEntityData = PooledFrameStream.Get();
        playersData = PlayerSerializer.SerializeFrame(simulation.playerSystem);
        //projectilesData = simulation.projectileSystem.SerializeFrame();
        using(PooledNetworkWriter writer = PooledNetworkWriter.Get(itemEntityData)) {
            itemEntityData.Position = 0;
            itemEntityData.BitPosition = 0;
            PlayerSerializer.SerializeItem(simulation.playerSystem, writer);
            NetworkedEntitySystems.SerializeAllEntitiesFrame(writer);
        }
        this.clientInputs = clientInputs;
    }
    
    public void Dispose () {
        playersData.Dispose();
        //projectilesData.Dispose();
        itemEntityData.Dispose();
    }
}
