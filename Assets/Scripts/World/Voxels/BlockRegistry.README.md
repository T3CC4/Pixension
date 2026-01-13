# BlockRegistry System

The BlockRegistry is a global singleton that allows WorldGenerators and other systems to register and share voxel block definitions. This prevents duplication and ensures consistency across the game.

## Features

- **Global Access**: Any generator or system can access registered blocks
- **Prevents Duplication**: Blocks are defined once and reused everywhere
- **Namespace Support**: Use prefixes like "generator:blockname" for organization
- **Default Blocks**: Common blocks (stone, dirt, grass, etc.) are pre-registered
- **Dynamic Registration**: Generators can register custom blocks at runtime

## Usage Examples

### Accessing Pre-registered Blocks

```csharp
using Pixension.Voxels;

// Get a block from the registry
VoxelData stone = BlockRegistry.Instance.GetBlock("stone");
VoxelData grass = BlockRegistry.Instance.GetBlock("grass");
VoxelData water = BlockRegistry.Instance.GetBlock("water");
```

### Registering Custom Blocks in a Generator

```csharp
public class MyCustomGenerator : WorldGenerator
{
    public MyCustomGenerator(int seed) : base(seed, "my_generator")
    {
        RegisterCustomBlocks();
    }

    private void RegisterCustomBlocks()
    {
        // Register generator-specific blocks with namespace prefix
        blockRegistry.RegisterBlock("my_generator:crystal", new VoxelData(
            VoxelType.Solid,
            new Color(0.5f, 0.8f, 1.0f) // Light blue crystal
        ));

        blockRegistry.RegisterBlock("my_generator:lava", new VoxelData(
            VoxelType.Liquid,
            new Color(1.0f, 0.3f, 0.0f, 0.8f) // Orange lava
        ));
    }

    public override void GenerateChunkTerrain(Chunk chunk)
    {
        // Use registered blocks
        VoxelData crystal = blockRegistry.GetBlock("my_generator:crystal");
        VoxelData stone = blockRegistry.GetBlock("stone"); // Default block

        // ... generation logic ...
    }
}
```

### Creating Block Variations

```csharp
// Create a darker version of stone
VoxelData darkStone = BlockRegistry.Instance.CreateColorVariation("stone", 0.7f);

// Create a color blend
Color targetColor = new Color(0.3f, 0.8f, 0.3f);
VoxelData customGrass = BlockRegistry.Instance.CreateColorLerp("grass", targetColor, 0.5f);
```

### Using Blocks from Other Generators

```csharp
// Another generator can use blocks registered by GrasslandGenerator
VoxelData mountainStone = blockRegistry.GetBlock("grassland:stone_mountain");
VoxelData wetSand = blockRegistry.GetBlock("grassland:sand_wet");
```

## Pre-registered Default Blocks

The following blocks are automatically registered on startup:

### Basic
- `air` - Empty space
- `water` - Transparent liquid
- `bedrock` - Indestructible bottom layer

### Stone
- `stone` - Standard gray stone
- `stone_dark` - Darker stone variant
- `stone_light` - Lighter stone variant

### Dirt
- `dirt` - Standard brown dirt
- `dirt_dark` - Darker dirt variant

### Grass
- `grass` - Standard green grass
- `grass_dark` - Darker grass (low/wet areas)
- `grass_light` - Lighter grass (high/dry areas)
- `grass_dry` - Yellow-green dry grass

### Sand
- `sand` - Standard beige sand
- `sand_light` - Lighter sand
- `sand_dark` - Darker/wet sand

### Other
- `snow` - White snow block
- `wood` - Dark brown wood
- `wood_light` - Lighter wood
- `leaves` - Green foliage
- `leaves_dark` - Darker foliage

## Best Practices

1. **Use Namespaces**: Prefix your custom blocks with your generator ID
   - Good: `"desert:cactus"`
   - Bad: `"cactus"` (might conflict with other generators)

2. **Register Early**: Call `RegisterCustomBlocks()` in your generator constructor

3. **Check Existence**: Use `HasBlock()` before accessing if unsure
   ```csharp
   if (blockRegistry.HasBlock("custom:block"))
   {
       VoxelData block = blockRegistry.GetBlock("custom:block");
   }
   ```

4. **Reuse Default Blocks**: Don't register new blocks for common materials

5. **Document Your Blocks**: Add comments explaining unique block properties

## API Reference

### Registration
- `RegisterBlock(string blockId, VoxelData voxelData)` - Register or update a block
- `UnregisterBlock(string blockId)` - Remove a block (use with caution)

### Access
- `GetBlock(string blockId)` - Get a block (returns Air if not found)
- `HasBlock(string blockId)` - Check if block exists
- `GetAllBlockIds()` - List all registered block IDs
- `GetBlockCount()` - Get total number of blocks

### Utilities
- `CreateColorVariation(string baseBlockId, float multiplier)` - Create darker/lighter variant
- `CreateColorLerp(string baseBlockId, Color target, float t)` - Blend with color

## World Height Limits

The world generation respects the following limits:
- **Minimum Height**: 0 (bedrock layer)
- **Maximum Height**: 2048
- **Default Base Height**: 1024 (average ground level)
- **Default Water Level**: 1000 (varies by generator)

Generators should clamp terrain height between these limits:
```csharp
int height = Mathf.Clamp(calculatedHeight, 0, 2048);
```
