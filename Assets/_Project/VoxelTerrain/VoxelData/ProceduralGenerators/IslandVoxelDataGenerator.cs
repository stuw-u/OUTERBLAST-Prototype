using Eldemarkki.VoxelTerrain.Settings;
using Eldemarkki.VoxelTerrain.Utilities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;
using UnityEngine;

namespace Eldemarkki.VoxelTerrain.VoxelData
{
    /// <summary>
    /// A generator that creates voxel data procedurally
    /// </summary>
    public class IslandVoxelDataGenerator : VoxelDataGenerator
    {
        /// <summary>
        /// The settings for the procedural generation
        /// </summary>
        [SerializeField] private ProceduralIslandSettings proceduralIslandSettings = new ProceduralIslandSettings();

        /// <summary>
        /// Starts generating the voxel data for a specified volume
        /// </summary>
        /// <param name="bounds">The volume to generate the voxel data for</param>
        /// <param name="allocator">The allocator for the new <see cref="VoxelDataVolume"/></param>
        /// <returns>The job handle and the voxel data generation job</returns>
        public override JobHandleWithData<IVoxelDataGenerationJob> GenerateVoxelData(Bounds bounds, Allocator allocator)
        {
            VoxelDataVolume voxelData = new VoxelDataVolume(bounds.size.ToInt3(), allocator);
            IslandTerrainVoxelDataCalculationJob job = new IslandTerrainVoxelDataCalculationJob {
                worldPositionOffset = bounds.min.ToInt3(),
                outputVoxelData = voxelData,
                ProceduralIslandSettings = proceduralIslandSettings,
                seed = (LobbyWorldInterface.inst == null) ? 0 : LobbyWorldInterface.inst.matchTerrainInfo.seed
            };
            job.Matrix = float4x4.EulerXYZ(new float3(math.radians(0f), math.radians(0f), math.radians(0f)));

            JobHandle jobHandle = job.Schedule(voxelData.Length, 256);

            JobHandleWithData<IVoxelDataGenerationJob> jobHandleWithData = new JobHandleWithData<IVoxelDataGenerationJob>();
            jobHandleWithData.JobHandle = jobHandle;
            jobHandleWithData.JobData = job;

            return jobHandleWithData;
        }
    }
}