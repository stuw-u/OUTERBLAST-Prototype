using System;
using UnityEngine;

namespace Eldemarkki.VoxelTerrain.Settings
{
    /// <summary>
    /// The procedural terrain generation settings
    /// </summary>
    [Serializable]
    public struct ProceduralTerrainSettings
    {
        /// <summary>
        /// The frequency of the noise
        /// </summary>
        [SerializeField] private float noiseFrequency;
        
        /// <summary>
        /// How many octaves the noise will have
        /// </summary>
        [SerializeField] private int noiseOctaveCount;
        
        /// <summary>
        /// The height multiplier
        /// </summary>
        [SerializeField] private float amplitude;
        
        /// <summary>
        /// Moves the height up and down
        /// </summary>
        [SerializeField] private float heightOffset;

        /// <summary>
        /// The frequency of the noise
        /// </summary>
        public float NoiseFrequency { get => noiseFrequency; set => noiseFrequency = value; }
        
        /// <summary>
        /// How many octaves the noise will have
        /// </summary>
        public int NoiseOctaveCount { get => noiseOctaveCount; set => noiseOctaveCount = value; }
        
        /// <summary>
        /// The height multiplier
        /// </summary>
        public float Amplitude { get => amplitude; set => amplitude = value; }
        
        /// <summary>
        /// Moves the height up and down
        /// </summary>
        public float HeightOffset { get => heightOffset; set => heightOffset = value; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="noiseFrequency">The frequency of the noise</param>
        /// <param name="noiseOctaveCount">How many octaves the noise will have</param>
        /// <param name="amplitude">The height multiplier</param>
        /// <param name="heightOffset">Moves the height up and down</param>
        public ProceduralTerrainSettings(float noiseFrequency, int noiseOctaveCount, float amplitude, float heightOffset)
        {
            this.noiseFrequency = noiseFrequency;
            this.noiseOctaveCount = noiseOctaveCount;
            this.amplitude = amplitude;
            this.heightOffset = heightOffset;
        }
    }

    /// <summary>
    /// The procedural island generation settings
    /// </summary>
    [Serializable]
    public struct ProceduralIslandSettings {
        /// <summary>
        /// The frequency of the noise
        /// </summary>
        [SerializeField] private float noiseFrequency;

        /// <summary>
        /// How many octaves the noise will have
        /// </summary>
        [SerializeField] private int noiseOctaveCount;

        /// <summary>
        /// The height multiplier
        /// </summary>
        [SerializeField] private float amplitude;
        
        [SerializeField] private float islandTop;
        
        [SerializeField] private float islandBottom;
        
        [SerializeField] private float islandRadius;

        /// <summary>
        /// The frequency of the noise
        /// </summary>
        public float NoiseFrequency {
            get => noiseFrequency; set => noiseFrequency = value;
        }

        /// <summary>
        /// How many octaves the noise will have
        /// </summary>
        public int NoiseOctaveCount {
            get => noiseOctaveCount; set => noiseOctaveCount = value;
        }

        /// <summary>
        /// Top layer of the island
        /// </summary>
        public float IslandTop {
            get => islandTop; set => islandTop = value;
        }

        /// <summary>
        /// Bottom layer of the island
        /// </summary>
        public float IslandBottom {
            get => islandBottom; set => islandBottom = value;
        }

        /// <summary>
        /// The height multiplier
        /// </summary>
        public float Amplitude {
            get => amplitude; set => amplitude = value;
        }

        public float IslandRadius {
            get => islandRadius; set => islandRadius = value;
        }



        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="noiseFrequency">The frequency of the noise</param>
        /// <param name="noiseOctaveCount">How many octaves the noise will have</param>
        /// <param name="amplitude">The height multiplier</param>
        public ProceduralIslandSettings (float noiseFrequency, int noiseOctaveCount, float amplitude, float islandTop, float islandBottom, float islandRadius) {
            this.noiseFrequency = noiseFrequency;
            this.noiseOctaveCount = noiseOctaveCount;
            this.amplitude = amplitude;
            this.islandTop = islandTop;
            this.islandBottom = islandBottom;
            this.islandRadius = islandRadius;
        }
    }
}