using System.Runtime.CompilerServices;
using Eldemarkki.VoxelTerrain.Settings;
using Eldemarkki.VoxelTerrain.Utilities;
using Eldemarkki.VoxelTerrain.VoxelData;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;


[CreateAssetMenu(fileName = "CastleGeneratorPass", menuName = "Custom/Terrain/Castle Generator Pass", order = -1)]
#region Asset
public class CastleGeneratorPass : TerrainGeneratorPassAsset {
    
    [Header("Parameters")]
    public CastleGeneratorPass_Parameters parameters;

    public override JobHandleWithData<IVoxelDataGenerationJob> GetGeneratorPassHandle (VoxelDataVolume voxelData, Bounds bounds, int seed, float size) {
        CastleGeneratorPass_CalculationJob job = new CastleGeneratorPass_CalculationJob {
            param = parameters,
            worldPositionOffset = bounds.min.ToInt3(),
            outputVoxelData = voxelData,
            seed = seed,
            invertedFracturationSize = 1f / parameters.fracturationSize
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
public struct CastleGeneratorPass_Parameters {
    public bool doFracturate;
    public float fracturationFreqency;  //0.05
    public float fracturationOffset;    //10
    public float fracturationSize;      //0.3f
}


[BurstCompile]
public struct CastleGeneratorPass_CalculationJob : IVoxelDataGenerationJob {

    public CastleGeneratorPass_Parameters param { get; set; }
    public VoxelDataVolume outputVoxelData { get; set; }
    public int3 worldPositionOffset { get; set; }
    public int seed { get; set; }
    public float invertedFracturationSize { get; set; }


    public void Execute (int index) {
        int3 worldPosition = IndexUtilities.IndexToXyz(index, outputVoxelData.Width, outputVoxelData.Height) + worldPositionOffset;

        float voxelData = outputVoxelData.GetVoxelData(index, 0);
        voxelData = math.max(voxelData, CalculateVoxelData(worldPosition));
        outputVoxelData.SetVoxelData(voxelData, index, 0);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float CalculateVoxelData (float3 worldPosition) {
        float2 noiseOffset = (GeneratorPassUtils.hash2_2(seed) - 0.5f) * 10000f;
        float2 fracturation = GeneratorPassUtils.voronoiDistance(worldPosition.xz * param.fracturationFreqency + noiseOffset);
        float fracture_value = math.lerp(1f - param.fracturationSize, 1f, math.min(param.fracturationSize, fracturation.x) * invertedFracturationSize);
        float offsetedY = worldPosition.y - fracturation.y * param.fracturationOffset;

        worldPosition.y = math.select(worldPosition.y, offsetedY, param.doFracturate);

        float total = 0f;

        total = math.max(total, GeneratorPassUtils.builder_CylinderY(worldPosition, new float3(-20f, 10f, -20f), 17f, 13f, 10f));
        total = math.max(total, GeneratorPassUtils.builder_CylinderY(worldPosition, new float3( 20f, 10f, -20f), 17f, 13f, 10f));
        total = math.max(total, GeneratorPassUtils.builder_CylinderY(worldPosition, new float3(-20f, 10f,  20f), 17f, 13f, 10f));
        total = math.max(total, GeneratorPassUtils.builder_CylinderY(worldPosition, new float3( 20f, 10f,  20f), 17f, 13f, 10f));

        total = math.max(total, GeneratorPassUtils.builder_CylinderY(worldPosition, new float3(-20f, 27f, -20f), 4f, 12f, 12f));
        total = math.max(total, GeneratorPassUtils.builder_CylinderY(worldPosition, new float3( 20f, 27f, -20f), 4f, 12f, 12f));
        total = math.max(total, GeneratorPassUtils.builder_CylinderY(worldPosition, new float3(-20f, 27f,  20f), 4f, 12f, 12f));
        total = math.max(total, GeneratorPassUtils.builder_CylinderY(worldPosition, new float3( 20f, 27f,  20f), 4f, 12f, 12f));

        total -= GeneratorPassUtils.builder_CylinderY(worldPosition, new float3( 20f, 28f,  20f), 4f, 4f, 4f);
        total -= GeneratorPassUtils.builder_CylinderY(worldPosition, new float3(-20f, 28f,  20f), 4f, 4f, 4f);
        total -= GeneratorPassUtils.builder_CylinderY(worldPosition, new float3( 20f, 28f, -20f), 4f, 4f, 4f);
        total -= GeneratorPassUtils.builder_CylinderY(worldPosition, new float3(-20f, 28f, -20f), 4f, 4f, 4f);

        total = math.max(total, GeneratorPassUtils.builder_CylinderY(worldPosition, new float3(-20f,  5f, -20f),  5f, 0f, 14f));
        total = math.max(total, GeneratorPassUtils.builder_CylinderY(worldPosition, new float3( 20f,  5f, -20f),  5f, 0f, 14f));
        total = math.max(total, GeneratorPassUtils.builder_CylinderY(worldPosition, new float3(-20f,  5f,  20f),  5f, 0f, 14f));
        total = math.max(total, GeneratorPassUtils.builder_CylinderY(worldPosition, new float3( 20f,  5f,  20f),  5f, 0f, 14f));

        total -= GeneratorPassUtils.builder_Box(worldPosition, new float3( 20f, 30f, -20f), new float3(3f, 5f, 12f));
        total -= GeneratorPassUtils.builder_Box(worldPosition, new float3( 20f, 30f, -20f), new float3(12f, 5f, 3f));
        total -= GeneratorPassUtils.builder_Box(worldPosition, new float3(-20f, 30f, -20f), new float3(3f, 5f, 12f));
        total -= GeneratorPassUtils.builder_Box(worldPosition, new float3(-20f, 30f, -20f), new float3(12f, 5f, 3f));
        total -= GeneratorPassUtils.builder_Box(worldPosition, new float3( 20f, 30f,  20f), new float3(3f, 5f, 12f));
        total -= GeneratorPassUtils.builder_Box(worldPosition, new float3( 20f, 30f,  20f), new float3(12f, 5f, 3f));
        total -= GeneratorPassUtils.builder_Box(worldPosition, new float3(-20f, 30f,  20f), new float3(3f, 5f, 12f));
        total -= GeneratorPassUtils.builder_Box(worldPosition, new float3(-20f, 30f,  20f), new float3(12f, 5f, 3f));

        total = math.max(total, GeneratorPassUtils.builder_Box(worldPosition, new float3(0f, 8f, 20f), new float3(40f, 10f, 5f)));
        total = math.max(total, GeneratorPassUtils.builder_Box(worldPosition, new float3(0f, 8f, -20f), new float3(40f, 10f, 5f)));
        total = math.max(total, GeneratorPassUtils.builder_Box(worldPosition, new float3(20f, 8f, 0f), new float3(5f, 10f, 40f)));
        total = math.max(total, GeneratorPassUtils.builder_Box(worldPosition, new float3(-20f, 8f, 0f), new float3(5f, 10f, 40f)));

        total = math.max(total, GeneratorPassUtils.builder_BoxDentedX(worldPosition, new float3(0f, 10f, 22f), new float3(40f, 13f, 2f), 0.15f, 3f));
        total = math.max(total, GeneratorPassUtils.builder_BoxDentedX(worldPosition, new float3(0f, 10f, -22f), new float3(40f, 13f, 2f), 0.15f, 3f));
        total = math.max(total, GeneratorPassUtils.builder_BoxDentedZ(worldPosition, new float3(22f, 10f, 0), new float3(2f, 13f, 40f), 0.15f, 3f));
        total = math.max(total, GeneratorPassUtils.builder_BoxDentedZ(worldPosition, new float3(-22f, 10f, 0), new float3(2f, 13f, 40f), 0.15f, 3f));

        return total;
    }
}
