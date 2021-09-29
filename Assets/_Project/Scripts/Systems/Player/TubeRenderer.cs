using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Rendering;
using Eldemarkki.VoxelTerrain.Utilities;
using System.Runtime.InteropServices;
using Unity.Mathematics;

namespace Unity.TubeRenderer {
    [RequireComponent(typeof(MeshFilter))]
    [DefaultExecutionOrder(1)]
    public class TubeRenderer : MonoBehaviour {
        [Min(0)]
        public int segments = 8;
        public Vector3[] positions;
        [Min(0)]
        public float startWidth = 1f;
        [Min(0)]
        public float endWidth = 1f;
        public Vector2 uvScale = Vector2.one;

        private MeshFilter meshFilter;
        private Mesh mesh = null;
        public NativeArray<MeshingVertexData> outputVertices;
        public NativeArray<ushort> outputTriangles;
        private JobHandleWithData<TubeJob> mesherJob;


        private void Awake () {
            meshFilter = GetComponent<MeshFilter>();
            if(mesh == null)
                mesh = new Mesh();
            meshFilter.mesh = mesh;
            
            PrepareMesh();
        }

        private void OnDestroy () {
            outputVertices.Dispose();
            outputTriangles.Dispose();
        }

        private void Update () {
            PrepareMesh();
        }

        private void LateUpdate () {
            GeneratePreparedMesh();
        }
        
        private void PrepareMesh () {
            if(mesherJob != null)
                return;

            if(!outputVertices.IsCreated || outputVertices.Length != positions.Length * segments) {
                if(outputVertices.IsCreated)
                    outputVertices.Dispose();
                if(outputTriangles.IsCreated)
                    outputTriangles.Dispose();
                outputVertices = new NativeArray<MeshingVertexData>(positions.Length * segments, Allocator.Persistent);
                outputTriangles = new NativeArray<ushort>(2 * 3 * outputVertices.Length, Allocator.Persistent);
            }

            NativeArray<float3> positionsNativeArray = new NativeArray<float3>(positions.Length, Allocator.TempJob);
            for(int i = 0; i < positions.Length; i++) {
                positionsNativeArray[i] = positions[i];
            }

            Matrix4x4 worldToLocal = transform.worldToLocalMatrix;
            Vector3 localForward = GetVertexForward(positions, 0);
            Vector3 lastLocalTop = -GravitySystem.GetGravityDirection(transform.position);
            Quaternion tubeRot = Quaternion.LookRotation(localForward, lastLocalTop);

            TubeJob tubeJob = new TubeJob() {
                worldToLocal = worldToLocal,
                lastLocalForward = localForward,
                localForward = localForward,
                lastLocalTop = lastLocalTop,
                theta = (math.PI * 2) / segments,
                width = startWidth,
                uvScale = new float4(uvScale.x, uvScale.y, 0f, 0f),

                outputTriangles = outputTriangles,
                outputVertices = outputVertices,
                positions = positionsNativeArray,

                rotation = tubeRot,
                segments = segments
            };
            JobHandle tubeJobHandle = tubeJob.Schedule();
            mesherJob = new JobHandleWithData<TubeJob>() {
                JobData = tubeJob,
                JobHandle = tubeJobHandle
            };

            #region Old
            /*float theta = (Mathf.PI * 2) / segments;

            Vector3[] verts = new Vector3[positions.Length * segments];
            Vector2[] uvs = new Vector2[verts.Length];
            Vector3[] normals = new Vector3[verts.Length];
            int[] tris = new int[2 * 3 * verts.Length];

            Vector3 localForward = GetVertexFwd(positions, 0);
            Vector3 lastLocalForward = localForward;
            Vector3 lastLocalTop = Vector3.up;
            Quaternion tubeRot = Quaternion.LookRotation(localForward, lastLocalTop);

            for(int i = 0; i < positions.Length; i++) {
                float dia = Mathf.Lerp(startWidth, endWidth, (float)i / positions.Length);

                localForward = GetVertexFwd(positions, i);
                tubeRot = Quaternion.LookRotation(localForward, lastLocalTop);
                lastLocalTop = Quaternion.FromToRotation(lastLocalForward, localForward) * lastLocalTop;
                lastLocalForward = localForward;

                for(int j = 0; j < segments; ++j) {
                    float t = theta * j;
                    Vector2 p = new Vector2(Mathf.Sin(t) * dia, Mathf.Cos(t) * dia);

                    Vector3 vert = positions[i] + (tubeRot * p);
                    int x = i * segments + j;
                    verts[x] = transform.InverseTransformPoint(vert);
                    uvs[x] = uvScale * new Vector2(t / (Mathf.PI * 2), ((float)i * this.positions.Length) / (float)subdivisions);
                    normals[x] = transform.InverseTransformVector((vert - positions[i]).normalized);
                    if(i >= positions.Length - 1)
                        continue;

                    tris[x * 6] = x + 1;
                    tris[x * 6 + 1] = x + segments;
                    tris[x * 6 + 2] = x;

                    tris[x * 6 + 3] = x + segments;
                    tris[x * 6 + 4] = x + segments - 1;
                    tris[x * 6 + 5] = x;
                }
            }
            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.normals = normals;
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;*/
            #endregion
        }

        

