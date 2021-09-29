using System.Runtime.CompilerServices;
using Eldemarkki.VoxelTerrain.Settings;
using Eldemarkki.VoxelTerrain.Utilities;
using Eldemarkki.VoxelTerrain.VoxelData;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;


[CreateAssetMenu(fileName = "CollesiumGeneratorPass", menuName = "Custom/Terrain/Collesium Generator Pass", order = -1)]
#region Asset
public class CollesiumGeneratorPass : TerrainGeneratorPassAsset {

    [Header("Parameters")]
    public CollesiumGeneratorPass_Parameters parameters;

    public override JobHandleWithData<IVoxelDataGenerationJob> GetGeneratorPassHandle (VoxelDataVolume voxelData, Bounds bounds, int seed, float size) {
        CollesiumGeneratorPass_CalculationJob job = new CollesiumGeneratorPass_CalculationJob {
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
public struct CollesiumGeneratorPass_Parameters {
    public float archCountX;// = 12f;
    public float archCountXMin;// = 12f;
    public float archCountY;// = 3f;
    public float archCountYMin;// = 3f;
    public float coliseumBottom;// = 10f;
    public float coliseumTop;// = 50f;
    public float coliseumTopMin;
    public float coliseumRadius;// = 30f;
    public float coliseumRadiusMin;
    public float coliseumWidth;// = 4f;
    public float coliseumInvWidth;//= 0.25f;
}


[BurstCompile]
public struct CollesiumGeneratorPass_CalculationJob : IVoxelDataGenerationJob {

    public CollesiumGeneratorPass_Parameters param { get; set; }
    public VoxelDataVolume outputVoxelData { get; set; }
    public int3 worldPositionOffset { get; set; }
    public int seed { get; set; }
    public float size { get; set; }


    public void Execute (int index) {
        int3 worldPosition = IndexUtilities.IndexToXyz(index, outputVoxelData.Width, outputVoxelData.Height) + worldPositionOffset;

        float voxelData = outputVoxelData.GetVoxelData(index, 0);
        voxelData = math.max(voxelData, CalculateVoxelData(worldPosition));
        outputVoxelData.SetVoxelData(voxelData, index, 0);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float CalculateVoxelData (float3 worldPosition) {

        float radius = math.lerp(param.coliseumRadiusMin, param.coliseumRadius, size);
        float top = math.lerp(param.coliseumTopMin, param.coliseumTop, size);

        int xCount = (int)math.round(math.lerp(param.archCountXMin, param.archCountX, size));
        int yCount = (int)math.round(math.lerp(param.archCountYMin, param.archCountY, size));

        float cylinderDistance = math.sqrt(worldPosition.x * worldPosition.x + worldPosition.z * worldPosition.z);
        float coliseumShape = math.saturate((param.coliseumWidth - math.abs(radius - cylinderDistance)) * math.select(0f, 1f, worldPosition.y > param.coliseumBottom && worldPosition.y < top) * param.coliseumInvWidth);
        float coliseumXCoords = GeneratorPassUtils.nfmod((math.atan2(worldPosition.x, worldPosition.z) * 0.15915494309f) * xCount);
        float coliseumYCoords = GeneratorPassUtils.nfmod(math.saturate(math.unlerp(param.coliseumBottom, top, worldPosition.y + 2f)) * yCount);
        coliseumShape -= GeneratorPassUtils.ArchCarve((coliseumXCoords * 2f - 1f) * 0.6f, (coliseumYCoords * 2f - 1f) * -0.6f, 0f, 2f);

        return coliseumShape;
    }
}
