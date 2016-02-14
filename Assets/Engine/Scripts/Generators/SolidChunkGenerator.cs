using Assets.Engine.Scripts.Core.Blocks;
using Assets.Engine.Scripts.Core.Chunks;
using UnityEngine;

namespace Assets.Engine.Scripts.Generators
{
    /// <summary>
    /// Simple generator which produces completely solid chunks
    /// </summary>
    public class SolidChunkGenerator : AChunkGenerator
    {
        #region IChunkGenerator implementation

		public override void Generate (Chunk chunk)
        {
			for (int i = 0; i < EngineSettings.ChunkConfig.VolumeTotal; i++)
				chunk[i] = new BlockData(BlockType.Dirt);
        }

        public override void OnCalculateProperties(int x, int y, int z, ref BlockData data)
        {
        }

        #endregion
    }
}