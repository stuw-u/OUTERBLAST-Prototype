using Eldemarkki.VoxelTerrain.Meshing.Data;
using Eldemarkki.VoxelTerrain.VoxelData;
using Eldemarkki.VoxelTerrain.Utilities;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using System.Runtime.CompilerServices;

namespace Eldemarkki.VoxelTerrain.Meshing.MarchingCubes
{
    /// <summary>
    /// A marching cubes mesh generation job
    /// </summary>
    [BurstCompile]
    public struct MarchingCubesJob : IMesherJob {

        [ReadOnly] private VoxelDataVolume _voxelData;
        [ReadOnly] public NativeHashMap<int3, BlitableArray<uint>> _voxelDataS;

        public float Isolevel { get; set; }
        public int MaxGrassInstance { get; set; }
        public float GrassProbabilities { get; set; }
        public float3 ChunkPosition { get; set; }

        public NativeCounter VertexCountCounter { get; set; } // The counter to keep track of the triangle index
        public NativeCounter GrassInstanceCounter { get; set; } // The counter to keep track of the triangle index

        [NativeDisableParallelForRestriction, WriteOnly] private NativeArray<MeshingVertexData> _vertices; // The generated vertices
        [NativeDisableParallelForRestriction, WriteOnly] private NativeArray<ushort> _triangles; // The generated triangles
        [NativeDisableParallelForRestriction, WriteOnly] private NativeArray<GrassData> _grassData; // The generated grass data
        public VoxelDataVolume VoxelData { get => _voxelData; set => _voxelData = value; } // The voxel data to generate the mesh from
        public NativeArray<MeshingVertexData> OutputVertices { get => _vertices; set => _vertices = value; } // The generated vertices
        public NativeArray<ushort> OutputTriangles { get => _triangles; set => _triangles = value; } // The generated triangles
        public NativeArray<GrassData> OutputGrassData { get => _grassData; set => _grassData = value; } // The generated grass data

