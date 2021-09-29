using System.Runtime.CompilerServices;
using Eldemarkki.VoxelTerrain.Settings;
using Eldemarkki.VoxelTerrain.Utilities;
using Unity.Burst;
using Unity.Mathematics;

namespace Eldemarkki.VoxelTerrain.VoxelData
{
    /// <summary>
    /// A procedural terrain voxel data calculation job
    /// </summary>
    [BurstCompile]
    public struct IslandTerrainVoxelDataCalculationJob : IVoxelDataGenerationJob
    {
        /// <summary>
        /// The procedural terrain generation settings
        /// </summary>
        public ProceduralIslandSettings ProceduralIslandSettings { get; set; }

        /// <summary>
        /// The sampling point's world position offset
        /// </summary>
        public int3 worldPositionOffset { get; set; }

        public int seed { get; set; }

        /// <summary>
        /// The generated voxel data
        /// </summary>
        public VoxelDataVolume outputVoxelData { get; set; }

        public float4x4 Matrix { get; set; }

        /// <summary>
        /// The execute method required for Unity's IJobParallelFor job type
        /// </summary>
        /// <param name="index">The iteration index provided by Unity's Job System</param>
        public void Execute(int index)
        {
            int3 worldPosition = IndexUtilities.IndexToXyz(index, outputVoxelData.Width, outputVoxelData.Height) + worldPositionOffset;
            int worldPositionX = worldPosition.x;
            int worldPositionY = worldPosition.y;
            int worldPositionZ = worldPosition.z;

            float voxelData = CalculateVoxelData(worldPositionX, worldPositionY, worldPositionZ);
            outputVoxelData.SetVoxelData(voxelData, index, 0);
        }

        /// <summary>
        /// Calculates the voxel data at the world-space position
        /// </summary>
        /// <param name="worldPositionX">Sampling point's world-space x position</param>
        /// <param name="worldPositionY">Sampling point's world-space y position</param>
        /// <param name="worldPositionZ">Sampling point's world-space z position</param>
        /// <returns>The voxel data sampled from the world-space position</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float CalculateVoxelData(int worldPositionX, int worldPositionY, int worldPositionZ) {

            
            float3 worldPosition = math.transform(Matrix, new float3(worldPositionX, worldPositionY, worldPositionZ));
            
            // Preparing and sampeling height-relative value
            float baseSoftNoise = OctaveNoise(worldPosition.x, worldPosition.y, worldPosition.z, ProceduralIslandSettings.NoiseFrequency, ProceduralIslandSettings.NoiseOctaveCount, seed);
            float topToBottomValue = math.saturate(math.unlerp(ProceduralIslandSettings.IslandBottom, ProceduralIslandSettings.IslandTop, worldPosition.y));
            float topToUpperRegionValue = math.select(0.8f, math.lerp(0.6f, 0.3f, math.saturate(math.unlerp(ProceduralIslandSettings.IslandTop, ProceduralIslandSettings.IslandTop+16, worldPosition.y))), worldPosition.y > ProceduralIslandSettings.IslandTop);
            float radiusAtHeight = math.lerp(0, topToBottomValue, ProceduralIslandSettings.IslandRadius);
            float inlerpUnder = math.saturate(math.unlerp(ProceduralIslandSettings.IslandBottom - 8, ProceduralIslandSettings.IslandTop, worldPosition.y));
            float radiusUnder = math.lerp(0, inlerpUnder, ProceduralIslandSettings.IslandRadius);
            
            // The distance from center line to a point in the XZ plane
            float cylinderDistance = math.sqrt(worldPosition.x * worldPosition.x + worldPosition.z * worldPosition.z);

            // Composing a soft cone shape
            float softConeShape = cylinderDistance;
            float softUnderConeShape = softConeShape;
            softConeShape /= radiusAtHeight;
            softUnderConeShape /= radiusAtHeight;
            softConeShape = 1f - softConeShape;
            softUnderConeShape = 1f - softUnderConeShape;
            softConeShape = math.max(softConeShape, 0);
            softConeShape = math.min(softConeShape, 0.7f);
            softUnderConeShape = math.max(softUnderConeShape, 0);

            // Merging noise and shape
            float softShape = (softConeShape * topToUpperRegionValue);
            float total = (baseSoftNoise * softShape) + softShape * 0.2f;

