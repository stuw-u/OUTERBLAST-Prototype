using Eldemarkki.VoxelTerrain.Meshing.Data;
using Eldemarkki.VoxelTerrain.VoxelData;
using Eldemarkki.VoxelTerrain.Utilities;
using Unity.Burst;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using System.Runtime.CompilerServices;

[BurstCompile]
public struct VoxelDataEditJob : IJobParallelFor {

    public int3 chunkPosition { get; set; }
    public float3 origin { get; set; }
    public int3 size { get; set; }
    public BlitableArray<uint> chunkData;
    [ReadOnly] public NativeArray<VoxelEditParameters> parameters;

    public void Execute (int index) {
        int totalSize = size.x * size.y * size.z;
        int3 p = IndexUtilities.IndexToXyz(index, size.x, size.y);
        int3 worldP = p + chunkPosition * new int3(size.x - 1, size.y - 1, size.z - 1);
        float distance = math.distance(origin, worldP);

        uint data = chunkData[index];
        for(int l = 0; l < parameters.Length; l++) {
            float normalizedDist = math.saturate(distance / parameters[l].radius);
            switch(parameters[l].type) {
                case VoxelEditType.Increment:
                data = IncreaseVoxelData(data, (1f - normalizedDist) * parameters[l].value, index, (byte)parameters[l].layer);
                break;
                case VoxelEditType.Min:
                data = MinVoxelData(data, normalizedDist * parameters[l].value, index, (byte)parameters[l].layer);
                break;
                case VoxelEditType.Max:
                data = MaxVoxelData(data, (1f - normalizedDist) * parameters[l].value, index, (byte)parameters[l].layer);
                break;
            }
        }
        chunkData[index] = data;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint IncreaseVoxelData (uint data, float increaseAmount, int index, byte coll) {
        byte newVoxelData = (byte)math.round(math.clamp((GetVoxelDataExtract(data, coll) + increaseAmount) * 255, 0, 255));
        return Insert(data, newVoxelData, coll);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint MaxVoxelData (uint data, float value, int index, byte coll) {
        byte newVoxelData = (byte)math.round(math.clamp(math.max(GetVoxelDataExtract(data, coll), value) * 255, 0, 255));
        return Insert(data, newVoxelData, coll);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint MinVoxelData (uint data, float value, int index, byte coll) {
        byte newVoxelData = (byte)math.round(math.clamp(math.min(GetVoxelDataExtract(data, coll), value) * 255, 0, 255));
        return Insert(data, newVoxelData, coll);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float GetVoxelDataExtract (uint data, byte coll) {
        return Extract(data, coll) / 255f;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint Insert (uint data, byte value, byte index) {
        uint mask = (uint)0xFF << (index * 8);
        return (data & ~mask) | ((uint)value << (index * 8));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte Extract (uint data, byte index) {
        return (byte)((data >> (index * 8)) & 0xFF);
    }
}
