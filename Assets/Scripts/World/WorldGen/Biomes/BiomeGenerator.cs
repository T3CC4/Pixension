using UnityEngine;

namespace Pixension.WorldGen.Biomes
{
    /// <summary>
    /// Generates biome distribution across the world using temperature and humidity noise
    /// Similar to Minecraft's biome generation system
    /// </summary>
    public class BiomeGenerator
    {
        private NoiseGenerator noise;
        private BiomeRegistry biomeRegistry;
        private int seed;

        // Noise scales for climate parameters (larger = bigger biomes)
        private float temperatureScale = 800f;  // Can be adjusted for bigger/smaller biomes
        private float humidityScale = 600f;
        private float continentalScale = 2000f;  // For ocean/land distinction
        private float erosionScale = 400f;       // For terrain variation

        public BiomeGenerator(int seed)
        {
            this.seed = seed;
            this.noise = new NoiseGenerator(seed);
            this.biomeRegistry = BiomeRegistry.Instance;
        }

        /// <summary>
        /// Gets the biome at a specific world position
        /// </summary>
        public BiomeData GetBiome(int worldX, int worldZ)
        {
            // Get climate parameters using noise
            float temperature = GetTemperature(worldX, worldZ);
            float humidity = GetHumidity(worldX, worldZ);
            float continental = GetContinentalness(worldX, worldZ);
            float erosion = GetErosion(worldX, worldZ);

            // Select biome based on climate parameters
            BiomeType biomeType = SelectBiome(temperature, humidity, continental, erosion);

            return biomeRegistry.GetBiome(biomeType);
        }

        /// <summary>
        /// Gets temperature at a position (0 = frozen, 1 = hot)
        /// Uses multiple noise layers for natural variation
        /// </summary>
        public float GetTemperature(int worldX, int worldZ)
        {
            // Base temperature from large-scale noise
            float temp = noise.Get2DNoise(worldX, worldZ, temperatureScale, 3, 0.5f, NoiseType.Simplex);
            temp = (temp + 1f) * 0.5f; // Convert from -1..1 to 0..1

            // Add altitude-based temperature reduction (higher = colder)
            // This is calculated elsewhere in terrain generation

            // Add equator-based temperature (optional - creates latitude-like bands)
            // float latitude = Mathf.Sin(worldZ / 1000f) * 0.2f;
            // temp += latitude;

            return Mathf.Clamp01(temp);
        }

        /// <summary>
        /// Gets humidity at a position (0 = dry, 1 = wet)
        /// </summary>
        public float GetHumidity(int worldX, int worldZ)
        {
            float humidity = noise.Get2DNoise(worldX + 5000, worldZ + 5000, humidityScale, 3, 0.5f, NoiseType.Simplex);
            humidity = (humidity + 1f) * 0.5f; // Convert from -1..1 to 0..1

            return Mathf.Clamp01(humidity);
        }

        /// <summary>
        /// Gets continentalness - determines ocean vs land (0 = ocean, 1 = inland)
        /// </summary>
        public float GetContinentalness(int worldX, int worldZ)
        {
            float continental = noise.Get2DNoise(worldX, worldZ, continentalScale, 4, 0.45f, NoiseType.Simplex);
            continental = (continental + 1f) * 0.5f;

            // Apply curve to create more defined ocean/land boundaries
            continental = Mathf.Pow(continental, 1.5f);

            return Mathf.Clamp01(continental);
        }

        /// <summary>
        /// Gets erosion - affects terrain smoothness (0 = eroded/smooth, 1 = jagged)
        /// </summary>
        public float GetErosion(int worldX, int worldZ)
        {
            float erosion = noise.Get2DNoise(worldX - 5000, worldZ - 5000, erosionScale, 2, 0.5f, NoiseType.Perlin);
            erosion = (erosion + 1f) * 0.5f;

            return Mathf.Clamp01(erosion);
        }