            // Test feature
            float archCountX = 12f; //12
            float archCountY = 3f; //3
            float coliseumMin = 10f;
            float coliseumMax = 50f;
            float coliseumRadius = 30f;
            float coliseumWidth = 4f; //4
            float coliseumInvWidth = 0.25f;

            float coliseumShape = math.saturate((coliseumWidth - math.abs(coliseumRadius - cylinderDistance)) * math.select(0f, 1f, worldPosition.y > coliseumMin && worldPosition.y < coliseumMax) * coliseumInvWidth);
            float coliseumXCoords = nfmod((math.atan2(worldPosition.x, worldPosition.z) * 0.15915494309f) * archCountX);
            float coliseumYCoords = nfmod(math.saturate(math.unlerp(coliseumMin, coliseumMax, worldPosition.y + 2f)) * archCountY);
            coliseumShape -= ArchCarve((coliseumXCoords * 2f - 1f) * 0.6f, (coliseumYCoords * 2f - 1f) * -0.6f, 0f, 2f);
            //total = math.max(total, coliseumShape);

            // Debug sphere
            float circle2 = math.sqrt(worldPosition.x * worldPosition.x + worldPosition.y * worldPosition.y + worldPosition.z * worldPosition.z) / (ProceduralIslandSettings.IslandRadius * 0.4f);
            circle2 = 1f - circle2;
            float total2 = ChangeSteepness(circle2 + baseSoftNoise * 0.1f, ProceduralIslandSettings.Amplitude);

            float voxelData = ChangeSteepness(total, ProceduralIslandSettings.Amplitude);
            return total;
            //return total2;
        }

        /// <summary>
        /// Calculates octave noise
        /// </summary>
        /// <param name="x">Sampling point's x position</param>
        /// <param name="y">Sampling point's y position</param>
        /// <param name="frequency">The frequency of the noise</param>
        /// <param name="octaveCount">How many layers of noise to combine</param>
        /// <returns>The sampled noise value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float OctaveNoise(float x, float y, float z, float frequency, int octaveCount, int seed)
        {
            float value = 0;
            float amp = 1f;
            float freq = frequency;

            for (int i = 0; i < octaveCount; i++)
            {
                // (x+1)/0.5 because noise.snoise returns a value from -1 to 1 so it needs to be scaled to go from 0 to 1.
                value += ((noise.snoise(new float4(x * freq, y * freq, z * freq, seed * 0.39f + 0.02f)) + 1) * 0.5f) * amp;
                freq *= 2f;
                amp *= 0.5f;
            }

            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float ChangeSteepness (float x, float s) {
            return s * (x + 0.5f) - s + 0.5f;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float ArchCarve (float posX, float posY, float height, float radius) {
            float2 lockedPos = new float2(posX, math.min(posY + height, 0f));
            return math.select(0f, math.saturate(1f-(math.length(lockedPos) * radius)), posY < 0.5f);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        float nfmod (float a) {
            return a - math.floor(a);
        }
    }
}

// ROBLOX PROTOTYPE NOISE PORT
/*

    float x = worldPositionX;
            float y = worldPositionY;
            float z = worldPositionZ;

            float noiseV = math.clamp(noise.snoise(
                new float3(
                    x * 0.12f,
                    y * 0.12f,
                    z * 0.12f
                )
            ) + 0.5f, 0, 1) * 0.7f;
            float detailNoise = math.clamp(noise.snoise(
                new float3(
                    y * 0.3f,
                    x * 0.3f,
                    z * 0.3f
                )
            ) + 0.5f, 0, 1) * 0.3f;

            noiseV += detailNoise;
            noiseV = math.clamp(noiseV, 0, 1);

            float cutoff = (y / 64f) * 2f;

            if(cutoff > 1)
                cutoff = math.min(0, (2 - cutoff) * 2);
            else
                cutoff = 1f;

            float cyl_rad = 32f * (math.max(0, y - 4) / 64f) * 2;

            float2 pos = new float2(x - 32f, z - 32f);
            float len = math.sqrt(pos.x * pos.x + pos.y * pos.y);

            float cyl_shape = math.max(0, (1 - (len / cyl_rad)) * 8f);
            float sum_shape = (math.min(cyl_shape, 1) * cutoff);


            float sum = (noiseV * sum_shape) + sum_shape * 0.2f;

            return math.clamp(sum, 0, 1);
 
*/
