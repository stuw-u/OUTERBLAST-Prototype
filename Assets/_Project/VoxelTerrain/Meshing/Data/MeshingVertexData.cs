using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine.Rendering;

namespace Eldemarkki.VoxelTerrain.Meshing.Data
{
    /// <summary>
    /// A struct to hold the data every vertex should have
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct MeshingVertexData
    {
        /// <summary>
        /// The vertex's local position
        /// </summary>
        public float3 position;

        /// <summary>
        /// The vertex's normal
        /// </summary>
        public float3 normal;

        /// <summary>
        /// The vertex's uv coordinates
        /// </summary>
        public float4 uvs;

        /// <summary>
        /// The constructor to create a <see cref="MeshingVertexData"/>
        /// </summary>
        /// <param name="position">The vertex's local position</param>
        /// <param name="normal">The vertex's normal</param>
        public MeshingVertexData(float3 position, float3 normal, float4 uvs)
        {
            this.position = position;
            this.normal = normal;
            this.uvs = uvs;
        }

        /// <summary>
        /// The memory layout of a single vertex in memory
        /// </summary>
        public static readonly VertexAttributeDescriptor[] VertexBufferMemoryLayout =
        {
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
            new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 4)
        };
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct GrassData {
        public float4 rootPosScale;
        public float4 rootRot;
        public float burntValue;

        public GrassData (float4 rootPosScale, float4 rootRot, float burntValue) {
            this.rootPosScale = rootPosScale;
            this.rootRot = rootRot;
            this.burntValue = burntValue;
        }
    };
}