using Assets.Engine.Scripts.Core.Blocks;
using Assets.Engine.Scripts.Core.Chunks;

namespace Assets.Engine.Scripts.Generators
{
    /// <summary>
    /// Simple generator which produces completely solid chunks
    /// </summary>
    public class SolidChunkGenerator : IChunkGenerator
    {
        #region IChunkGenerator implementation

		public void Generate (Chunk chunk)
        {
			for (int i = 0; i < EngineSettings.ChunkConfig.VolumeTotal; i++)
				chunk[i] = new BlockData(BlockType.Dirt);
        }

        #endregion
    }
}