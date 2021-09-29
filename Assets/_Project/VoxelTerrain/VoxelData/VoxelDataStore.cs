using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Eldemarkki.VoxelTerrain.Utilities;
using Eldemarkki.VoxelTerrain.Utilities.Intersection;
using Eldemarkki.VoxelTerrain.World;
using Eldemarkki.VoxelTerrain.World.Chunks;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;
using UnityEngine;
using System;

namespace Eldemarkki.VoxelTerrain.VoxelData
{
    /// <summary>
    /// A store which handles getting and setting the voxel data for the world
    /// </summary>
    public class VoxelDataStore : MonoBehaviour {

        private Dictionary<int3, VoxelDataVolume> _chunks;
        private Dictionary<int3, JobHandleWithData<IVoxelDataGenerationJob>> _generationJobHandles;
        public VoxelWorld VoxelWorld { get; set; }

        public void Init () {
            _chunks = new Dictionary<int3, VoxelDataVolume>();
            _generationJobHandles = new Dictionary<int3, JobHandleWithData<IVoxelDataGenerationJob>>();
        }

        public void Dispose () {
            if(_chunks == null) {
                return;
            }

            foreach(VoxelDataVolume chunk in _chunks.Values) {
                chunk.Dispose();
            }
        }
        
        public bool TryGetVoxelData (int3 worldPosition, byte coll, out float voxelData) {
            int3 chunkCoordinate = VectorUtilities.WorldPositionToCoordinate(worldPosition, VoxelWorld.WorldSettings.ChunkSize);
            ApplyChunkChanges(chunkCoordinate);
            if(_chunks.TryGetValue(chunkCoordinate, out VoxelDataVolume chunk)) {
                int3 voxelDataLocalPosition = worldPosition.Mod(VoxelWorld.WorldSettings.ChunkSize);
                return chunk.TryGetVoxelData(voxelDataLocalPosition.x, voxelDataLocalPosition.y, voxelDataLocalPosition.z, coll, out voxelData);
            } else {
                voxelData = 0;
                return false;
            }
        }
        
        public bool TryGetVoxelDataChunk (int3 chunkCoordinate, out VoxelDataVolume chunk) {
            ApplyChunkChanges(chunkCoordinate);
            return _chunks.TryGetValue(chunkCoordinate, out chunk);
        }
        
        public NativeArray<float> GetSurroundingVoxelData (int3 chunkCoordinate, int3 size) {
            NativeArray<float> surroundingVoxelData = new NativeArray<float>((size.x + 2) * (size.y + 2) * (size.z + 2), Allocator.Persistent);

            for(int cx = -1; cx < size.x + 1; cx++) {
                for(int cy = -1; cy < size.y + 1; cy++) {
                    for(int cz = -1; cz < size.z + 1; cz++) {
                        int3 cOffset = new int3(
                            (int)math.floor((float)cx / size.x),
                            (int)math.floor((float)cy / size.y),
                            (int)math.floor((float)cz / size.z));
                        int3 c = new int3(
                            chunkCoordinate.x + cOffset.x,
                            chunkCoordinate.y + cOffset.y,
                            chunkCoordinate.z + cOffset.z);
                        int3 p = new int3(
                            cx - cOffset.x * (size.x - 1),
                            cy - cOffset.y * (size.y - 1),
                            cz - cOffset.z * (size.z - 1));

                        if(_chunks.TryGetValue(c, out VoxelDataVolume chunk)) {
                            ApplyChunkChanges(c);
                            surroundingVoxelData[IndexUtilities.XyzToIndex(cx + 1, cy + 1, cz + 1, size.x + 2, size.y + 2)] = chunk.GetVoxelData(p, 0);
                        }
                    }
                }
            }

            return surroundingVoxelData;
        }
        
        public NativeHashMap<int3, BlitableArray<uint>> GetSurroundingChunkData (int3 chunkCoordinate) {
            NativeHashMap<int3, BlitableArray<uint>> neighbourChunkData = new NativeHashMap<int3, BlitableArray<uint>>(27, Allocator.Persistent);
            for(int cx = -1; cx < 2; cx++) {
                for(int cy = -1; cy < 2; cy++) {
                    for(int cz = -1; cz < 2; cz++) {
                        int3 c = new int3(
                            chunkCoordinate.x + cx,
                            chunkCoordinate.y + cy,
                            chunkCoordinate.z + cz);

                        if(_chunks.TryGetValue(c, out VoxelDataVolume chunk)) {
                            ApplyChunkChanges(c);
                            neighbourChunkData[new int3(cx, cy, cz)] = chunk._voxelData;
                        }
                    }
                }
            }
            return neighbourChunkData;
        }

