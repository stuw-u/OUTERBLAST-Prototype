using Unity.Jobs;
using Unity.Mathematics;

namespace Eldemarkki.VoxelTerrain.VoxelData
{
    /// <summary>
    /// An interface for voxel data generation jobs
    /// </summary>
    public interface IVoxelDataGenerationJob : IJobParallelFor {
        int3 worldPositionOffset { get; set; }
        VoxelDataVolume outputVoxelData { get; set; }
    }
}