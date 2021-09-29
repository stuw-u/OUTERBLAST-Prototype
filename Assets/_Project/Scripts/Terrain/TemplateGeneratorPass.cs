using System.Runtime.CompilerServices;
using Eldemarkki.VoxelTerrain.Settings;
using Eldemarkki.VoxelTerrain.Utilities;
using Eldemarkki.VoxelTerrain.VoxelData;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;




// STEP 1: 'Template' with your new generator. Do not forget to add the space (if there should be one)
// in the asset creator attribute.

// STEP 2: Pick blending mode




[CreateAssetMenu(fileName = "TemplateGeneratorPass", menuName = "Custom/Terrain/Template Generator Pass", order = -1)]
#region Asset
public class TemplateGeneratorPass : TerrainGeneratorPassAsset {

    [Header("Parameters")]
    public TemplateGeneratorPass_Parameters parameters;
    
    public override JobHandleWithData<IVoxelDataGenerationJob> GetGeneratorPassHandle (VoxelDataVolume voxelData, Bounds bounds, int seed, float size) {
        TemplateGeneratorPass_CalculationJob job = new TemplateGeneratorPass_CalculationJob {
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
public struct TemplateGeneratorPass_Parameters {

}


[BurstCompile]
public struct TemplateGeneratorPass_CalculationJob : IVoxelDataGenerationJob {

    public TemplateGeneratorPass_Parameters param { get; set; }
    public VoxelDataVolume outputVoxelData { get; set; }
    public int3 worldPositionOffset { get; set; }
    public int seed { get; set; }
    public float size { get; set; }


    public void Execute (int index) {
        int3 worldPosition = IndexUtilities.IndexToXyz(index, outputVoxelData.Width, outputVoxelData.Height) + worldPositionOffset;

        /* --- MAX BLENDING
        float voxelData = outputVoxelData.GetVoxelData(index, 0);
        voxelData = math.max(voxelData, CalculateVoxelData(worldPosition));
        outputVoxelData.SetVoxelData(voxelData, index, 0);
        */

        /* --- MIN BLENDING
        float voxelData = outputVoxelData.GetVoxelData(index, 0);
        voxelData = math.min(voxelData, CalculateVoxelData(worldPosition));
        outputVoxelData.SetVoxelData(voxelData, index, 0);
        */

        /* --- MUL BLENDING
        float voxelData = outputVoxelData.GetVoxelData(index, 0);
        voxelData *= CalculateVoxelData(worldPosition);
        outputVoxelData.SetVoxelData(voxelData, index, 0);
        */

        /* --- ADD BLENDING
        float voxelData = outputVoxelData.GetVoxelData(index, 0);
        voxelData += CalculateVoxelData(worldPosition);
        outputVoxelData.SetVoxelData(voxelData, index, 0);
        */

        /* --- SET VALUE
        float voxelData = CalculateVoxelData(worldPosition);
        outputVoxelData.SetVoxelData(voxelData, index, 0);
        */
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float CalculateVoxelData (float3 worldPosition) {
        return 0f;
    }
}
 