        public void GroupedVoxelEdits (float3 origin, params VoxelEditParameters[] parameters) {
            // Find total edit region. Start by looping through all parameters to find max radius
            float maxRadius = 0f;
            for(int i = 0; i < parameters.Length; i++) {
                maxRadius = math.max(parameters[i].radius, maxRadius);
            }
            if(maxRadius <= 0f) {
                return;
            }

            // Prepare parameters
            NativeArray<VoxelEditParameters> paramsNativeArray = new NativeArray<VoxelEditParameters>(parameters.Length, Allocator.TempJob);
            for(int i = 0; i < parameters.Length; i++) {
                paramsNativeArray[i] = parameters[i];
            }

            // Calculate the bound of the edit
            int chunkSize = VoxelWorld.WorldSettings.ChunkSize;
            int3 min = (int3)math.floor(origin - new float3(maxRadius + 2f, maxRadius + 2f, maxRadius + 2f));
            int3 max = (int3)math.ceil(origin + new float3(maxRadius + 2f, maxRadius + 2f, maxRadius + 2f));
            int3 chunkMin = VectorUtilities.WorldPositionToCoordinate(min, chunkSize);
            int3 chunkMax = VectorUtilities.WorldPositionToCoordinate(max, chunkSize);
            int chunkCount = 0;

            // Prepare count chunks, prepare jobs
            for(int chunkX = chunkMin.x; chunkX <= chunkMax.x; chunkX++) {
                for(int chunkY = chunkMin.y; chunkY <= chunkMax.y; chunkY++) {
                    for(int chunkZ = chunkMin.z; chunkZ <= chunkMax.z; chunkZ++) {
                        int3 chunkCoords = new int3(chunkX, chunkY, chunkZ);
                        if(VoxelWorld.ChunkStore.TryGetChunkAtCoordinate(chunkCoords, out Chunk chunk)) {
                            if(!TryGetVoxelDataChunk(chunkCoords, out VoxelDataVolume voxelDataVolume))
                                continue;
                            chunk.HasChanged = true;
                            chunkCount++;
                        }
                    }
                }
            }
            NativeArray<JobHandle> editJobHandle = new NativeArray<JobHandle>(chunkCount, Allocator.Temp);
            int c = -1;
            for(int chunkX = chunkMin.x; chunkX <= chunkMax.x; chunkX++) {
                for(int chunkY = chunkMin.y; chunkY <= chunkMax.y; chunkY++) {
                    for(int chunkZ = chunkMin.z; chunkZ <= chunkMax.z; chunkZ++) {
                        int3 chunkCoords = new int3(chunkX, chunkY, chunkZ);
                        if(!VoxelWorld.ChunkStore.TryGetChunkAtCoordinate(chunkCoords, out Chunk chunk))
                            continue;
                        if(!TryGetVoxelDataChunk(chunkCoords, out VoxelDataVolume voxelDataVolume))
                            continue;
                        c++;

                        VoxelDataEditJob editJob = new VoxelDataEditJob() {
                            chunkData = voxelDataVolume._voxelData,
                            chunkPosition = chunkCoords,
                            origin = origin,
                            parameters = paramsNativeArray,
                            size = new int3(chunkSize + 1, chunkSize + 1, chunkSize + 1)
                        };
                        JobHandle jobHandle = editJob.Schedule((chunkSize + 1) * (chunkSize + 1) * (chunkSize + 1), 128);
                        editJobHandle[c] = jobHandle;
                    }
                }
            }
            for(int i = 0; i < c + 1; i++) {
                editJobHandle[i].Complete();
            }
            paramsNativeArray.Dispose();
        }

