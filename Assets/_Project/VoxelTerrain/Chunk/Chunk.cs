using Eldemarkki.VoxelTerrain.Meshing.Data;
using Eldemarkki.VoxelTerrain.VoxelData;
using Eldemarkki.VoxelTerrain.Meshing;
using Eldemarkki.VoxelTerrain.Meshing.MarchingCubes;
using Eldemarkki.VoxelTerrain.Utilities;
using Blast.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;

namespace Eldemarkki.VoxelTerrain.World.Chunks {
    /// <summary>
    /// A component used for visualizing a chunk of the world 
    /// </summary>
    [RequireComponent(typeof(MeshRenderer), typeof(MeshFilter), typeof(MeshCollider))]
    [DefaultExecutionOrder(-1)]
    public class Chunk : MonoBehaviour {
        /// <summary>
        /// The world that "owns" this chunk
        /// </summary>

        private VoxelWorld _voxelWorld;
        private MeshFilter _meshFilter;
        private MeshCollider _meshCollider;
        private MeshRenderer _meshRender;

        public int3 ChunkCoordinate {
            get; set;
        }

        private bool grassEnabled;
        private ComputeBuffer grassDataBuffer;
        private ComputeBuffer grassMatriciesBuffer;
        private ComputeBuffer grassMatriciesInverseBuffer;

        private ComputeBuffer argsBuffer;
        private int cachedInstanceCount = 0;
        private uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
        private ComputeBuffer blastBuffer;
        public List<BlastProbeData> blastProbes;
        private JobHandleWithData<IMesherJob> mesherJob;
        const float explosionTimeout = 4f;

        #region Mesh Buffers
        public NativeHashMap<int3, BlitableArray<uint>> surroundingChunkData;
        public PoolableNativeArrayWrapper<MeshingVertexData> outputVertices;
        public PoolableNativeArrayWrapper<ushort> outputTriangles;
        public PoolableNativeArrayWrapper<GrassData> outputGrassData;
        #endregion

        //private Material materialChunkCopy;
        private Material grassMaterial;
        private MaterialPropertyBlock grassMaterialProperty;
        private ComputeShader computeChunkCopy;
        
        public bool HasChanged {
            get; set;
        }

        #region Monobehaviour
        private void Awake () {
            _meshFilter = GetComponent<MeshFilter>();
            _meshCollider = GetComponent<MeshCollider>();
            _meshRender = GetComponent<MeshRenderer>();
            _meshRender.material = LobbyWorldInterface.inst.terrainStyle.terrainMaterial;
        }

        private void OnDestroy () {
            ReleaseBuffers();
            if(computeChunkCopy)
                Destroy(computeChunkCopy);
        }

        public void ManualUpdate () {
            if(HasChanged) {
                PrepareMesh();
            }

            if(grassEnabled && cachedInstanceCount > 0 && LobbyWorldInterface.inst.terrainStyle != null) {
                Graphics.DrawMeshInstancedIndirect(
                    LobbyWorldInterface.inst.terrainStyle.decoMesh, 0, grassMaterial,
                    _meshRender.bounds, argsBuffer, 0, grassMaterialProperty, ShadowCastingMode.Off, true
                );
            }
        }

        public void ManualLateUpdate () {
            // Process the mesh that should've been baked my now *wink wink*
            if(mesherJob != null) {
                GeneratePreparedMesh();
            }
        }
        #endregion
        
        public void Initialize (int3 coordinate, VoxelWorld voxelWorld) {
            _voxelWorld = voxelWorld;
            grassEnabled = _voxelWorld.WorldSettings.quality > Blast.Settings.Quality.Low && LobbyWorldInterface.inst.terrainStyle.enableDeco;

            transform.position = coordinate.ToVectorInt() * voxelWorld.WorldSettings.ChunkSize;
            name = GetName(coordinate);
            ChunkCoordinate = coordinate;

            ReleaseBuffers();

            if(grassEnabled) {
                grassMaterialProperty = new MaterialPropertyBlock();
                //materialChunkCopy = new Material(_voxelWorld.WorldSettings.GrassMaterial);
                grassMaterial = LobbyWorldInterface.inst.terrainStyle.decoMaterial;
                blastBuffer = new ComputeBuffer(8, sizeof(float) * 5);
                //materialChunkCopy.SetBuffer("blastProbeData", blastBuffer);
                grassMaterialProperty.SetBuffer("blastProbeData", blastBuffer);
                blastProbes = new List<BlastProbeData>(8);
            }

            computeChunkCopy = Instantiate(_voxelWorld.WorldSettings.GrassDataToMatrixCompute);

            CreateBuffers();
            _meshFilter.sharedMesh = new Mesh();
            PrepareMesh();
            GeneratePreparedMesh();
        }

