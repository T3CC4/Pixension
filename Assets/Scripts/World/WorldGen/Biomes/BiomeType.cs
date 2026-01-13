namespace Pixension.WorldGen.Biomes
{
    /// <summary>
    /// Defines all available biome types in the world
    /// Similar to Minecraft's biome system
    /// </summary>
    public enum BiomeType
    {
        // Temperature-based biomes
        Ocean,
        DeepOcean,
        FrozenOcean,

        // Cold biomes
        Tundra,
        Taiga,
        SnowyMountains,
        IcePlains,

        // Temperate biomes
        Plains,
        Forest,
        BirchForest,
        FlowerForest,
        Mountains,
        Hills,
        River,

        // Warm biomes
        Savanna,
        Desert,
        Mesa,
        Badlands,

        // Wet biomes
        Swamp,
        Jungle,
        Rainforest,

        // Special biomes
        MushroomIsland,
        Beach,
        SnowyBeach,
        StoneBeach
    }
}
