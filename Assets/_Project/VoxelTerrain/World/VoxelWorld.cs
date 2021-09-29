using Eldemarkki.VoxelTerrain.Meshing;
using Eldemarkki.VoxelTerrain.Meshing.Data;
using Eldemarkki.VoxelTerrain.Settings;
using Eldemarkki.VoxelTerrain.VoxelData;
using Eldemarkki.VoxelTerrain.World.Chunks;
using Eldemarkki.VoxelTerrain.Utilities;
using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Blast.Collections;

namespace Eldemarkki.VoxelTerrain.World {
    /// <summary>
    /// The main entry point for interacting with the voxel world
    /// </summary>
    public class VoxelWorld : MonoBehaviour {

        #region References
        public static VoxelWorld inst;

        [SerializeField] private WorldSettings worldSettings;
        public WorldSettings WorldSettings => worldSettings;

        [SerializeField] private VoxelMesher voxelMesher;
        public VoxelMesher VoxelMesher => voxelMesher;

        [SerializeField] private VoxelDataStore voxelDataStore;
        public VoxelDataStore VoxelDataStore => voxelDataStore;

        [SerializeField] private ChunkStore chunkStore;
        public ChunkStore ChunkStore => chunkStore;

        [HideInInspector] public List<Chunk> parrallelColliderMeshGeneration = new List<Chunk>();
        [HideInInspector] public List<int> parrallelMeshIds = new List<int>();
        #endregion

        [Header("Parameters")]
        [SerializeField] private int3 loadZoneMin = new int3(-3, -3, -3);
        [SerializeField] private int3 loadZoneMax = new int3(2, 2, 2);

        private TerrainTypeAsset terrainType;
        private int seed;

        public NativeArrayPool<GrassData> grassDataNativeArrayPool;
        public NativeArrayPool<float3> normalFieldNativeArrayPool;
        public NativeArrayPool<MeshingVertexData> verticiesNativeArrayPool;
        public NativeArrayPool<ushort> trisNativeArrayPool;


        public void Init (TerrainTypeAsset terrainType, int seed, float size) {
            inst = this;
            this.terrainType = terrainType;
            this.seed = seed;
            

            voxelDataStore.VoxelWorld = this;
            if(!NetAssist.IsHeadlessServer) {
                if(Blast.Settings.SettingsManager.settings != null) {
                    worldSettings.quality = Blast.Settings.SettingsManager.settings.grassQuality;
                } else if(NetAssist.inst.Mode.HasFlag(NetAssistMode.Record)) {
                    worldSettings.quality = NetAssist.inst.settings.grassQuality;
                }
            } else {
                worldSettings.quality = Blast.Settings.Quality.Low;
            }

            int chunkSize = WorldSettings.ChunkSize + 1;
            int voxelCount = (chunkSize - 1) * (chunkSize - 1) * (chunkSize - 1);
            int maxLength = 15 * voxelCount;
            grassDataNativeArrayPool = NativeArrayPool<GrassData>.Create(WorldSettings.maxGrassInstance[(int)WorldSettings.quality], NativeArrayOptions.UninitializedMemory);
            normalFieldNativeArrayPool = NativeArrayPool<float3>.Create((chunkSize + 1) * (chunkSize + 1) * (chunkSize + 1), NativeArrayOptions.UninitializedMemory);
            verticiesNativeArrayPool = NativeArrayPool<MeshingVertexData>.Create(maxLength, NativeArrayOptions.UninitializedMemory);
            trisNativeArrayPool = NativeArrayPool<ushort>.Create(maxLength, NativeArrayOptions.UninitializedMemory);

            voxelDataStore.Init();
            chunkStore.Init();

            GenerateTerrainInRegion(loadZoneMin, loadZoneMax, size);
        }

        public void Dispose () {
            grassDataNativeArrayPool.Dispose();
            normalFieldNativeArrayPool.Dispose();
            verticiesNativeArrayPool.Dispose();
            trisNativeArrayPool.Dispose();
            voxelDataStore.Dispose();
        }


