using System.Runtime.CompilerServices;
using Eldemarkki.VoxelTerrain.Settings;
using Eldemarkki.VoxelTerrain.Utilities;
using Eldemarkki.VoxelTerrain.VoxelData;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;

[CreateAssetMenu(fileName = "RidgedBillowPlanetoidGeneratorPass", menuName = "Custom/Terrain/Ridged Billow Planetoid Planetoid Generator Pass", order = -1)]
#region Asset
public class RidgedBillowPlanetoidGeneratorPass : TerrainGeneratorPassAsset {

    [Header("Parameters")]
    public RidgedBillowPlanetoidGeneratorPass_Parameters parameters;

    public override JobHandleWithData<IVoxelDataGenerationJob> GetGeneratorPassHandle (VoxelDataVolume voxelData, Bounds bounds, int seed, float size) {
        RidgedBillowPlanetoidGeneratorPass_CalculationJob job = new RidgedBillowPlanetoidGeneratorPass_CalculationJob {
            param = parameters,
            worldPositionOffset = bounds.min.ToInt3(),
            outputVoxelData = voxelData,
            seed = seed,
            size = size,
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
public struct RidgedBillowPlanetoidGeneratorPass_Parameters {
    public float planetRadius;
    public float planetRadiusMin;
    public float planetThinkness;
    public float planetSmoothness;
    public float baseShapeMul;
    public float noiseFrequency;                //0.034
    public int noiseOctaveCount;                //4
    public float freqMul; //0.2
    public float ampMul; //0.45
    public float billowRidgedMix;
}


[BurstCompile]
public struct RidgedBillowPlanetoidGeneratorPass_CalculationJob : IVoxelDataGenerationJob {

    public RidgedBillowPlanetoidGeneratorPass_Parameters param { get; set; }
    public VoxelDataVolume outputVoxelData { get; set; }
    public int3 worldPositionOffset { get; set; }
    public int seed { get; set; }
    public float size { get; set; }


    public void Execute (int index) {
        int3 worldPosition = IndexUtilities.IndexToXyz(index, outputVoxelData.Width, outputVoxelData.Height) + worldPositionOffset;

        float voxelData = CalculateVoxelData(worldPosition);
        outputVoxelData.SetVoxelData(voxelData, index, 0);

    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float CalculateVoxelData (float3 worldPosition) {

        float radius = math.lerp(param.planetRadiusMin, param.planetRadius, size);

        float noiseOutput = GeneratorPassUtils.OctaveNoiseBillowRidged(worldPosition.x, worldPosition.y, worldPosition.z, param.noiseFrequency, param.noiseOctaveCount, seed, param.billowRidgedMix, param.freqMul, param.ampMul).x;
        float baseSoftNoise = noiseOutput;

        float sphereDist = math.distance(float3.zero, worldPosition);
        float sphereSmoothShape = math.saturate((param.planetThinkness - math.abs(sphereDist - radius)) / param.planetThinkness) * param.baseShapeMul;

        return baseSoftNoise * sphereSmoothShape;
    }
}