        private void GeneratePreparedMesh () {
            if(mesherJob == null)
                return;

            mesherJob.JobHandle.Complete();
            
            SubMeshDescriptor subMesh = new SubMeshDescriptor(0, 0);
            mesh.SetVertexBufferParams(outputVertices.Length, MeshingVertexData.VertexBufferMemoryLayout);
            mesh.SetIndexBufferParams(outputTriangles.Length, IndexFormat.UInt16);
            mesh.SetVertexBufferData(mesherJob.JobData.outputVertices, 0, 0, outputVertices.Length, 0, MeshUpdateFlags.DontValidateIndices);
            mesh.SetIndexBufferData(mesherJob.JobData.outputTriangles, 0, 0, outputTriangles.Length, MeshUpdateFlags.DontValidateIndices);
            
            mesh.subMeshCount = 1;
            subMesh.indexCount = outputTriangles.Length;
            mesh.SetSubMesh(0, subMesh);
            mesh.RecalculateBounds();

            mesherJob.JobData.positions.Dispose();

            mesherJob = null;
        }

        private Vector3 GetVertexForward (Vector3[] positions, int i) {
            if(i == positions.Length - 1) {
                return (positions[i - 1] - positions[i]).normalized;
            } else {
                return (positions[i] - positions[i + 1]).normalized;
            }
        }
    }

    [Burst.BurstCompile]
    public struct TubeJob : IJob {

        public NativeArray<float3> positions;
        public NativeArray<MeshingVertexData> outputVertices;
        public NativeArray<ushort> outputTriangles;
        public float3 localForward;
        public float3 lastLocalForward;
        public float3 lastLocalTop;
        public float4 uvScale;
        public quaternion rotation;
        public float4x4 worldToLocal;
        public int segments;
        public float width;
        public float theta;

        public void Execute () {
            for(int i = 0; i < positions.Length; i++) {
                localForward = GetVertexForward(i);
                rotation = quaternion.LookRotationSafe(localForward, lastLocalTop);
                lastLocalTop = math.mul(Quaternion.FromToRotation(lastLocalForward, localForward), lastLocalTop);
                lastLocalForward = localForward;

                for(int j = 0; j < segments; j++) {
                    float t = theta * j;
                    float2 p = new float2(math.sin(t) * width, math.cos(t) * width);

                    float3 vert = positions[i] + math.mul(rotation, new float3(p.x, p.y, 0f));
                    int x = i * segments + j;
                    outputVertices[x] = new MeshingVertexData(
                        math.transform(worldToLocal, vert), 
                        math.rotate(worldToLocal, math.normalizesafe(vert - positions[i])), 
                        uvScale * new float4(t / (math.PI * 2), (float)i * positions.Length, 0f, 0f));

                    if(i >= positions.Length - 1)
                        continue;

                    outputTriangles[x * 6] = (ushort)(x + 1);
                    outputTriangles[x * 6 + 1] = (ushort)(x + segments);
                    outputTriangles[x * 6 + 2] = (ushort)x;

                    outputTriangles[x * 6 + 3] = (ushort)(x + segments);
                    outputTriangles[x * 6 + 4] = (ushort)(x + segments - 1);
                    outputTriangles[x * 6 + 5] = (ushort)x;
                }
            }
        }

        private float3 GetVertexForward (int i) {
            if(i == positions.Length - 1) {
                return math.normalizesafe(positions[i - 1] - positions[i]);
            } else {
                return math.normalizesafe(positions[i] - positions[i + 1]);
            }
        }
    }

    /// <summary>
    /// A struct to hold the data every vertex should have
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct MeshingVertexData {

        public float3 position;
        public float3 normal;
        public float4 uvs;
        
        public MeshingVertexData (float3 position, float3 normal, float4 uvs) {
            this.position = position;
            this.normal = normal;
            this.uvs = uvs;
        }

        public static readonly VertexAttributeDescriptor[] VertexBufferMemoryLayout = {
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
            new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 4)
        };
    }
}