using Engine.Scripts.Core.Blocks;
using Engine.Scripts.Core.Chunks;

namespace Engine.Scripts.Generators
{
    /// <summary>
    ///     Simple generator which produces completely solid chunks
    /// </summary>
    public class SolidChunkGenerator: AChunkGenerator
    {
        #region IChunkGenerator implementation

        public override void Generate(Chunk chunk)
        {
            for (int y = EngineSettings.ChunkConfig.Mask; y>=0; y--)
            {
                for (int z = 0; z<EngineSettings.ChunkConfig.Size; z++)
                {
                    for (int x = 0; x<EngineSettings.ChunkConfig.Size; x++)
                    {
                        chunk.GenerateBlock(x, y, z, new BlockData(BlockType.Dirt));
                    }
                }
            }
        }

        #endregion IChunkGenerator implementation
    }
}