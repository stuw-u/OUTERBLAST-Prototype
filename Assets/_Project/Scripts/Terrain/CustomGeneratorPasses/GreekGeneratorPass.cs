using System.Runtime.CompilerServices;
using Eldemarkki.VoxelTerrain.Settings;
using Eldemarkki.VoxelTerrain.Utilities;
using Eldemarkki.VoxelTerrain.VoxelData;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;

[CreateAssetMenu(fileName = "GreekGeneratorPass", menuName = "Custom/Terrain/Greek Generator Pass", order = -1)]
#region Asset
public class GreekGeneratorPass : TerrainGeneratorPassAsset {

    [Header("Parameters")]
    public GreekGeneratorPass_Parameters parameters;

    public override JobHandleWithData<IVoxelDataGenerationJob> GetGeneratorPassHandle (VoxelDataVolume voxelData, Bounds bounds, int seed, float size) {
        GreekGeneratorPass_CalculationJob job = new GreekGeneratorPass_CalculationJob {
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
public struct GreekGeneratorPass_Parameters {

}


[BurstCompile]
public struct GreekGeneratorPass_CalculationJob : IVoxelDataGenerationJob {

    public GreekGeneratorPass_Parameters param { get; set; }
    public VoxelDataVolume outputVoxelData { get; set; }
    public int3 worldPositionOffset { get; set; }
    public int seed { get; set; }


    public void Execute (int index) {
        int3 worldPosition = IndexUtilities.IndexToXyz(index, outputVoxelData.Width, outputVoxelData.Height) + worldPositionOffset;

        float voxelData = outputVoxelData.GetVoxelData(index, 0);
        voxelData = math.max(voxelData, CalculateVoxelData(worldPosition));
        outputVoxelData.SetVoxelData(voxelData, index, 0);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float CalculateVoxelData (float3 worldPosition) {
        float total = 0f;

        total = math.max(total, GeneratorPassUtils.builder_CylinderY(worldPosition, new float3(-14f, 13f, -18f), 15f, 5f, 5f));
        total = math.max(total, GeneratorPassUtils.builder_CylinderY(worldPosition, new float3(-5f, 13f, -18f), 15f, 5f, 5f));
        total = math.max(total, GeneratorPassUtils.builder_CylinderY(worldPosition, new float3(5f, 13f, -18f), 15f, 5f, 5f));
        total = math.max(total, GeneratorPassUtils.builder_CylinderY(worldPosition, new float3(14f, 13f, -18f), 15f, 5f, 5f));

        total = math.max(total, GeneratorPassUtils.builder_CylinderY(worldPosition, new float3(-14f, 13f, 18f), 15f, 5f, 5f));
        total = math.max(total, GeneratorPassUtils.builder_CylinderY(worldPosition, new float3(-5f, 13f, 18f), 15f, 5f, 5f));
        total = math.max(total, GeneratorPassUtils.builder_CylinderY(worldPosition, new float3(5f, 13f, 18f), 15f, 5f, 5f));
        total = math.max(total, GeneratorPassUtils.builder_CylinderY(worldPosition, new float3(14f, 13f, 18f), 15f, 5f, 5f));

        total = math.max(total, GeneratorPassUtils.builder_CylinderY(worldPosition, new float3(-14f, 13f, 18f), 15f, 5f, 5f));
        total = math.max(total, GeneratorPassUtils.builder_CylinderY(worldPosition, new float3(-14f, 13f, 9f), 15f, 5f, 5f));
        total = math.max(total, GeneratorPassUtils.builder_CylinderY(worldPosition, new float3(-14f, 13f, 0f), 15f, 5f, 5f));
        total = math.max(total, GeneratorPassUtils.builder_CylinderY(worldPosition, new float3(-14f, 13f, -9f), 15f, 5f, 5f));
        total = math.max(total, GeneratorPassUtils.builder_CylinderY(worldPosition, new float3(-14f, 13f, -18f), 15f, 5f, 5f));

        total = math.max(total, GeneratorPassUtils.builder_CylinderY(worldPosition, new float3(14f, 13f, 18f), 15f, 5f, 5f));
        total = math.max(total, GeneratorPassUtils.builder_CylinderY(worldPosition, new float3(14f, 13f, 9f), 15f, 5f, 5f));
        total = math.max(total, GeneratorPassUtils.builder_CylinderY(worldPosition, new float3(14f, 13f, 0f), 15f, 5f, 5f));
        total = math.max(total, GeneratorPassUtils.builder_CylinderY(worldPosition, new float3(14f, 13f, -9f), 15f, 5f, 5f));
        total = math.max(total, GeneratorPassUtils.builder_CylinderY(worldPosition, new float3(14f, 13f, -18f), 15f, 5f, 5f));

        total = math.max(total, GeneratorPassUtils.builder_Box(worldPosition, new float3(0f, 8f, 0f), new float3(40f, 3f, 48f)));
        total = math.max(total, GeneratorPassUtils.builder_Box(worldPosition, new float3(0f, 8f, 0f), new float3(34f, 5f, 42f)));
        total = math.max(total, GeneratorPassUtils.builder_Box(worldPosition, new float3(0f, 28f, 0f), new float3(34f, 3f, 42f)));

        total = math.max(total, GeneratorPassUtils.builder_TrianglePrismX(worldPosition, new float3(0f, 31f, 0f), new float3(38f, 13f, 44f)));
        total -= GeneratorPassUtils.builder_TrianglePrismX(worldPosition, new float3(0f, 33f, 23f), new float3(28f, 9f, 2f));
        total -= GeneratorPassUtils.builder_TrianglePrismX(worldPosition, new float3(0f, 33f, -23f), new float3(28f, 9f, 2f));

        return total;
    }
}
