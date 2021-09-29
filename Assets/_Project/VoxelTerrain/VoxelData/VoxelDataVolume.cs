using System;
using Eldemarkki.VoxelTerrain.Utilities;
using Unity.Collections;
using Unity.Mathematics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Eldemarkki.VoxelTerrain.VoxelData
{

    /// <summary>
    /// A 3-dimensional volume of voxel data
    /// </summary>
    public struct VoxelDataVolume : IDisposable {
        /// <summary>
        /// The native array which contains the voxel data. Voxel data is stored as bytes (0 to 255), and later mapped to go from 0 to 1
        /// </summary>
        public BlitableArray<uint> _voxelData;

        /// <summary>
        /// The width of the volume
        /// </summary>
        public int Width { get; }

        /// <summary>
        /// The height of the volume
        /// </summary>
        public int Height { get; }

        /// <summary>
        /// The depth of the volume
        /// </summary>
        public int Depth { get; }

        /// <summary>
        /// The size of this volume
        /// </summary>
        public int3 Size => new int3(Width, Height, Depth);

        /// <summary>
        /// How many voxel data points does this volume contain
        /// </summary>
        public int Length => Width * Height * Depth;

        /// <summary>
        /// Creates a <see cref="VoxelDataVolume"/> with a persistent allocator
        /// </summary>
        /// <param name="width">The width of the volume</param>
        /// <param name="height">The height of the volume</param>
        /// <param name="depth">The depth of the volume</param>
        /// <exception cref="ArgumentException">Thrown when any of the dimensions is negative</exception>
        public VoxelDataVolume (int width, int height, int depth) : this(width, height, depth, Allocator.Persistent) { }

        /// <summary>
        /// Creates a <see cref="VoxelDataVolume"/>
        /// </summary>
        /// <param name="width">The width of the volume</param>
        /// <param name="height">The height of the volume</param>
        /// <param name="depth">The depth of the volume</param>
        /// <param name="allocator">How the memory should be allocated</param>
        /// <exception cref="ArgumentException">Thrown when any of the dimensions is negative</exception>
        public VoxelDataVolume (int width, int height, int depth, Allocator allocator) {
            if(width < 0 || height < 0 || depth < 0) {
                throw new ArgumentException("The dimensions of this volume must all be positive!");
            }

            //_voxelData = new NativeArray<ulong>(width * height * depth, allocator);

            _voxelData = new BlitableArray<uint>(width * height * depth, allocator);
            for(int x = 0; x < _voxelData.Length; x++) {
                _voxelData[x] = 0;
            }

            Width = width;
            Height = height;
            Depth = depth;
        }

        /// <summary>
        /// Creates a <see cref="VoxelDataVolume"/> with a persistent allocator
        /// </summary>
        /// <param name="size">Amount of items in 1 dimension of this volume</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="size"/> is negative</exception>
        public VoxelDataVolume (int size) : this(size, Allocator.Persistent) { }

        /// <summary>
        /// Creates a <see cref="VoxelDataVolume"/>
        /// </summary>
        /// <param name="size">Amount of items in 1 dimension of this volume</param>
        /// <param name="allocator">How the memory should be allocated</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="size"/> is negative</exception>
        public VoxelDataVolume (int size, Allocator allocator) : this(size, size, size, allocator) { }

        /// <summary>
        /// Creates a <see cref="VoxelDataVolume"/> with a persistent allocator
        /// </summary>
        /// <param name="size">The 3-dimensional size of this volume</param>
        /// <exception cref="ArgumentException">Thrown when any of the dimensions is negative</exception>
        public VoxelDataVolume (int3 size) : this(size.x, size.y, size.z, Allocator.Persistent) { }

        /// <summary>
        /// Creates a <see cref="VoxelDataVolume"/>
        /// </summary>
        /// <param name="size">The 3-dimensional size of this volume</param>
        /// <param name="allocator">How the memory should be allocated</param>
        /// <exception cref="ArgumentException">Thrown when any of the dimensions is negative</exception>
        public VoxelDataVolume (int3 size, Allocator allocator) : this(size.x, size.y, size.z, allocator) { }

        /// <summary>
        /// Disposes the native voxel data array
        /// </summary>
        public void Dispose () {
            _voxelData.Dispose();
        }

        /// <summary>
        /// Stores the <paramref name="voxelData"/> at <paramref name="localPosition"/>. The <paramref name="voxelData"/> will be clamped to be in range [0, 1]
        /// </summary>
        /// <param name="voxelData">The new voxel data</param>
        /// <param name="localPosition">The location of that voxel data</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetVoxelData (float voxelData, int3 localPosition, byte coll) {
            int index = IndexUtilities.XyzToIndex(localPosition, Width, Height);
            SetVoxelData(voxelData, index, coll);
        }

        /// <summary>
        /// Stores the <paramref name="voxelData"/> at <paramref name="x"/>, <paramref name="y"/>, <paramref name="z"/>. The <paramref name="voxelData"/> will be clamped to be in range [0, 1]
        /// </summary>
        /// <param name="voxelData">The new voxel data</param>
        /// <param name="x">The x value of the voxel data location</param>
        /// <param name="y">The y value of the voxel data location</param>
        /// <param name="z">The z value of the voxel data location</param>
        /// [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetVoxelData (float voxelData, int x, int y, int z, byte coll) {
            int index = IndexUtilities.XyzToIndex(x, y, z, Width, Height);
            SetVoxelData(voxelData, index, coll);
        }

        /// <summary>
        /// Stores the <paramref name="voxelData"/> at <paramref name="index"/>. The <paramref name="voxelData"/> will be clamped to be in range [0, 1]
        /// </summary>
        /// <param name="voxelData">The new voxel data</param>
        /// <param name="index">The index in the native array</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetVoxelData (float voxelData, int index, byte coll) {
            uint value = _voxelData[index];

            _voxelData[index] = Insert(value, (byte)math.round(255f * math.saturate(voxelData)), coll);
        }

        /// <summary>
        /// Tries to get the voxel data at <paramref name="localPosition"/>. If the data exists at <paramref name="localPosition"/>, true will be returned and <paramref name="voxelData"/> will be set to the value (range [0, 1]). If it doesn't exist, false will be returned and <paramref name="voxelData"/> will be set to 0.
        /// </summary>
        /// <param name="localPosition">The local position of the voxel data to get</param>
        /// <param name="voxelData">A voxel data in the range [0, 1] at <paramref name="localPosition"/></param>
        /// <returns>Does a voxel data point exist at <paramref name="localPosition"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetVoxelData (int3 localPosition, byte coll, out float voxelData) {
            return TryGetVoxelData(localPosition.x, localPosition.y, localPosition.z, coll, out voxelData);
        }

        /// <summary>
        /// Tries to get the voxel data at <paramref name="x"/>, <paramref name="y"/>, <paramref name="z"/>. If the data exists at <paramref name="x"/>, <paramref name="y"/>, <paramref name="z"/>, true will be returned and <paramref name="voxelData"/> will be set to the value (range [0, 1]). If it doesn't exist, false will be returned and <paramref name="voxelData"/> will be set to 0.
        /// </summary>
        /// <param name="x">The x value of the voxel data location</param>
        /// <param name="y">The y value of the voxel data location</param>
        /// <param name="z">The z value of the voxel data location</param>
        /// <param name="voxelData">A voxel data in the range [0, 1] at <paramref name="x"/>, <paramref name="y"/>, <paramref name="z"/></param>
        /// <returns>Does a voxel data point exist at <paramref name="x"/>, <paramref name="y"/>, <paramref name="z"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetVoxelData (int x, int y, int z, byte coll, out float voxelData) {
            int index = IndexUtilities.XyzToIndex(x, y, z, Width, Height);
            return TryGetVoxelData(index, coll, out voxelData);
        }

        /// <summary>
        /// Gets the voxel data at <paramref name="index"/>. If the data exists at <paramref name="index"/>, true will be returned and <paramref name="voxelData"/> will be set to the value (range [0, 1]). If it doesn't exist, false will be returned and <paramref name="voxelData"/> will be set to 0.
        /// </summary>
        /// <param name="index">The index in the native array</param>
        /// <param name="voxelData">A voxel data in the range [0, 1] at <paramref name="index"/></param>
        /// <returns>Does a voxel data point exist at <paramref name="index"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetVoxelData (int index, byte coll, out float voxelData) {
            if(index >= 0 && index < _voxelData.Length) {
                voxelData = Extract(_voxelData[index], coll) / 255f;
                return true;
            }

            voxelData = 0;
            return false;
        }

        /// <summary>
        /// Gets the voxel data at <paramref name="localPosition"/>. If the data doesn't exist at <paramref name="localPosition"/>, an <see cref="IndexOutOfRangeException"/> will be thrown
        /// </summary>
        /// <param name="localPosition">The local position of the voxel data to get</param>
        /// <returns>The voxel data at <paramref name="localPosition"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetVoxelData (int3 localPosition, byte coll) {
            return GetVoxelData(localPosition.x, localPosition.y, localPosition.z, coll);
        }

        /// <summary>
        /// Gets the voxel data at <paramref name="x"/>, <paramref name="y"/>, <paramref name="z"/>. If the data doesn't exist at <paramref name="x"/>, <paramref name="y"/>, <paramref name="z"/>, an <see cref="IndexOutOfRangeException"/> will be thrown
        /// </summary>
        /// <param name="x">The x value of the voxel data location</param>
        /// <param name="y">The y value of the voxel data location</param>
        /// <param name="z">The z value of the voxel data location</param>
        /// <returns>The voxel data at <paramref name="x"/>, <paramref name="y"/>, <paramref name="z"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetVoxelData (int x, int y, int z, byte coll) {
            int index = IndexUtilities.XyzToIndex(x, y, z, Width, Height);
            return GetVoxelData(index, coll);
        }

        /// <summary>
        /// Gets the voxel data at <paramref name="index"/>. If the data doesn't exist at <paramref name="index"/>, an <see cref="IndexOutOfRangeException"/> will be thrown
        /// </summary>
        /// <param name="index">The index in the native array</param>
        /// <returns>The voxel data at <paramref name="index"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetVoxelData (int index, byte coll) {
            return Extract(_voxelData[index], coll) / 255f;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetVoxelDataExtract (uint data, byte coll) {
            return Extract(data, coll) / 255f;
        }

        /// <summary>
        /// Increases the voxel data at <paramref name="localPosition"/> by <paramref name="increaseAmount"/>. If <paramref name="localPosition"/> is out of <see cref="Size"/>, an <see cref="IndexOutOfRangeException"/> will be thrown.
        /// </summary>
        /// <param name="increaseAmount">How much the voxel data at <paramref name="localPosition"/> should be increased by</param>
        /// <param name="localPosition">The local position of the voxel data to increase</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void IncreaseVoxelData (float increaseAmount, int3 localPosition, byte coll) {
            int index = IndexUtilities.XyzToIndex(localPosition, Width, Height);
            IncreaseVoxelData(increaseAmount, index, coll);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void MaxVoxelData (float value, int3 localPosition, byte coll) {
            int index = IndexUtilities.XyzToIndex(localPosition, Width, Height);
            MaxVoxelData(value, index, coll);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void MinVoxelData (float value, int3 localPosition, byte coll) {
            int index = IndexUtilities.XyzToIndex(localPosition, Width, Height);
            MinVoxelData(value, index, coll);
        }

        /// <summary>
        /// Increases the voxel data at <paramref name="x"/>, <paramref name="y"/>, <paramref name="z"/> by <paramref name="increaseAmount"/>. If <paramref name="x"/>, <paramref name="y"/>, <paramref name="z"/> is out of <see cref="Size"/>, an <see cref="IndexOutOfRangeException"/> will be thrown.
        /// </summary>
        /// <param name="increaseAmount">How much the voxel data at <paramref name="x"/>, <paramref name="y"/>, <paramref name="z"/> should be increased by</param>
        /// <param name="x">The x value of the voxel data location</param>
        /// <param name="y">The y value of the voxel data location</param>
        /// <param name="z">The z value of the voxel data location</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void IncreaseVoxelData (float increaseAmount, int x, int y, int z, byte coll) {
            int index = IndexUtilities.XyzToIndex(x, y, z, Width, Height);
            IncreaseVoxelData(increaseAmount, index, coll);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void MaxVoxelData (float value, int x, int y, int z, byte coll) {
            int index = IndexUtilities.XyzToIndex(x, y, z, Width, Height);
            MaxVoxelData(value, index, coll);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void MinVoxelData (float value, int x, int y, int z, byte coll) {
            int index = IndexUtilities.XyzToIndex(x, y, z, Width, Height);
            MinVoxelData(value, index, coll);
        }

        /// <summary>
        /// Increases the voxel data at <paramref name="index"/> by <paramref name="increaseAmount"/>. If <paramref name="index"/> is outside of <see cref="Length"/>, an <see cref="IndexOutOfRangeException"/> will be thrown.
        /// </summary>
        /// <param name="increaseAmount"></param>
        /// <param name="index"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void IncreaseVoxelData (float increaseAmount, int index, byte coll) {
            uint data = _voxelData[index];
            byte newVoxelData = (byte)math.round(math.clamp((GetVoxelDataExtract(data, coll) + increaseAmount) * 255, 0, 255));
            _voxelData[index] = Insert(data, newVoxelData, coll);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void MaxVoxelData (float value, int index, byte coll) {
            uint data = _voxelData[index];
            byte newVoxelData = (byte)math.round(math.clamp(math.max(GetVoxelDataExtract(data, coll), value) * 255, 0, 255));
            _voxelData[index] = Insert(data, newVoxelData, coll);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void MinVoxelData (float value, int index, byte coll) {
            uint data = _voxelData[index];
            byte newVoxelData = (byte)math.round(math.clamp(math.min(GetVoxelDataExtract(data, coll), value) * 255, 0, 255));
            _voxelData[index] = Insert(data, newVoxelData, coll);
        }

        /// <summary>
        /// Copies the voxel data from the source volume if the volumes are the same size
        /// </summary>
        /// <param name="sourceVolume">The source volume, which should be the same size as this volume</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyFrom (VoxelDataVolume sourceVolume) {
            if(Width == sourceVolume.Width && Height == sourceVolume.Height && Depth == sourceVolume.Depth) {
                _voxelData.CopyFrom(sourceVolume._voxelData);
            } else {
                throw new ArgumentException($"The chunks are not the same size! Width: {Width.ToString()}/{sourceVolume.Width.ToString()}, Height: {Height.ToString()}/{sourceVolume.Height.ToString()}, Depth: {Depth.ToString()}/{sourceVolume.Depth.ToString()}");
            }
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
}