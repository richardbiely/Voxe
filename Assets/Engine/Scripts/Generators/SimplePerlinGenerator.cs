using Assets.Engine.Plugins.CoherentNoise.Scripts.Generation;
using Assets.Engine.Scripts.Core.Blocks;
using Assets.Engine.Scripts.Core.Chunks;
using UnityEngine;

namespace Assets.Engine.Scripts.Generators
{
    /// <summary>
    /// Simple generator which produces thresholded 3D perlin noise
    /// </summary>
    public class SimplePerlinGenerator : IChunkGenerator
    {
        readonly ValueNoise m_noise = new ValueNoise (0);
	
        #region IChunkGenerator implementation
		public void Generate (Chunk chunk)
        {
            int index = 0;
			for (int y = 0; y < EngineSettings.ChunkConfig.SizeYTotal; y++)
			{
	            for (int z = 0; z < EngineSettings.ChunkConfig.Size; z++)
	            {
					int wz = z + (chunk.Pos.Z << EngineSettings.ChunkConfig.LogSize);

	                for (int x = 0; x < EngineSettings.ChunkConfig.Size; x++, index++)
	                {
						int wx = x + (chunk.Pos.X << EngineSettings.ChunkConfig.LogSize);

	                    if (m_noise.GetValue(new Vector3(wx, y, wz) * 0.1f) > 0f)
	                    {
							chunk[index] = new BlockData (BlockType.Dirt);
                        }
						else
						{
							chunk[index] = BlockData.Air;
	                    }
                    }
                }
            }
        }

        public void OnCalculateProperties(int x, int y, int z, ref BlockData data)
        {
        }

        #endregion
    }
}