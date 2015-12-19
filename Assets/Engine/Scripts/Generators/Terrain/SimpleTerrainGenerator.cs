using Assets.Engine.Plugins.CoherentNoise.Scripts.Generation;
using Assets.Engine.Scripts.Core.Blocks;
using Assets.Engine.Scripts.Core.Chunks;
using UnityEngine;

namespace Assets.Engine.Scripts.Generators.Terrain
{
    /// <summary>
    /// Produces a simple Minecraft-like terrain
    /// </summary>
    public class SimpleTerrainGenerator : IMiniChunkGenerator
    {
        readonly ValueNoise m_noise = new ValueNoise (0);
	
        #region IChunkGenerator implementation
        public void Generate (MiniChunk section)
        {
            for (int z = 0; z < EngineSettings.ChunkConfig.SizeZ; z++)
            {
                int wz = z + (section.Pos.Z << EngineSettings.ChunkConfig.LogSizeZ);

                for (int y = 0; y < EngineSettings.ChunkConfig.SizeY; y++)
                {
                    int wy = y+section.OffsetY;

                    for (int x = 0; x < EngineSettings.ChunkConfig.SizeX; x++)
                    {
                        int wx = x + (section.Pos.X << EngineSettings.ChunkConfig.LogSizeX);

                        bool currentPoint = Eval(wx, wy, wz);
                        bool up = Eval(wx, wy + 1, wz);
					
                        if (currentPoint) {
                            if (!up) {
                                section [x, y, z] = new BlockData (BlockType.Grass);
                                // Grass block was placed, lets place a tree blueprint here
                                if (wy < EngineSettings.ChunkConfig.SizeYTotal)
                                {
                                    //int height =
                                }
                            } else {
                                if (wy > 50) {
                                    section [x, y, z] = new BlockData (BlockType.Dirt);
                                } else {
                                    section [x, y, z] = new BlockData (BlockType.Stone);
                                }
                            }
                        } else {
                            section [x, y, z] = BlockData.Air;
                        }
                    }
                }
            }
        }
        #endregion

        private const float Coef = 0.075f;

        private bool Eval (int x, int y, int z)
        {
            float density = m_noise.GetValue(new Vector3(x, y, z) * Coef);
            density -= ((y - 64) * Coef);
            return density > 0f;
        }
    }
}