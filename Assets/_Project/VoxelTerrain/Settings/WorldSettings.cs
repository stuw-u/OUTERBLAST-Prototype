using Eldemarkki.VoxelTerrain.World.Chunks;
using UnityEngine;

namespace Eldemarkki.VoxelTerrain.Settings
{
    /// <summary>
    /// Parameters that specify how the world will be generated
    /// </summary>
    [System.Serializable]
    public class WorldSettings
    {
        public Blast.Settings.Quality quality;

        /// <summary>
        /// The chunk's size. This represents the width, height and depth in Unity units.
        /// </summary>
        [SerializeField] private int chunkSize = 16;

        /// <summary>
        /// The chunk's prefab that will be instantiated
        /// </summary>
        [SerializeField] private Chunk chunkPrefab;

        /// <summary>
        /// The grass mesh that will be drawn all over the surface
        /// </summary>
        [SerializeField] private Mesh grassMesh;

        /// <summary>
        /// The grass material
        /// </summary>
        [SerializeField] private Material grassMaterial;

        /// <summary>
        /// The compute shader that will convert grass data to matricies that can be used in the grass shader
        /// </summary>
        [SerializeField] private ComputeShader grassDataToMatrixCompute;


        public int[] maxGrassInstance = new int[] { 0, 20000, 20000, 40000 };
        public float[] grassProbabilities = new float[] { 0f, 0.25f, 0.5f, 1f };


        /// <summary>
        /// The chunk's size. This represents the width, height and depth in Unity units.
        /// </summary>
        public int ChunkSize => chunkSize;

        /// <summary>
        /// The chunk's prefab that will be instantiated
        /// </summary>
        public Chunk ChunkPrefab => chunkPrefab;

        /// <summary>
        /// The grass mesh that will be drawn all over the surface
        /// </summary>
        public Mesh GrassMesh => grassMesh;

        /// <summary>
        /// The grass material
        /// </summary>
        public Material GrassMaterial => grassMaterial;

        /// <summary>
        /// The compute shader that will convert grass data to matricies that can be used in the grass shader
        /// </summary>
        public ComputeShader GrassDataToMatrixCompute => grassDataToMatrixCompute;
    }
}