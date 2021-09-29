using System.Runtime.CompilerServices;
using Eldemarkki.VoxelTerrain.Settings;
using Eldemarkki.VoxelTerrain.Utilities;
using Eldemarkki.VoxelTerrain.VoxelData;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;


[CreateAssetMenu(fileName = "PyramidGeneratorPass", menuName = "Custom/Terrain/Pyramid Generator Pass", order = -1)]
#region Asset
public class PyramidGeneratorPass : TerrainGeneratorPassAsset {

    [Header("Parameters")]
    public PyramidGeneratorPass_Parameters parameters;

    public override JobHandleWithData<IVoxelDataGenerationJob> GetGeneratorPassHandle (VoxelDataVolume voxelData, Bounds bounds, int seed, float size) {
        PyramidGeneratorPass_CalculationJob job = new PyramidGeneratorPass_CalculationJob {
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

public enum PyramidGeneratorPass_Type {
    MaxOfXZ,
    AddXZ
}

[System.Serializable]
public struct PyramidGeneratorPass_Parameters {
    public float minHeight;
    public float maxHeight;
    public float maxBaseSize;
    public float falloff;
    public PyramidGeneratorPass_Type type;
}

[BurstCompile]
public struct PyramidGeneratorPass_CalculationJob : IVoxelDataGenerationJob {

    public PyramidGeneratorPass_Parameters param { get; set; }
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
        float heightValue = math.saturate(math.unlerp(param.minHeight, param.maxHeight, worldPosition.y));
        heightValue = math.select(heightValue, 1f, heightValue == 0f);
        float baseValue = math.lerp(param.maxBaseSize, 0f, heightValue) - math.select(
            math.max(math.abs(worldPosition.x), math.abs(worldPosition.z)),
            math.abs(worldPosition.x) + math.abs(worldPosition.z),
            param.type == PyramidGeneratorPass_Type.AddXZ
        );

        return baseValue * param.falloff;
    }
}
