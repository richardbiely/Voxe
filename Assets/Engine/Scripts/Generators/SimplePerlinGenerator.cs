using Engine.Plugins.CoherentNoise.Scripts.Generation;
using Engine.Scripts.Core.Blocks;
using Engine.Scripts.Core.Chunks;
using UnityEngine;

namespace Engine.Scripts.Generators
{
    /// <summary>
    ///     Simple generator which produces thresholded 3D perlin noise
    /// </summary>
    public class SimplePerlinGenerator: AChunkGenerator
    {
        private readonly ValueNoise m_noise = new ValueNoise(0);

        #region IChunkGenerator implementation

        public override void Generate(Chunk chunk)
        {
            int xOffset = chunk.Pos.X<<EngineSettings.ChunkConfig.LogSize;
            int yOffset = chunk.Pos.Y<<EngineSettings.ChunkConfig.LogSize;
            int zOffset = chunk.Pos.Z<<EngineSettings.ChunkConfig.LogSize;

            for (int y = EngineSettings.ChunkConfig.Mask; y>=0; y--)
            {
                int wy = y+yOffset;

                for (int z = 0; z<EngineSettings.ChunkConfig.Size; z++)
                {
                    int wz = z+zOffset;

                    for (int x = 0; x<EngineSettings.ChunkConfig.Size; x++)
                    {
                        int wx = x+xOffset;

                        float noise = m_noise.GetValue(new Vector3(wx, wy, wz)*0.10f);//10
                        if (noise>0f)
                        {
                            chunk.GenerateBlock(x, y, z, new BlockData(BlockType.Dirt));
                        }
                        else
                        {
                            chunk.GenerateBlock(x, y, z, BlockData.Air);
                        }
                    }
                }
            }
        }

        #endregion IChunkGenerator implementation
    }
}