        #region Collision Generation (LateUpdate)
        private void LateUpdate () {

            // Getting the ids
            foreach(Chunk chunk in parrallelColliderMeshGeneration) {
                parrallelMeshIds.Add(chunk.GetMeshId);
            }

            // Needing to apply, four mesh to four chunk should take 1/4 the time.
            Parallel.ForEach(parrallelMeshIds, (i) => {
                Physics.BakeMesh(i, false);
            });

            // Applying the baked mesh
            foreach(Chunk chunk in parrallelColliderMeshGeneration) {
                chunk.ApplyBakedCollisionMesh();
            }

            parrallelColliderMeshGeneration.Clear();
            parrallelMeshIds.Clear();
        }
        #endregion


        #region Chunk Generation
        private void GenerateTerrainInRegion (int3 min, int3 max, float size) {
            // Allocate the chunks
            for(int x = min.x; x <= max.x; x++)
                for(int y = min.y; y <= max.y; y++)
                    for(int z = min.z; z <= max.z; z++) {
                        AllocateVolumetricData(new int3(x, y, z), size);
                    }
            
            // For each pass, generate then force to complete at the end
            foreach(TerrainGeneratorPassAsset pass in terrainType.generationPasses) {
                for(int x = min.x; x <= max.x; x++)
                    for(int y = min.y; y <= max.y; y++)
                        for(int z = min.z; z <= max.z; z++) {
                            GenerateVolumetricData(new int3(x, y, z), pass, size);
                        }
                for(int x = min.x; x <= max.x; x++)
                    for(int y = min.y; y <= max.y; y++)
                        for(int z = min.z; z <= max.z; z++) {
                            VoxelDataStore.ApplyChunkChanges(new int3(x, y, z));
                        }
            }

            // Create the chunkies
            for(int x = min.x; x <= max.x; x++)
                for(int y = min.y; y <= max.y; y++)
                    for(int z = min.z; z <= max.z; z++)
                        CreateChunkAtCoordinate(new int3(x, y, z));
        }

        public void CreateChunkAtCoordinate (int3 chunkCoordinate) {
            int3 worldPosition = chunkCoordinate * WorldSettings.ChunkSize;
            Chunk chunk = Instantiate(WorldSettings.ChunkPrefab, worldPosition.ToVectorInt(), Quaternion.identity);
            chunk.transform.SetParent(transform);
            chunk.Initialize(chunkCoordinate, this);
            ChunkStore.AddChunk(chunk);
            chunk.GeneratePreparedMesh();
        }

        public void AllocateVolumetricData (int3 chunkCoordinate, float size) {
            Bounds chunkBounds = BoundsUtilities.GetChunkBounds(chunkCoordinate, WorldSettings.ChunkSize);
            VoxelDataVolume voxelData = new VoxelDataVolume(chunkBounds.size.ToInt3(), Allocator.Persistent);

            foreach(TerrainGeneratorPassAsset pass in terrainType.generationPasses) {
                JobHandleWithData<IVoxelDataGenerationJob> jobHandleWithData = pass.GetGeneratorPassHandle(voxelData, chunkBounds, seed, size);
                VoxelDataStore.SetVoxelDataJobHandle(jobHandleWithData, chunkCoordinate);
                VoxelDataStore.ApplyChunkChanges(chunkCoordinate);
            }
        }

        public void GenerateVolumetricData (int3 chunkCoordinate, TerrainGeneratorPassAsset pass, float size) {
            Bounds chunkBounds = BoundsUtilities.GetChunkBounds(chunkCoordinate, WorldSettings.ChunkSize);
            VoxelDataStore.TryGetVoxelDataChunk(chunkCoordinate, out VoxelDataVolume chunk);

            JobHandleWithData<IVoxelDataGenerationJob> jobHandleWithData = pass.GetGeneratorPassHandle(chunk, chunkBounds, seed, size);
            VoxelDataStore.SetVoxelDataJobHandle(jobHandleWithData, chunkCoordinate);
        }
        #endregion
    }
}