        private void PrepareMesh () {
            HasChanged = false;
            outputVertices = VoxelWorld.inst.verticiesNativeArrayPool.Get();
            outputTriangles = VoxelWorld.inst.trisNativeArrayPool.Get();
            outputGrassData = VoxelWorld.inst.grassDataNativeArrayPool.Get();
            CreateMesh(_voxelWorld.VoxelDataStore, ChunkCoordinate, _voxelWorld.WorldSettings.quality);
        }

        public void GeneratePreparedMesh () {
            if(mesherJob == null)
                return;

            mesherJob.JobHandle.Complete();

            IMesherJob mjob = mesherJob.JobData;

            Mesh mesh = _meshFilter.sharedMesh;
            SubMeshDescriptor subMesh = new SubMeshDescriptor(0, 0);
            int vertexCount = mjob.VertexCountCounter.Count * 3;
            mesh.SetVertexBufferParams(vertexCount, MeshingVertexData.VertexBufferMemoryLayout);
            mesh.SetIndexBufferParams(vertexCount, IndexFormat.UInt16);
            mesh.SetVertexBufferData(mjob.OutputVertices, 0, 0, vertexCount, 0, MeshUpdateFlags.DontValidateIndices); //MeshUpdateFlags.Default
            mesh.SetIndexBufferData(mjob.OutputTriangles, 0, 0, vertexCount, MeshUpdateFlags.DontValidateIndices); //MeshUpdateFlags.Default

            uint matriciesCount = (uint)mjob.GrassInstanceCounter.Count;
            UpdateGrassBuffers(mjob.OutputGrassData, matriciesCount);
            
            mjob.GrassInstanceCounter.Dispose();
            mjob.VertexCountCounter.Dispose();

            mesh.subMeshCount = 1;
            subMesh.indexCount = vertexCount;
            mesh.SetSubMesh(0, subMesh);
            mesh.RecalculateBounds();
            _voxelWorld.parrallelColliderMeshGeneration.Add(this);
            
            outputVertices.Return();
            outputTriangles.Return();
            outputGrassData.Return();

            mesherJob = null;
        }

        public void ApplyBakedCollisionMesh () {
            if(_meshFilter.sharedMesh != null)
                _meshCollider.sharedMesh = _meshFilter.sharedMesh;
        }

        private void UpdateGrassBuffers (NativeArray<GrassData> grassDataOutput, uint matriciesCount) {
            cachedInstanceCount = (int)matriciesCount;
            if(matriciesCount == 0) {
                args[0] = 0;
                args[1] = 0;
                args[2] = 0;
                args[3] = 0;
                argsBuffer.SetData(args);

                return;
            }

            int kernel = computeChunkCopy.FindKernel("CSMain");
            grassDataBuffer.SetData(grassDataOutput, 0, 0, (int)matriciesCount);
            computeChunkCopy.Dispatch(kernel, (int)matriciesCount, 1, 1);

            args[0] = _voxelWorld.WorldSettings.GrassMesh.GetIndexCount(0);
            args[1] = matriciesCount;
            args[2] = _voxelWorld.WorldSettings.GrassMesh.GetIndexStart(0);
            args[3] = _voxelWorld.WorldSettings.GrassMesh.GetBaseVertex(0);
            argsBuffer.SetData(args);
        }

        private void CreateBuffers () {
            if(argsBuffer == null)
                argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);

            if(_voxelWorld.WorldSettings.maxGrassInstance[(int)_voxelWorld.WorldSettings.quality] > 0) {
                if(grassDataBuffer == null)
                    grassDataBuffer = new ComputeBuffer(_voxelWorld.WorldSettings.maxGrassInstance[(int)_voxelWorld.WorldSettings.quality], 
                        System.Runtime.InteropServices.Marshal.SizeOf(new GrassData()));
                if(grassMatriciesBuffer == null)
                    grassMatriciesBuffer = new ComputeBuffer(_voxelWorld.WorldSettings.maxGrassInstance[(int)_voxelWorld.WorldSettings.quality],
                        System.Runtime.InteropServices.Marshal.SizeOf(new float4x4()));
                if(grassMatriciesInverseBuffer == null)
                    grassMatriciesInverseBuffer = new ComputeBuffer(_voxelWorld.WorldSettings.maxGrassInstance[(int)_voxelWorld.WorldSettings.quality],
                        System.Runtime.InteropServices.Marshal.SizeOf(new float4x4()));

                int kernel = computeChunkCopy.FindKernel("CSMain");
                computeChunkCopy.SetBuffer(kernel, "InputData", grassDataBuffer);
                computeChunkCopy.SetBuffer(kernel, "OuputMatrix", grassMatriciesBuffer);
                computeChunkCopy.SetBuffer(kernel, "OuputMatrixInverse", grassMatriciesInverseBuffer);
                if(grassEnabled) {
                    //materialChunkCopy.SetBuffer("grassDataBuffer", grassDataBuffer);
                    //materialChunkCopy.SetBuffer("matriciesBuffer", grassMatriciesBuffer);
                    //materialChunkCopy.SetBuffer("invMatriciesBuffer", grassMatriciesInverseBuffer);
                    grassMaterialProperty.SetBuffer("grassDataBuffer", grassDataBuffer);
                    grassMaterialProperty.SetBuffer("matriciesBuffer", grassMatriciesBuffer);
                    grassMaterialProperty.SetBuffer("invMatriciesBuffer", grassMatriciesInverseBuffer);
                }
            }

