using Eldemarkki.VoxelTerrain.Utilities;
using Eldemarkki.VoxelTerrain.VoxelData;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine;
using Eldemarkki.VoxelTerrain.Meshing.Data;

namespace Eldemarkki.VoxelTerrain.Meshing
{
    public abstract class VoxelMesher : MonoBehaviour
    {

        /// <summary>
        /// Starts a mesh generation job
        /// </summary>
        /// <param name="voxelDataStore">The store where to retrieve the voxel data from</param>
        /// <param name="chunkCoordinate">The coordinate of the chunk that will be generated</param>
        /// <returns>The job handle and the actual mesh generation job</returns>
        public abstract void CreateMesh (VoxelDataStore voxelDataStore, int3 chunkCoordinate, Blast.Settings.Quality quality, 
            out JobHandleWithData<IMesherJob> mesherJob,
            out JobHandleWithData<NormalFromVoxelDataJob> normalJob);
    }
}