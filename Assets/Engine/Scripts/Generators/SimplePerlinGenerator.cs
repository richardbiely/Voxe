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
			for (int y = 0; y < EngineSettings.ChunkConfig.SizeYTotal; y++)
			{
	            for (int z = 0; z < EngineSettings.ChunkConfig.SizeZ; z++)
	            {
					int wz = z + (chunk.Pos.Z << EngineSettings.ChunkConfig.LogSizeZ);

	                for (int x = 0; x < EngineSettings.ChunkConfig.SizeX; x++)
	                {
						int wx = x + (chunk.Pos.X << EngineSettings.ChunkConfig.LogSizeX);

	                    if (m_noise.GetValue(new Vector3(wx, y, wz) * 0.1f) > 0f)
	                    {
							chunk[x, y, z] = new BlockData (BlockType.Dirt);
	                    }
						else
						{
							chunk[x, y, z] = BlockData.Air;
	                    }
	                }
                }
            }
        }
        #endregion
    }
}