        public void SpawnIslandGeneration (float3 origin, float radius, float height) {

            // Find region size, effect radius, etc.
            #region Chunk Bound Preparations
            float maxRadius = radius;
            if(maxRadius <= 0f)
                return;

            int chunkSize = VoxelWorld.WorldSettings.ChunkSize;
            int3 min = (int3)math.floor(origin - new float3(maxRadius + 2f, maxRadius + 2f, maxRadius + 2f));
            int3 max = (int3)math.ceil(origin + new float3(maxRadius + 2f, maxRadius + 2f, maxRadius + 2f));
            int3 chunkMin = VectorUtilities.WorldPositionToCoordinate(min, chunkSize);
            int3 chunkMax = VectorUtilities.WorldPositionToCoordinate(max, chunkSize);
            #endregion

            // Run trough all chunks, do chunk specific operations, then run through all tile in the chunk.
            for(int chunkX = chunkMin.x; chunkX <= chunkMax.x; chunkX++)
                for(int chunkY = chunkMin.y; chunkY <= chunkMax.y; chunkY++)
                    for(int chunkZ = chunkMin.z; chunkZ <= chunkMax.z; chunkZ++) {

                        #region Chunk Checks
                        bool chunkAffected = false;

                        int3 chunkCoords = new int3(chunkX, chunkY, chunkZ);
                        int3 chunkCoordsWorld = chunkCoords * chunkSize;
                        if(!_chunks.ContainsKey(chunkCoords)) {
                            continue;
                        }
                        if(!TryGetVoxelDataChunk(chunkCoords, out VoxelDataVolume voxelDataVolume)) {
                            continue;
                        }
                        #endregion

                        // Running through all cells
                        int index = 0;
                        for(int z = chunkCoordsWorld.z; z <= chunkSize + chunkCoordsWorld.z; z++)
                            for(int y = chunkCoordsWorld.y; y <= chunkSize + chunkCoordsWorld.y; y++)
                                for(int x = chunkCoordsWorld.x; x <= chunkSize + chunkCoordsWorld.x; x++) {

                                    #region Unaffected Cells
                                    float3 posToCenter = new float3(origin.x - x, origin.y - y, origin.z - z);
                                    bool isOutsideBound =
                                        !(x >= min.x && x <= max.x) ||
                                        !(y >= min.y && y <= max.y) ||
                                        !(z >= min.z && z <= max.z);
                                    if(isOutsideBound) {
                                        index++;
                                        continue;
                                    }
                                    float distSqr = math.lengthsq(posToCenter);
                                    if(distSqr > radius * radius) {
                                        index++;
                                        continue;
                                    }
                                    #endregion

                                    float distance = math.sqrt(distSqr) / radius;
                                    chunkAffected = true;

                                    voxelDataVolume.IncreaseVoxelData(math.select(3f, 1f, posToCenter.y < 0f) * (posToCenter.y / radius) * (1f - distance), index, 0);
                                    index++;
                                }

                        // Notify chunk about cell changes
                        if(chunkAffected && VoxelWorld.ChunkStore.TryGetChunkAtCoordinate(chunkCoords, out Chunk chunk)) {
                            chunk.HasChanged = true;
                        }
                    }
        }
        
