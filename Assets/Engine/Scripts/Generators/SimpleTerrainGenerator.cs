using Assets.Engine.Plugins.CoherentNoise.Scripts.Generation;
using Assets.Engine.Scripts.Core.Blocks;
using Assets.Engine.Scripts.Core.Chunks;
using UnityEngine;

namespace Assets.Engine.Scripts.Generators
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
            for (int y = EngineSettings.ChunkConfig.MaskYTotal; y>=0; y--)
            {
                for (int z = 0; z<EngineSettings.ChunkConfig.Size; z++)
                {
                    int wz = z+(chunk.Pos.Z<<EngineSettings.ChunkConfig.LogSize);

                    for (int x = 0; x<EngineSettings.ChunkConfig.Size; x++)
                    {
                        int wx = x+(chunk.Pos.X<<EngineSettings.ChunkConfig.LogSize);

                        bool currentPoint = Eval(wx, y, wz);
                        if (currentPoint)
                        {
                            bool up = Eval(wx, y+1, wz);
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