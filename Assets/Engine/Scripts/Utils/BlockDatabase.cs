using Assets.Engine.Scripts.Atlas;
using Assets.Engine.Scripts.Builders;
using Assets.Engine.Scripts.Core.Blocks;
using Assets.Engine.Scripts.Physics;
using UnityEngine;

namespace Assets.Engine.Scripts.Utils
{
    // TODO: Make this configurable
    public static class BlockDatabase
    {
        private static readonly IBlockBuilder[] SBlockBuilders =
        {
            null, // AIR
            new CubeBuilder
                (new[]
                {
                    BlockTexture.Dirt,
                    BlockTexture.Dirt,
                    BlockTexture.Dirt,
                    BlockTexture.Dirt,
                    BlockTexture.Dirt,
                    BlockTexture.Dirt
                }), // DIRT
            new CubeBuilder
                (new[]
                {
                    BlockTexture.GrassSide,
                    BlockTexture.GrassSide,
                    BlockTexture.GrassSide,
                    BlockTexture.GrassSide,
                    BlockTexture.Grass,
                    BlockTexture.Dirt
                }), // GRASS
            new CubeBuilder
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
            new BlockInfo(false, PhysicsType.None, new Color32(0xFF, 0xFF, 0xFF, 0xFf)),
            // DIRT
            new BlockInfo(true, PhysicsType.Solid, new Color32(0x5C, 0x3B, 0x00, 0xFf)),
            // GRASS
            new BlockInfo(true, PhysicsType.Solid, new Color32(0x00, 0x80, 0x00, 0xFf)),
            // STONE
            new BlockInfo(true, PhysicsType.Solid, new Color32(0xA9, 0xA9, 0xA9, 0xFf))
        };

        /// <summary>
        /// Gets the block builder for the given block type
        /// </summary>
        public static IBlockBuilder GetBlockBuilder(BlockType type)
        {
            return SBlockBuilders[(int) type];
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