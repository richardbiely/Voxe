using Engine.Scripts.Builders.Faces;
using UnityEngine;

namespace Engine.Scripts.Core.Blocks
{
    // TODO: Make this configurable
    public static class BlockDatabase
    {
        private static readonly IFaceBuilder[] SFaceBuilders =
        {
            null, // AIR
            new CubeFaceBuilder
                (new[]
                {
                    BlockTexture.Dirt,
                    BlockTexture.Dirt,
                    BlockTexture.Dirt,
                    BlockTexture.Dirt,
                    BlockTexture.Dirt,
                    BlockTexture.Dirt
                }), // DIRT
            new CubeFaceBuilder
                (new[]
                {
                    BlockTexture.GrassSide,
                    BlockTexture.GrassSide,
                    BlockTexture.GrassSide,
                    BlockTexture.GrassSide,
                    BlockTexture.Grass,
                    BlockTexture.Dirt
                }), // GRASS
            new CubeFaceBuilder
                (new[]
                {
                    BlockTexture.Stone,
                    BlockTexture.Stone,
                    BlockTexture.Stone,
                    BlockTexture.Stone,
                    BlockTexture.Stone,
                    BlockTexture.Stone
                }) // STONE
        };

        private static readonly BlockInfo[] SBlockInfo =
        {
            // AIR
            new BlockInfo(false, new Color32(0xFF, 0xFF, 0xFF, 0xFF)),
            // DIRT
            new BlockInfo(true, new Color32(0x5C, 0x3B, 0x00, 0xFF)),
            // GRASS
            new BlockInfo(true, new Color32(0x00, 0x80, 0x00, 0xFF)),
            // STONE
            new BlockInfo(true, new Color32(0xA9, 0xA9, 0xA9, 0xFF))
        };

        /// <summary>
        /// Gets the block builder for the given block type
        /// </summary>
        public static IFaceBuilder GetFaceBuilder(BlockType type)
        {
            return SFaceBuilders[(int) type];
        }

        /// <summary>
        /// Gets the block info for the given block type
        /// </summary>
        public static BlockInfo GetBlockInfo(BlockType type)
        {
            return SBlockInfo[(int) type];
        }
    }
}