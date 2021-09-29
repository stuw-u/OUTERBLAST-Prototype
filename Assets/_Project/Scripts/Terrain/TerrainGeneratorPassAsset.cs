using Eldemarkki.VoxelTerrain.Utilities;
using Eldemarkki.VoxelTerrain.VoxelData;
using UnityEngine;



public class TerrainGeneratorPassAsset : ScriptableObject {
    new public string name;

    public virtual JobHandleWithData<IVoxelDataGenerationJob> GetGeneratorPassHandle (VoxelDataVolume voxelData, Bounds bounds, int seed, float size) {
        return null;
    }
}
