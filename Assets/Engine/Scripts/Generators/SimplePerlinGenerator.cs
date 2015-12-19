using Assets.Engine.Plugins.CoherentNoise.Scripts.Generation;
using Assets.Engine.Scripts.Core.Blocks;
using Assets.Engine.Scripts.Core.Chunks;
using UnityEngine;

namespace Assets.Engine.Scripts.Generators
{
    /// <summary>
    /// Simple generator which produces thresholded 3D perlin noise
    /// </summary>
    public class SimplePerlinGenerator : IMiniChunkGenerator
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

                        if (m_noise.GetValue(new Vector3(wx, wy, wz) * 0.1f) > 0f)
                        {
                            section[x, y, z] = new BlockData (BlockType.Dirt);
                        } else {
                            section[x, y, z] = BlockData.Air;
                        }
                    }
                }
            }
        }
        #endregion
    }
}