        public void GetChunksInRegion (float3 min, float3 max, Action<Chunk> action) {

            // Calculate the bound of the edit
            int chunkSize = VoxelWorld.WorldSettings.ChunkSize;
            int3 chunkMin = VectorUtilities.WorldPositionToCoordinate(math.floor(min), chunkSize);
            int3 chunkMax = VectorUtilities.WorldPositionToCoordinate(math.floor(max), chunkSize);

            // Run trough all chunks, do chunk specific operations, then run through all tile in the chunk.
            for(int chunkX = chunkMin.x; chunkX <= chunkMax.x; chunkX++) {
                for(int chunkY = chunkMin.y; chunkY <= chunkMax.y; chunkY++) {
                    for(int chunkZ = chunkMin.z; chunkZ <= chunkMax.z; chunkZ++) {
                        int3 chunkCoords = new int3(chunkX, chunkY, chunkZ);

                        if(VoxelWorld.ChunkStore.TryGetChunkAtCoordinate(chunkCoords, out Chunk chunk)) {
                            action(chunk);
                        }
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float distanceSqr (float dx, float dy, float dz) {
            return (dx * dx + dy * dy + dz * dz);
        }

        public void SetVoxelData (float voxelData, int3 worldPosition, byte coll) {
            IEnumerable<int3> affectedChunkCoordinates = ChunkStore.GetChunkCoordinatesContainingPoint(worldPosition, VoxelWorld.WorldSettings.ChunkSize);

            foreach(int3 chunkCoordinate in affectedChunkCoordinates) {
                if(!_chunks.ContainsKey(chunkCoordinate)) { continue; }

                if(TryGetVoxelDataChunk(chunkCoordinate, out VoxelDataVolume voxelDataVolume)) {
                    int3 localPos = (worldPosition - chunkCoordinate * VoxelWorld.WorldSettings.ChunkSize).Mod(VoxelWorld.WorldSettings.ChunkSize + 1);
                    voxelDataVolume.SetVoxelData(voxelData, localPos.x, localPos.y, localPos.z, coll);

                    if(VoxelWorld.ChunkStore.TryGetChunkAtCoordinate(chunkCoordinate, out Chunk chunk)) {
                        chunk.HasChanged = true;
                    }
                }
            }
        }

        public void SetVoxelDataChunk (VoxelDataVolume chunkVoxelData, int3 chunkCoordinate) {
            if(_chunks.TryGetValue(chunkCoordinate, out VoxelDataVolume voxelDataVolume)) {
                voxelDataVolume.CopyFrom(chunkVoxelData);
            } else {
                _chunks.Add(chunkCoordinate, chunkVoxelData);
            }

            if(VoxelWorld.ChunkStore.TryGetChunkAtCoordinate(chunkCoordinate, out Chunk chunk)) {
                chunk.HasChanged = true;
            }
        }

        public void SetVoxelDataJobHandle (JobHandleWithData<IVoxelDataGenerationJob> generationJobHandle, int3 chunkCoordinate) {
            if(!_generationJobHandles.ContainsKey(chunkCoordinate)) {
                _generationJobHandles.Add(chunkCoordinate, generationJobHandle);
            }
        }

        public void ApplyChunkChanges (int3 chunkCoordinate) {
            if(_generationJobHandles.TryGetValue(chunkCoordinate, out JobHandleWithData<IVoxelDataGenerationJob> jobHandle)) {
                jobHandle.JobHandle.Complete();
                SetVoxelDataChunk(jobHandle.JobData.outputVoxelData, chunkCoordinate);
                _generationJobHandles.Remove(chunkCoordinate);
            }
        }

        public static void DrawBounds (Bounds b, Color color, float delay = 0) {
    // bottom
    var p1 = new Vector3(b.min.x, b.min.y, b.min.z);
    var p2 = new Vector3(b.max.x, b.min.y, b.min.z);
    var p3 = new Vector3(b.max.x, b.min.y, b.max.z);
    var p4 = new Vector3(b.min.x, b.min.y, b.max.z);

    Debug.DrawLine(p1, p2, Color.red, delay);
    Debug.DrawLine(p2, p3, Color.red, delay);
    Debug.DrawLine(p3, p4, Color.red, delay);
    Debug.DrawLine(p4, p1, Color.red, delay);

    // top
    var p5 = new Vector3(b.min.x, b.max.y, b.min.z);
    var p6 = new Vector3(b.max.x, b.max.y, b.min.z);
    var p7 = new Vector3(b.max.x, b.max.y, b.max.z);
    var p8 = new Vector3(b.min.x, b.max.y, b.max.z);

    Debug.DrawLine(p5, p6, Color.red, delay);
    Debug.DrawLine(p6, p7, Color.red, delay);
    Debug.DrawLine(p7, p8, Color.red, delay);
    Debug.DrawLine(p8, p5, Color.red, delay);

    // sides
    Debug.DrawLine(p1, p5, Color.red, delay);
    Debug.DrawLine(p2, p6, Color.red, delay);
    Debug.DrawLine(p3, p7, Color.red, delay);
    Debug.DrawLine(p4, p8, Color.red, delay);
}
    }
}

public enum VoxelEditType {
    Increment,
    Min,
    Max
}

public struct VoxelEditParameters {
    public VoxelEditType type;
    public float radius;
    public float value;
    public int layer;

    public VoxelEditParameters (VoxelEditType type, float radius, float value, int layer) {
        this.type = type;
        this.radius = radius;
        this.value = value;
        this.layer = layer;
    }
}