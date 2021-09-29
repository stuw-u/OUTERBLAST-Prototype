using System.Runtime.CompilerServices;
using Eldemarkki.VoxelTerrain.Settings;
using Eldemarkki.VoxelTerrain.Utilities;
using Eldemarkki.VoxelTerrain.VoxelData;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;


[CreateAssetMenu(fileName = "BlockyGeneratorPass", menuName = "Custom/Terrain/Blocky Generator Pass", order = -1)]
#region Asset
public class BlockyGeneratorPass : TerrainGeneratorPassAsset {

    [Header("Parameters")]
    public BlockyGeneratorPass_Parameters parameters;

    public override JobHandleWithData<IVoxelDataGenerationJob> GetGeneratorPassHandle (VoxelDataVolume voxelData, Bounds bounds, int seed, float size) {
        BlockyGeneratorPass_CalculationJob job = new BlockyGeneratorPass_CalculationJob {
            param = parameters,
            worldPositionOffset = bounds.min.ToInt3(),
            outputVoxelData = voxelData,
            seed = seed
        };

        JobHandle jobHandle = job.Schedule(voxelData.Length, 256);
        JobHandleWithData<IVoxelDataGenerationJob> jobHandleWithData = new JobHandleWithData<IVoxelDataGenerationJob>();
        jobHandleWithData.JobHandle = jobHandle;
        jobHandleWithData.JobData = job;

        return jobHandleWithData;
    }
}
#endregion


[System.Serializable]
public struct BlockyGeneratorPass_Parameters {

}


[BurstCompile]
public struct BlockyGeneratorPass_CalculationJob : IVoxelDataGenerationJob {

    public BlockyGeneratorPass_Parameters param { get; set; }
    public VoxelDataVolume outputVoxelData { get; set; }
    public int3 worldPositionOffset { get; set; }
    public int seed { get; set; }


    public void Execute (int index) {
        int3 worldPosition = IndexUtilities.IndexToXyz(index, outputVoxelData.Width, outputVoxelData.Height) + worldPositionOffset;

        float voxelData = outputVoxelData.GetVoxelData(index, 0);
        outputVoxelData.SetVoxelData(math.step(voxelData, 0.5f), index, 0);
    }
}
