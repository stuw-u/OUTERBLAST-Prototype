using System.Runtime.CompilerServices;
using Eldemarkki.VoxelTerrain.Settings;
using Eldemarkki.VoxelTerrain.Utilities;
using Eldemarkki.VoxelTerrain.VoxelData;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;


[CreateAssetMenu(fileName = "FracturedIslandGeneratorPass", menuName = "Custom/Terrain/Fractured Island Generator Pass", order = -1)]
#region Asset
public class FracturedIslandGeneratorPass : TerrainGeneratorPassAsset {

    [Header("Parameters")]
    public FracturedIslandGeneratorPass_Parameters parameters;

    public override JobHandleWithData<IVoxelDataGenerationJob> GetGeneratorPassHandle (VoxelDataVolume voxelData, Bounds bounds, int seed, float size) {
        FracturedIslandGeneratorPass_CalculationJob job = new FracturedIslandGeneratorPass_CalculationJob {
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
public struct FracturedIslandGeneratorPass_Parameters {
    public float islandTop;             //10    
    public float islandBottom;          //32
    public float islandBottomMin;
    public float islandRadius;          //96
    public float islandRadiusMin;
    public float fracturationFreqency;  //0.05
    public float fracturationFreqencyMin;
    public float fracturationOffset;    //10
    public float fracturationSize;      //0.3f
    public float fracturationSizeMin;      
    public float noiseFrequency;        //0.034
    public int noiseOctaveCount;        //4
    public float softShapeOverlayMultiplier;    //0.2
    public float softShapeMaxClamp;             //0.7
    public float hillsMaxHeight;                //16
    public float minHillMul;                    //0.3
    public float maxHillMul;                    //0.6
    public bool isBlocky;
}


[BurstCompile]
public struct FracturedIslandGeneratorPass_CalculationJob : IVoxelDataGenerationJob {

    public FracturedIslandGeneratorPass_Parameters param { get; set; }
    public VoxelDataVolume outputVoxelData { get; set; }
    public int3 worldPositionOffset { get; set; }
    public int seed { get; set; }
    public float size { get; set; }


    public void Execute (int index) {
        int3 worldPosition = IndexUtilities.IndexToXyz(index, outputVoxelData.Width, outputVoxelData.Height) + worldPositionOffset;
        worldPosition = math.select(worldPosition, (int3)(math.floor((float3)worldPosition * 0.3333f) * 3f), param.isBlocky);

        float voxelDataValue = CalculateVoxelData(worldPosition);
        float voxelData = math.select(voxelDataValue, 1f - math.step(CalculateVoxelData(worldPosition), 0.5f), param.isBlocky);
        outputVoxelData.SetVoxelData(voxelData, index, 0);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float CalculateVoxelData (float3 worldPosition) {

        float radius = math.lerp(param.islandRadiusMin, param.islandRadius, size);
        float bottom = math.lerp(param.islandBottomMin, param.islandBottom, size);
        float fractSize = math.lerp(param.fracturationSizeMin, param.fracturationSize, size);
        float fractFreq = math.lerp(param.fracturationFreqencyMin, param.fracturationFreqency, size);
        float invFractSize = 1f / fractSize;

        float2 noiseOffset = (GeneratorPassUtils.hash2_2(seed) - 0.5f) * 10000f;
        float2 fracturation = GeneratorPassUtils.voronoiDistance(worldPosition.xz * fractFreq + noiseOffset);
        float fracture_value = math.lerp(1f - fractSize, 1f, math.min(fractSize, fracturation.x) * invFractSize);



        // Preparing and sampeling height-relative value
        float baseSoftNoise = GeneratorPassUtils.OctaveNoise(worldPosition.x, worldPosition.y, worldPosition.z, param.noiseFrequency, param.noiseOctaveCount, seed);
        float offsetedY = worldPosition.y - fracturation.y * param.fracturationOffset;
        float topToBottomValue = math.saturate(math.unlerp(bottom, param.islandTop, offsetedY));
        float bottomFadeValue = math.saturate(math.unlerp(bottom, bottom + 10f, worldPosition.y));
        float topToUpperRegionValue = math.select(0.8f, math.lerp(param.maxHillMul, param.minHillMul, math.saturate(math.unlerp(param.islandTop, param.islandTop + param.hillsMaxHeight, offsetedY))), offsetedY > param.islandTop);
        float radiusAtHeight = math.lerp(0, topToBottomValue, radius);
        float inlerpUnder = math.saturate(math.unlerp(bottom - 8, param.islandTop, offsetedY));
        float radiusUnder = math.lerp(0, inlerpUnder, radius);


        // The distance from center line to a point in the XZ plane
        float cylinderDistance = math.sqrt(worldPosition.x * worldPosition.x + worldPosition.z * worldPosition.z);


        // Composing a soft cone shape
        float softConeShape = cylinderDistance;
        softConeShape /= radiusAtHeight;
        softConeShape = 1f - softConeShape;
        softConeShape = math.clamp(softConeShape * bottomFadeValue, 0f, param.softShapeMaxClamp);
        softConeShape *= math.select(0f, 1f, worldPosition.y > bottom);
        softConeShape *= fracture_value;


        // Merging noise and shape
        float softShape = (softConeShape * topToUpperRegionValue);
        return (baseSoftNoise * softShape) + softShape * param.softShapeOverlayMultiplier;
    }
}
