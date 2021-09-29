using Eldemarkki.VoxelTerrain.Meshing.Data;
using Eldemarkki.VoxelTerrain.VoxelData;
using Eldemarkki.VoxelTerrain.Utilities;
using Unity.Burst;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using System.Runtime.CompilerServices;

[BurstCompile]
public struct NormalFromVoxelDataJob : IJobParallelFor {

    public int3 size { get; set; }
    [ReadOnly] private NativeHashMap<int3, BlitableArray<ulong>> _voxelData;
    [WriteOnly, NativeDisableParallelForRestriction] private NativeArray<float3> _normalField;

    public NativeHashMap<int3, BlitableArray<ulong>> VoxelData { get => _voxelData; set => _voxelData = value; } // The voxel data to generate the mesh from
    public NativeArray<float3> OutputNormalField { get => _normalField; set => _normalField = value; } // The normal field generate the mesh from


    public void Execute (int index) {
        // Unpack the index
        // Correctly sample grids value
        int3 p = IndexUtilities.IndexToXyz(index, size.x + 1, size.y + 1);
        _normalField[index] = getNormal(p);
    }


    private float3 getNormal (int3 p) {
        return new float3(
            getGridValue(p.x - 1, p.y, p.z) - getGridValue(p.x + 1, p.y, p.z),
            getGridValue(p.x, p.y - 1, p.z) - getGridValue(p.x, p.y + 1, p.z),
            getGridValue(p.x, p.y, p.z - 1) - getGridValue(p.x, p.y, p.z + 1)
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float getGridValue (int x, int y, int z) {
        int3 c = new int3(
            (int)math.floor((float)x / size.x),
            (int)math.floor((float)y / size.y),
            (int)math.floor((float)z / size.z));
        if(_voxelData.TryGetValue(c, out BlitableArray<ulong> data)) {
            return Extract(data[IndexUtilities.XyzToIndex(
                x - c.x * (size.x - 1),
                y - c.y * (size.y - 1),
                z - c.z * (size.z - 1),
                size.x, size.y)], 0) * 0.00392156862f;
        } else {
            return 0f;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte Extract (ulong data, byte index) {
        return (byte)((data >> (index * 8)) & 0xFF);
    }
}
