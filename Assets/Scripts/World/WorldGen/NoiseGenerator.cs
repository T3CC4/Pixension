using UnityEngine;

namespace Pixension.WorldGen
{
    public class NoiseGenerator
    {
        private int seed;
        private float seedOffsetX;
        private float seedOffsetZ;

        public NoiseGenerator(int seed)
        {
            this.seed = seed;

            // Generiere Seed-Offsets aus dem Seed
            System.Random rng = new System.Random(seed);
            seedOffsetX = (float)rng.NextDouble() * 10000f;
            seedOffsetZ = (float)rng.NextDouble() * 10000f;
        }

        public float Get2DNoise(float x, float z, float scale, int octaves, float persistence)
        {
            // Validierung
            if (scale <= 0f) scale = 1f;
            if (octaves < 1) octaves = 1;

            float total = 0f;
            float frequency = 1f;
            float amplitude = 1f;
            float maxValue = 0f;

            for (int i = 0; i < octaves; i++)
            {
                // Korrekte Noise-Berechnung mit Seed-Offset
                float sampleX = (x * frequency / scale) + seedOffsetX;
                float sampleZ = (z * frequency / scale) + seedOffsetZ;

                // Perlin Noise gibt 0-1 zurück, konvertiere zu -1 bis 1
                float perlinValue = Mathf.PerlinNoise(sampleX, sampleZ) * 2f - 1f;
                total += perlinValue * amplitude;

                maxValue += amplitude;
                amplitude *= persistence;
                frequency *= 2f;
            }

            // Normalisiere auf -1 bis 1
            return total / maxValue;
        }

        public float Get3DNoise(float x, float y, float z, float scale)
        {
            float xy = Get2DNoise(x, y, scale, 1, 0.5f);
            float yz = Get2DNoise(y, z, scale, 1, 0.5f);
            float xz = Get2DNoise(x, z, scale, 1, 0.5f);

            float xyz = (xy + yz + xz) / 3f;
            return xyz;
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
    }
}