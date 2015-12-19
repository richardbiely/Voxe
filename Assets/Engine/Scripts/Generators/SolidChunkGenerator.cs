using Assets.Engine.Scripts.Core.Blocks;
using Assets.Engine.Scripts.Core.Chunks;

namespace Assets.Engine.Scripts.Generators
{
    /// <summary>
    /// Simple generator which produces completely solid chunks
    /// </summary>
    public class SolidChunkGenerator : IMiniChunkGenerator
    {
        #region IChunkGenerator implementation
        public void Generate (MiniChunk section)
        {
            for (int z = 0; z < EngineSettings.ChunkConfig.SizeZ; z++)
            {
                for (int y = 0; y < EngineSettings.ChunkConfig.SizeY; y++)
                {
                    for (int x = 0; x < EngineSettings.ChunkConfig.SizeX; x++)
                    {
                        section [x, y, z] = new BlockData (BlockType.Dirt);
                    }
                }
            }
        }
        #endregion
    }
}