        /// <summary>
        /// Selects the appropriate biome based on climate parameters
        /// Uses a decision tree similar to Minecraft's biome selection
        /// </summary>
        private BiomeType SelectBiome(float temperature, float humidity, float continental, float erosion)
        {
            // === OCEAN BIOMES (low continentalness) ===
            if (continental < 0.3f)
            {
                if (continental < 0.15f)
                {
                    // Deep ocean
                    if (temperature < 0.3f)
                        return BiomeType.FrozenOcean;
                    else
                        return BiomeType.DeepOcean;
                }
                else
                {
                    // Regular ocean
                    if (temperature < 0.3f)
                        return BiomeType.FrozenOcean;
                    else
                        return BiomeType.Ocean;
                }
            }

            // === BEACH BIOMES (coastal areas) ===
            if (continental < 0.4f)
            {
                if (erosion < 0.3f)
                    return BiomeType.StoneBeach;
                else if (temperature < 0.3f)
                    return BiomeType.SnowyBeach;
                else
                    return BiomeType.Beach;
            }

            // === RIVER BIOMES (low erosion in certain humidity) ===
            if (erosion < 0.15f && humidity > 0.4f && continental > 0.4f && continental < 0.7f)
            {
                return BiomeType.River;
            }

            // === MOUNTAIN BIOMES (high erosion or specific patterns) ===
            if (erosion > 0.75f || (erosion > 0.6f && continental > 0.7f))
            {
                if (temperature < 0.3f)
                    return BiomeType.SnowyMountains;
                else
                    return BiomeType.Mountains;
            }

            // === HILLS (medium-high erosion) ===
            if (erosion > 0.55f && erosion <= 0.75f)
            {
                return BiomeType.Hills;
            }

            // === LAND BIOMES (temperature and humidity based) ===

            // Very cold biomes (temperature < 0.25)
            if (temperature < 0.25f)
            {
                if (humidity < 0.3f)
                    return BiomeType.Tundra;
                else if (humidity < 0.6f)
                    return BiomeType.IcePlains;
                else
                    return BiomeType.Taiga;
            }

            // Cold biomes (temperature 0.25 - 0.45)
            if (temperature < 0.45f)
            {
                if (humidity < 0.4f)
                    return BiomeType.Tundra;
                else
                    return BiomeType.Taiga;
            }

            // Temperate biomes (temperature 0.45 - 0.7)
            if (temperature < 0.7f)
            {
                if (humidity < 0.3f)
                    return BiomeType.Plains;
                else if (humidity < 0.6f)
                {
                    // Forests - use noise for variation
                    float forestVariation = noise.Get2DNoise(temperature * 1000, humidity * 1000, 100f, 1, 1f);
                    if (forestVariation > 0.3f)
                        return BiomeType.BirchForest;
                    else if (forestVariation < -0.2f)
                        return BiomeType.FlowerForest;
                    else
                        return BiomeType.Forest;
                }
                else
                    return BiomeType.Swamp;
            }

            // Warm biomes (temperature 0.7 - 0.85)
            if (temperature < 0.85f)
            {
                if (humidity < 0.2f)
                    return BiomeType.Savanna;
                else if (humidity < 0.5f)
                    return BiomeType.Plains;
                else if (humidity < 0.7f)
                    return BiomeType.Forest;
                else
                    return BiomeType.Jungle;
            }

            // Hot biomes (temperature > 0.85)
            if (humidity < 0.15f)
                return BiomeType.Desert;
            else if (humidity < 0.3f)
            {
                // Mesa/Badlands variation
                float mesaVariation = noise.Get2DNoise(temperature * 1000, humidity * 1000, 80f, 1, 1f);
                return mesaVariation > 0 ? BiomeType.Mesa : BiomeType.Badlands;
            }
            else if (humidity < 0.6f)
                return BiomeType.Savanna;
            else if (humidity < 0.8f)
                return BiomeType.Jungle;
            else
                return BiomeType.Rainforest;
        }

        /// <summary>
        /// Sets the scale for biome size (larger scale = bigger biomes)
        /// </summary>
        public void SetBiomeScale(float temperatureScale, float humidityScale)
        {
            this.temperatureScale = temperatureScale;
            this.humidityScale = humidityScale;
        }

        /// <summary>
        /// Gets smooth biome transition value between current and neighbor biomes
        /// Returns a blend factor (0-1) for smooth biome transitions
        /// </summary>
        public float GetBiomeBlend(int worldX, int worldZ, int sampleRadius = 8)
        {
            BiomeData centerBiome = GetBiome(worldX, worldZ);
            int differentCount = 0;
            int totalSamples = 0;

            // Sample neighboring positions
            for (int dx = -sampleRadius; dx <= sampleRadius; dx += sampleRadius)
            {
                for (int dz = -sampleRadius; dz <= sampleRadius; dz += sampleRadius)
                {
                    if (dx == 0 && dz == 0) continue;

                    BiomeData neighborBiome = GetBiome(worldX + dx, worldZ + dz);
                    if (neighborBiome.type != centerBiome.type)
                    {
                        differentCount++;
                    }
                    totalSamples++;
                }
            }

            // Return blend factor (0 = pure biome, 1 = transition zone)
            return (float)differentCount / totalSamples;
        }
    }
}
