using UnityEngine;

namespace Pixension.WorldGen
{
    public class NoiseGenerator
    {
        private int seed;

        public NoiseGenerator(int seed)
        {
            this.seed = seed;
        }

        public float Get2DNoise(float x, float z, float scale, int octaves, float persistence)
        {
            float total = 0f;
            float frequency = 1f;
            float amplitude = 1f;
            float maxValue = 0f;

            for (int i = 0; i < octaves; i++)
            {
                float sampleX = (x / scale) * frequency + seed;
                float sampleZ = (z / scale) * frequency + seed * 0.1f;

                float perlinValue = Mathf.PerlinNoise(sampleX, sampleZ) * 2f - 1f;
                total += perlinValue * amplitude;

                maxValue += amplitude;
                amplitude *= persistence;
                frequency *= 2f;
            }

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