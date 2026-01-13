using UnityEngine;

namespace Pixension.WorldGen
{
    public enum NoiseType
    {
        Perlin,
        Simplex,
        Cellular,
        Ridged,
        Billow
    }

    public class NoiseGenerator
    {
        private int seed;
        private float seedOffsetX;
        private float seedOffsetZ;

        public NoiseGenerator(int seed)
        {
            this.seed = seed;

            // Generate seed offsets from the seed
            System.Random rng = new System.Random(seed);
            seedOffsetX = (float)rng.NextDouble() * 10000f;
            seedOffsetZ = (float)rng.NextDouble() * 10000f;
        }

        public float Get2DNoise(float x, float z, float scale, int octaves, float persistence, NoiseType type = NoiseType.Perlin)
        {
            // Validation
            if (scale <= 0f) scale = 1f;
            if (octaves < 1) octaves = 1;

            float total = 0f;
            float frequency = 1f;
            float amplitude = 1f;
            float maxValue = 0f;

            for (int i = 0; i < octaves; i++)
            {
                float sampleX = (x * frequency / scale) + seedOffsetX;
                float sampleZ = (z * frequency / scale) + seedOffsetZ;

                float noiseValue = 0f;

                switch (type)
                {
                    case NoiseType.Perlin:
                        noiseValue = PerlinNoise(sampleX, sampleZ);
                        break;
                    case NoiseType.Simplex:
                        noiseValue = SimplexNoise(sampleX, sampleZ);
                        break;
                    case NoiseType.Cellular:
                        noiseValue = CellularNoise(sampleX, sampleZ);
                        break;
                    case NoiseType.Ridged:
                        noiseValue = RidgedNoise(sampleX, sampleZ);
                        break;
                    case NoiseType.Billow:
                        noiseValue = BillowNoise(sampleX, sampleZ);
                        break;
                }

                total += noiseValue * amplitude;

                maxValue += amplitude;
                amplitude *= persistence;
                frequency *= 2f;
            }

            // Normalize to -1 to 1
            return total / maxValue;
        }

        // Legacy method for backward compatibility
        public float Get2DNoise(float x, float z, float scale, int octaves, float persistence)
        {
            return Get2DNoise(x, z, scale, octaves, persistence, NoiseType.Perlin);
        }

        public float Get3DNoise(float x, float y, float z, float scale, NoiseType type = NoiseType.Perlin)
        {
            float xy = Get2DNoise(x, y, scale, 1, 0.5f, type);
            float yz = Get2DNoise(y, z, scale, 1, 0.5f, type);
            float xz = Get2DNoise(x, z, scale, 1, 0.5f, type);

            float xyz = (xy + yz + xz) / 3f;
            return xyz;
        }

        public float Get3DNoise(float x, float y, float z, float scale)
        {
            return Get3DNoise(x, y, z, scale, NoiseType.Perlin);
        }

        private float PerlinNoise(float x, float z)
        {
            // Perlin Noise returns 0-1, convert to -1 to 1
            float perlinValue = Mathf.PerlinNoise(x, z) * 2f - 1f;
            return perlinValue;
        }

        private float SimplexNoise(float x, float z)
        {
            // Simplified 2D Simplex-like noise
            // Uses multiple offset Perlin samples for a smoother, more natural look
            float n1 = Mathf.PerlinNoise(x, z);
            float n2 = Mathf.PerlinNoise(x + 5.2f, z + 1.3f);
            float n3 = Mathf.PerlinNoise(x - 3.7f, z + 8.1f);

            float result = (n1 + n2 + n3) / 3f;
            return result * 2f - 1f;
        }

        private float CellularNoise(float x, float z)
        {
            // Voronoi/Cellular noise - creates cell-like patterns
            int xi = Mathf.FloorToInt(x);
            int zi = Mathf.FloorToInt(z);

            float minDist = float.MaxValue;

            // Check 3x3 grid of cells
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    int cellX = xi + dx;
                    int cellZ = zi + dz;

                    // Get random point in this cell
                    Vector2 cellPoint = GetCellPoint(cellX, cellZ);
                    Vector2 point = new Vector2(cellX + cellPoint.x, cellZ + cellPoint.y);

                    // Calculate distance to this point
                    float dist = Vector2.Distance(new Vector2(x, z), point);
                    minDist = Mathf.Min(minDist, dist);
                }
            }

            // Normalize and convert to -1 to 1
            float normalized = Mathf.Clamp01(minDist);
            return normalized * 2f - 1f;
        }

        private float RidgedNoise(float x, float z)
        {
            // Ridged noise - creates sharp mountain ridges
            float n = Mathf.PerlinNoise(x, z) * 2f - 1f;
            return 1f - Mathf.Abs(n);
        }

        private float BillowNoise(float x, float z)
        {
            // Billow noise - creates puffy cloud-like formations
            float n = Mathf.PerlinNoise(x, z) * 2f - 1f;
            return Mathf.Abs(n) * 2f - 1f;
        }

        private Vector2 GetCellPoint(int cellX, int cellZ)
        {
            // Generate consistent random point for this cell
            int hash = Hash(cellX, cellZ);
            System.Random rng = new System.Random(hash);

            return new Vector2((float)rng.NextDouble(), (float)rng.NextDouble());
        }

        public int Hash(int x, int z)
        {
            return (x * 73856093) ^ (z * 19349663) ^ seed;
        }

        public System.Random GetRandom(int x, int z)
        {
            int hash = Hash(x, z);
            return new System.Random(hash);
        }

        // Additional utility methods for advanced noise combinations

        public float Turbulence(float x, float z, float scale, int octaves)
        {
            float value = 0f;
            float amplitude = 1f;
            float frequency = 1f;

            for (int i = 0; i < octaves; i++)
            {
                float sampleX = (x * frequency / scale) + seedOffsetX;
                float sampleZ = (z * frequency / scale) + seedOffsetZ;

                value += Mathf.Abs(PerlinNoise(sampleX, sampleZ)) * amplitude;

                amplitude *= 0.5f;
                frequency *= 2f;
            }

            return value;
        }

        public float DomainWarp(float x, float z, float scale, float strength)
        {
            // Apply domain warping for more organic shapes
            float offsetX = Get2DNoise(x, z, scale, 2, 0.5f) * strength;
            float offsetZ = Get2DNoise(x + 100, z + 100, scale, 2, 0.5f) * strength;

            return Get2DNoise(x + offsetX, z + offsetZ, scale, 3, 0.5f);
        }
    }
}
