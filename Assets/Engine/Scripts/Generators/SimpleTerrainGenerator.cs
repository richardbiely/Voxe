using Engine.Plugins.CoherentNoise.Scripts.Generation;
using Engine.Scripts.Core.Blocks;
using Engine.Scripts.Core.Chunks;
using UnityEngine;

namespace Engine.Scripts.Generators
{
    /// <summary>
    ///     Produces a simple Minecraft-like terrain
    /// </summary>
    public class SimpleTerrainGenerator: AChunkGenerator
    {
        private const float Coef = 0.015f;
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

                        bool currentPoint = Eval(wx, wy, wz);
                        if (currentPoint)
                        {
                            bool up = Eval(wx, wy+1, wz);
                            if (!up)
                            {
                                chunk.GenerateBlock(x, y, z, new BlockData(BlockType.Grass));
                            }
                            else
                            {
                                if (y>50)
                                    chunk.GenerateBlock(x, y, z, new BlockData(BlockType.Dirt));
                                else
                                    chunk.GenerateBlock(x, y, z, new BlockData(BlockType.Stone));
                            }
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

        private bool Eval(int x, int y, int z)
        {
            float density = m_noise.GetValue(new Vector3(x, y, z)*Coef);
            density -= (y-64)*Coef;
            return density>0f;
        }
    }
}