            // Allocating mesh buffers
            VoxelWorld.inst.VoxelDataStore.TryGetVoxelDataChunk(ChunkCoordinate, out VoxelDataVolume mainChunk);
            surroundingChunkData = VoxelWorld.inst.VoxelDataStore.GetSurroundingChunkData(ChunkCoordinate);
        }

        private void ReleaseBuffers () {
            if(_voxelWorld.WorldSettings.maxGrassInstance[(int)_voxelWorld.WorldSettings.quality] > 0) {
                if(grassDataBuffer != null)
                    grassDataBuffer.Release();
                grassDataBuffer = null;

                if(grassMatriciesBuffer != null)
                    grassMatriciesBuffer.Release();
                grassMatriciesBuffer = null;

                if(grassMatriciesInverseBuffer != null)
                    grassMatriciesInverseBuffer.Release();
                grassMatriciesInverseBuffer = null;
            }

            if(argsBuffer != null)
                argsBuffer.Release();
            argsBuffer = null;

            if(blastBuffer != null)
                blastBuffer.Release();
            blastBuffer = null;
            
            if(surroundingChunkData.IsCreated)
                surroundingChunkData.Dispose();
        }

        public int GetMeshId {
            get {
                if(_meshFilter.sharedMesh != null)
                    return _meshFilter.sharedMesh.GetInstanceID();
                else
                    return -1;
            }
        }
        
        public static string GetName (int3 chunkCoordinate) {
            return $"Chunk_{chunkCoordinate.x.ToString()}_{chunkCoordinate.y.ToString()}_{chunkCoordinate.z.ToString()}";
        }
        
        public void AddNewBlastProbe (BlastProbeData blastProbeData) {
            if(!grassEnabled) {
                return;
            }

            if(blastProbes.Count < 8) {
                blastProbes.Add(blastProbeData);
            } else {
                for(int i = 0; i < blastProbes.Count; i++) {
                    if(Time.time - blastProbes[i].explosionTime > explosionTimeout) {
                        blastProbes[i] = blastProbeData;
                        break;
                    }
                }
            }

            //materialChunkCopy.SetInt("blastProbeCount", blastProbes.Count);
            grassMaterialProperty.SetInt("blastProbeCount", blastProbes.Count);
            blastBuffer.SetData(blastProbes);
        }

        #region Marching Cube Mesher
        private const float isolevel = 0.5f;

        public void CreateMesh (VoxelDataStore voxelDataStore, int3 chunkCoordinate, Blast.Settings.Quality quality) {

            
            voxelDataStore.TryGetVoxelDataChunk(chunkCoordinate, out VoxelDataVolume mainChunk);
            NativeCounter vertexCountCounter = new NativeCounter(Allocator.TempJob);
            NativeCounter grassMatriciesCouter = new NativeCounter(Allocator.TempJob);
            int voxelCount = (mainChunk.Width - 1) * (mainChunk.Height - 1) * (mainChunk.Depth - 1);
            int maxLength = 15 * voxelCount;
            
            MarchingCubesJob marchingCubesJob = new MarchingCubesJob {
                VoxelData = mainChunk,
                _voxelDataS = surroundingChunkData,
                Isolevel = isolevel,
                MaxGrassInstance = _voxelWorld.WorldSettings.maxGrassInstance[(int)quality],
                GrassProbabilities = _voxelWorld.WorldSettings.grassProbabilities[(int)quality],
                ChunkPosition = chunkCoordinate * new float3(mainChunk.Width - 1, mainChunk.Height - 1, mainChunk.Depth - 1),
                VertexCountCounter = vertexCountCounter,
                GrassInstanceCounter = grassMatriciesCouter,

                OutputVertices = outputVertices.nativeArray,
                OutputTriangles = outputTriangles.nativeArray,
                OutputGrassData = outputGrassData.nativeArray,
            };
            JobHandle jobHandle = marchingCubesJob.Schedule(voxelCount, 128);

            JobHandleWithData<IMesherJob> mesherJobHandleWithData = new JobHandleWithData<IMesherJob>();
            mesherJobHandleWithData.JobHandle = jobHandle;
            mesherJobHandleWithData.JobData = marchingCubesJob;
            mesherJob = mesherJobHandleWithData;
        }
        #endregion
    }

    public struct BlastProbeData {
        public float3 origin;
        public float radius;
        public float explosionTime;

        public BlastProbeData (float3 origin, float radius, float explosionTime) {
            this.origin = origin;
            this.radius = radius;
            this.explosionTime = explosionTime;
        }
    };
}