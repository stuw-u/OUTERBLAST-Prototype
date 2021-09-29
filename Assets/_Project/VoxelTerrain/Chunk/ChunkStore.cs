using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

using Eldemarkki.VoxelTerrain.Utilities;
using Eldemarkki.VoxelTerrain.Utilities.Intersection;
using Eldemarkki.VoxelTerrain.World;
using Eldemarkki.VoxelTerrain.World.Chunks;

namespace Eldemarkki.VoxelTerrain.World.Chunks
{
    /// <summary>
    /// A container for all of the chunks in the world
    /// </summary>
    public class ChunkStore : MonoBehaviour {
        /// A dictionary of all the chunks currently in the world. The key is the chunk's coordinate, and the value is the chunk
        private Dictionary<int3, Chunk> _chunks;

        public void Init () {
            _chunks = new Dictionary<int3, Chunk>();
        }

        private void Update () {
            foreach(KeyValuePair<int3, Chunk> chunk in _chunks) {
                chunk.Value.ManualUpdate();
            }
        }

        private void LateUpdate () {
            foreach(KeyValuePair<int3, Chunk> chunk in _chunks) {
                chunk.Value.ManualLateUpdate();
            }
        }

        /// <summary>
        /// Gets whether or not a chunk exists at a coordinate
        /// </summary>
        /// <param name="chunkCoordinate">The coordinate of the chunk to check</param>
        /// <returns>Is there a chunk at the coordinate</returns>
        public bool DoesChunkExistAtCoordinate(int3 chunkCoordinate)
        {
            return _chunks.ContainsKey(chunkCoordinate);
        }

        /// <summary>
        /// Tries to get a chunk from a coordinate, if it finds one it returns true and sets chunk to the found chunk, otherwise it returns false and sets chunk to null
        /// </summary>
        /// <param name="chunkCoordinate">The chunk's coordinate</param>
        /// <param name="chunk">The chunk that was found. If none was found, it is null</param>
        /// <returns>Is there a chunk at the coordinate</returns>
        public bool TryGetChunkAtCoordinate(int3 chunkCoordinate, out Chunk chunk)
        {
            return _chunks.TryGetValue(chunkCoordinate, out chunk);
        }

        /// <summary>
        /// Gets a collection of chunk coordinates whose Manhattan Distance to the coordinate parameter is more than <see cref="renderDistance"/>
        /// </summary>
        /// <param name="coordinate">Central coordinate</param>
        /// <param name="renderDistance">The radius of the chunks the player can see</param>
        /// <returns>A collection of chunk coordinates outside of the viewing range from the coordinate parameter</returns>
        public IEnumerable<int3> GetChunkCoordinatesOutsideOfRenderDistance(int3 coordinate, int renderDistance)
        {
            foreach (int3 chunkCoordinate in _chunks.Keys.ToList())
            {
                int dX = math.abs(coordinate.x - chunkCoordinate.x);
                int dY = math.abs(coordinate.y - chunkCoordinate.y);
                int dZ = math.abs(coordinate.z - chunkCoordinate.z);

                if (dX > renderDistance || dY > renderDistance || dZ > renderDistance)
                {
                    yield return chunkCoordinate;
                }
            }
        }

        /// <summary>
        /// Adds a chunk to the chunk store
        /// </summary>
        /// <param name="chunk">The chunk to add</param>
        public void AddChunk(Chunk chunk)
        {
            if (!_chunks.ContainsKey(chunk.ChunkCoordinate))
            {
                _chunks.Add(chunk.ChunkCoordinate, chunk);
            }
        }

        /// <summary>
        /// Removes a chunk from a coordinate
        /// </summary>
        /// <param name="chunkCoordinate">The coordinate of the chunk to remove</param>
        public void RemoveChunk(int3 chunkCoordinate)
        {
            _chunks.Remove(chunkCoordinate);
        }

        /// <summary>
        /// Removes a chunk from a coordinate and destroys its GameObject
        /// </summary>
        /// <param name="chunkCoordinate">The coordinate of the chunk to remove and destroy</param>
        public void DestroyChunk(int3 chunkCoordinate)
        {
            if (TryGetChunkAtCoordinate(chunkCoordinate, out Chunk chunk))
            {
                Destroy(chunk.gameObject);
                RemoveChunk(chunkCoordinate);
            }
        }

        /// <summary>
        /// Gets a collection of chunks that contain a world position. For a chunk to contain a position, the position has to be inside of the chunk or on the chunk's edge
        /// </summary>
        /// <param name="worldPosition">The world position to check</param>
        /// <param name="chunkSize">The size of the chunks</param>
        /// <returns>A collection of chunk coordinates that contain the world position</returns>
        public static IEnumerable<int3> GetChunkCoordinatesContainingPoint (int3 worldPosition, int chunkSize) {
            int3 localPosition = VectorUtilities.Mod(worldPosition, chunkSize);

            int chunkCheckCountX = localPosition.x == 0 ? 1 : 0;
            int chunkCheckCountY = localPosition.y == 0 ? 1 : 0;
            int chunkCheckCountZ = localPosition.z == 0 ? 1 : 0;

            int3 origin = VectorUtilities.WorldPositionToCoordinate(worldPosition, chunkSize);

            // The origin (worldPosition as a chunk coordinate) is always included
            yield return origin;

            // The first corner can be skipped, since it's (0, 0, 0) and would just return origin
            for(int i = 1; i < 8; i++) {
                var cornerOffset = LookupTables.CubeCorners[i];
                if(cornerOffset.x <= chunkCheckCountX && cornerOffset.y <= chunkCheckCountY && cornerOffset.z <= chunkCheckCountZ) {
                    yield return origin - cornerOffset;
                }
            }
        }
    }
}