        /// <summary>
        /// The execute method required by the Unity Job System's IJobParallelFor
        /// </summary>
        /// <param name="index">The iteration index</param>
        public void Execute(int index)
        {
            // The position of the voxel Voxel inside the chunk. Goes from (0, 0, 0) to (densityVolume.Width-1, densityVolume.Height-1, densityVolume.Depth-1). Both are inclusive.
            int3 voxelLocalPosition = IndexUtilities.IndexToXyz(index, _voxelData.Width - 1, _voxelData.Height - 1);
            int3 worldPos = voxelLocalPosition + (int3)ChunkPosition;

            VoxelCorners<float> densities = _voxelData.GetVoxelDataUnitCube(voxelLocalPosition, 0);
            VoxelCorners<float> burntDensities = _voxelData.GetVoxelDataUnitCube(voxelLocalPosition, 1);
            VoxelCorners<float> brokenDensities = _voxelData.GetVoxelDataUnitCube(voxelLocalPosition, 2);

            byte cubeIndex = MarchingCubesFunctions.CalculateCubeIndex(densities, Isolevel);
            if (cubeIndex == 0 || cubeIndex == 255)
            {
                return;
            }

            VoxelCorners<int3> corners = MarchingCubesFunctions.GetCorners(voxelLocalPosition);

            int edgeIndex = MarchingCubesLookupTables.EdgeTable[cubeIndex];

            MarchingCubesFunctions.GenerateVertexList(densities, burntDensities, brokenDensities, corners, edgeIndex, Isolevel, 
                out VertexList<float3> vertexList, out VertexList<float> burntList, out VertexList<float> brokenList);

            // Index at the beginning of the row
            int rowIndex = 15 * cubeIndex;
            float3 up = new float3(0f, 1f, 0f);

            Random rnd = Random.CreateFromIndex((uint)(index));

            for (int i = 0; MarchingCubesLookupTables.TriangleTable[rowIndex+i] != -1 && i < 15; i += 3)
            {
                int triangleIndex0 = MarchingCubesLookupTables.TriangleTable[rowIndex + i + 0];
                int triangleIndex1 = MarchingCubesLookupTables.TriangleTable[rowIndex + i + 1];
                int triangleIndex2 = MarchingCubesLookupTables.TriangleTable[rowIndex + i + 2];
                float3 vertex1 = vertexList[triangleIndex0];
                float3 vertex2 = vertexList[triangleIndex1];
                float3 vertex3 = vertexList[triangleIndex2];
                float3 nA = sampleNormal(vertex1);
                float3 nB = sampleNormal(vertex2);
                float3 nC = sampleNormal(vertex3);
                float burntValue1 = burntList[triangleIndex0];
                float burntValue2 = burntList[triangleIndex1];
                float burntValue3 = burntList[triangleIndex2];
                float brokenValue1 = brokenList[triangleIndex0];
                float brokenValue2 = brokenList[triangleIndex1];
                float brokenValue3 = brokenList[triangleIndex2];
                float avgBurntValue = (burntValue1 + burntValue2 + burntValue3) / 3f;
                float avgBrokenValue = (brokenValue1 + brokenValue2 + brokenValue3) / 3f;

                if (!vertex1.Equals(vertex2) && !vertex1.Equals(vertex3) && !vertex2.Equals(vertex3))
                {
                    float3 flatForwardNormal = math.normalize(vertex2 - vertex1);
                    float3 flatNormal = math.normalize(math.cross(vertex2 - vertex1, vertex3 - vertex1));

                    if(GrassProbabilities > 0f) {
                        #region Apply Grass
                        // Apply grass
                        float dot = math.dot(flatNormal, up);
                        if(avgBrokenValue < 0.5f && dot > 0.6f) {

                            float trisArea = GetTriangleArea(vertex1, vertex2, vertex3);
                            int bladeCount = (int)math.round(trisArea * rnd.NextInt(30, 50) * GrassProbabilities * math.saturate(math.unlerp(-20f, -10f, worldPos.y)));

                            for(int blade = 0; blade < bladeCount; blade++) {
                                float3 pos = GetPointInTriangle(
                                    rnd, 
                                    vertex1, vertex2, vertex3, 
                                    nA, nB, nC, burntValue1, burntValue2, burntValue3, 
                                    out float3 grassNormal, 
                                    out float grassBurntValue) + ChunkPosition;
                                dot = math.dot(grassNormal, up);
                                if(dot < 0.6f) {
                                    continue;
                                }
                                int grassInstanceIndex = GrassInstanceCounter.Increment();

                                if(grassInstanceIndex <= _grassData.Length) {
                                    _grassData[grassInstanceIndex] = new GrassData(
                                        new float4(pos, rnd.NextFloat(0.6f, 1.3f) * math.saturate(math.unlerp(0.4f, 0.9f, dot))),
                                        math.mul(quaternion.LookRotation(flatForwardNormal, grassNormal), quaternion.RotateY(rnd.NextFloat(0f, 2 * math.PI))).value,
                                        grassBurntValue
                                     );
                                }
                            }
                        }
                        #endregion
                    }

                    int triangleIndex = VertexCountCounter.Increment() * 3;
                    
                    _vertices[triangleIndex + 0] = new MeshingVertexData(vertex1, nA, new float4(burntValue1, brokenValue1, 0, 0));
                    _triangles[triangleIndex + 0] = (ushort)(triangleIndex + 0);

                    _vertices[triangleIndex + 1] = new MeshingVertexData(vertex2, nB, new float4(burntValue2, brokenValue2, 0, 0));
                    _triangles[triangleIndex + 1] = (ushort)(triangleIndex + 1);

                    _vertices[triangleIndex + 2] = new MeshingVertexData(vertex3, nC, new float4(burntValue3, brokenValue3, 0, 0));
                    _triangles[triangleIndex + 2] = (ushort)(triangleIndex + 2);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float3 GetPointInTriangle (Random rand, 
            float3 a, float3 b, float3 c, 
            float3 nA, float3 nB, float3 nC,
            float burntA, float burntB, float burntC,
            out float3 normal,
            out float burntValue) {

            float3 ab = b - a;
            float3 ac = c - a;

            float x = rand.NextFloat();       //  % along ab
            float y = rand.NextFloat();       //  % along ac

            if(x + y >= 1f) {
                x = 1f - x;
                y = 1f - y;
            }

            normal = math.lerp(nA, math.lerp(nB, nC, y), x);
            burntValue = math.lerp(burntA, math.lerp(burntB, burntC, y), x);
            return a + ((ab * x) + (ac * y));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float GetTriangleArea (float3 a, float3 b, float3 c) {
            float3 e1 = b - a;
            float3 e2 = c - a;
            float3 e3 = math.cross(e1, e2);

            return 0.5f * math.sqrt(e3.x * e3.x + e3.y * e3.y + e3.z * e3.z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float3 sampleNormal (float3 uv) {
            return -math.normalizesafe(sampleGradient(uv, 0.1f));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float3 sampleGradient (float3 uv, float eps) {
            return new float3(
                sampleGridValue(uv + new float3(eps, 0, 0)) - sampleGridValue(uv - new float3(eps, 0, 0)),
                sampleGridValue(uv + new float3(0, eps, 0)) - sampleGridValue(uv - new float3(0, eps, 0)),
                sampleGridValue(uv + new float3(0, 0, eps)) - sampleGridValue(uv - new float3(0, 0, eps))
            );
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float sampleGridValue (float3 p) {
            //p += new float3(1f, 1f, 1f);
            int3 chunkSize = _voxelData.Size + new int3(1, 1, 1);
            float3 pFrac = math.frac(p);
            int3 pFlr = (int3)math.floor(p);
            int3 pCei = new int3(pFlr.x + 1, pFlr.y + 1, pFlr.z + 1);

            float value_000 = getGridValue(pFlr.x, pFlr.y, pFlr.z);
            float value_100 = getGridValue(pCei.x, pFlr.y, pFlr.z);
            float value_010 = getGridValue(pFlr.x, pCei.y, pFlr.z);
            float value_110 = getGridValue(pCei.x, pCei.y, pFlr.z);
            float value_001 = getGridValue(pFlr.x, pFlr.y, pCei.z);
            float value_101 = getGridValue(pCei.x, pFlr.y, pCei.z);
            float value_011 = getGridValue(pFlr.x, pCei.y, pCei.z);
            float value_111 = getGridValue(pCei.x, pCei.y, pCei.z);

            return valueTrilinear(
                    value_000, value_100,
                    value_010, value_110,
                    value_001, value_101,
                    value_011, value_111,
                    pFrac.x, pFrac.y, pFrac.z);
        }

        private float getGridValue (int x, int y, int z) {
            int3 c = new int3(
                (int)math.floor((float)x / _voxelData.Width),
                (int)math.floor((float)y / _voxelData.Height),
                (int)math.floor((float)z / _voxelData.Depth));
            if(_voxelDataS.TryGetValue(c, out BlitableArray<uint> data)) {
                return Extract(data[IndexUtilities.XyzToIndex(
                    x - c.x * (_voxelData.Width - 1),
                    y - c.y * (_voxelData.Height - 1),
                    z - c.z * (_voxelData.Depth - 1),
                     _voxelData.Width, _voxelData.Height)], 0) * 0.00392156862f;
            } else {
                return 0f;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte Extract (ulong data, byte index) {
            return (byte)((data >> (index * 8)) & 0xFF);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float valueBilinear (float v1, float v2, float v3, float v4, float x, float y) {
            float s = math.lerp(v1, v2, x);
            float t = math.lerp(v3, v4, x);
            return math.lerp(s, t, y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float valueTrilinear (float v1, float v2, float v3, float v4, float v5, float v6, float v7, float v8, float x, float y, float z) {
            float s = valueBilinear(v1, v2, v3, v4, x, y);
            float t = valueBilinear(v5, v6, v7, v8, x, y);
            return math.lerp(s, t, z);
        }
    }
}