using System.Runtime.CompilerServices;
using Eldemarkki.VoxelTerrain.Settings;
using Eldemarkki.VoxelTerrain.Utilities;
using Eldemarkki.VoxelTerrain.VoxelData;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;


[CreateAssetMenu(fileName = "MetaCavesGeneratorPass", menuName = "Custom/Terrain/Meta Caves Generator Pass", order = -1)]
#region Asset
public class MetaCavesGeneratorPass : TerrainGeneratorPassAsset {

    [Header("Parameters")]
    public MetaCavesGeneratorPass_Parameters parameters;

    public override JobHandleWithData<IVoxelDataGenerationJob> GetGeneratorPassHandle (VoxelDataVolume voxelData, Bounds bounds, int seed, float size) {
        MetaCavesGeneratorPass_CalculationJob job = new MetaCavesGeneratorPass_CalculationJob {
            param = parameters,
            worldPositionOffset = bounds.min.ToInt3(),
            outputVoxelData = voxelData,
            seed = seed,
            size = size
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
public struct MetaCavesGeneratorPass_Parameters {
    public float noiseFrequency;                //0.034
    public int noiseOctaveCount;                //4
}


[BurstCompile]
public struct MetaCavesGeneratorPass_CalculationJob : IVoxelDataGenerationJob {

    public MetaCavesGeneratorPass_Parameters param { get; set; }
    public VoxelDataVolume outputVoxelData { get; set; }
    public int3 worldPositionOffset { get; set; }
    public int seed { get; set; }
    public float size { get; set; }


    public void Execute (int index) {
        int3 worldPosition = IndexUtilities.IndexToXyz(index, outputVoxelData.Width, outputVoxelData.Height) + worldPositionOffset;

        float voxelDataValue = CalculateVoxelData(worldPosition);
        outputVoxelData.SetVoxelData(voxelDataValue, index, 0);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float CalculateVoxelData (float3 worldPosition) {
        float3 p = worldPosition;

        // Preparing and sampeling height-relative value
        float warp1 = GeneratorPassUtils.OctaveNoise(p.x, p.y * 1.25f, p.z, param.noiseFrequency * 1f, param.noiseOctaveCount, seed);
        float warp2 = GeneratorPassUtils.OctaveNoise(p.x, p.y * 1.25f, p.z, param.noiseFrequency * 1f, param.noiseOctaveCount, seed + 1);
        float warp3 = GeneratorPassUtils.OctaveNoise(p.x, p.y * 1.25f, p.z, param.noiseFrequency * 1f, param.noiseOctaveCount, seed + 2);
        float vor3f = -GeneratorPassUtils.SingleCellular3Edge(p + new float3(warp1, warp2, warp3) * 50f, 0.025f, seed) + 0.3f;

        return vor3f;
    }
}
