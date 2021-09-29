using System.Runtime.CompilerServices;
using Eldemarkki.VoxelTerrain.Settings;
using Eldemarkki.VoxelTerrain.Utilities;
using Eldemarkki.VoxelTerrain.VoxelData;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;


[CreateAssetMenu(fileName = "DonutIslandGeneratorPass", menuName = "Custom/Terrain/Donut Island Generator Pass", order = -1)]
#region Asset
public class DonutIslandGeneratorPass : TerrainGeneratorPassAsset {

    [Header("Parameters")]
    public DonutIslandGeneratorPass_Parameters parameters;

    public override JobHandleWithData<IVoxelDataGenerationJob> GetGeneratorPassHandle (VoxelDataVolume voxelData, Bounds bounds, int seed, float size) {
        DonutIslandGeneratorPass_CalculationJob job = new DonutIslandGeneratorPass_CalculationJob {
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
public struct DonutIslandGeneratorPass_Parameters {
    public float noiseFrequency;                //0.034
    public int noiseOctaveCount;                //4
    public float islandRadius;                  //96
    public float islandRadiusMin;               
    public float donutRadius;
    public float donutHeightMultiplier;
    public float donutRadiusConeMultiplier;
    public int offestY;
    public float softShapeOverlayMultiplier;    //0.2
    public float softShapeMaxClamp;             //0.7
}


[BurstCompile]
public struct DonutIslandGeneratorPass_CalculationJob : IVoxelDataGenerationJob {

    public DonutIslandGeneratorPass_Parameters param { get; set; }
    public VoxelDataVolume outputVoxelData { get; set; }
    public int3 worldPositionOffset { get; set; }
    public int seed { get; set; }
    public float size { get; set; }


    public void Execute (int index) {
        int3 worldPosition = IndexUtilities.IndexToXyz(index, outputVoxelData.Width, outputVoxelData.Height) + worldPositionOffset;
        worldPosition.y -= param.offestY;
        outputVoxelData.SetVoxelData(CalculateVoxelData(worldPosition), index, 0);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float CalculateVoxelData (float3 worldPosition) {


        // Preparing and sampeling height-relative value
        float baseSoftNoise = GeneratorPassUtils.OctaveNoise(worldPosition.x, worldPosition.y, worldPosition.z, param.noiseFrequency, param.noiseOctaveCount, seed);


        float radius = math.lerp(param.islandRadiusMin, param.islandRadius, size);
        float2 pointOnCircle = math.normalizesafe(worldPosition.xz) * radius;
        if(math.all(pointOnCircle == float2.zero)) {
            pointOnCircle = new float2(radius, 0f);
        }
        float distanceToCircle2D = math.distance(float2.zero, worldPosition.xz);
        float distanceToCircle = math.distance(new float3(pointOnCircle.x, 0f, pointOnCircle.y), new float3(worldPosition.x, worldPosition.y * param.donutHeightMultiplier + distanceToCircle2D * param.donutRadiusConeMultiplier, worldPosition.z));


        // Composing a soft cone shape
        float softShape = distanceToCircle;
        softShape /= param.donutRadius;
        softShape = 1f - math.max(0f, softShape);


        // Merging noise and shape
        return (baseSoftNoise * softShape) + softShape * param.softShapeOverlayMultiplier;
    }
}
