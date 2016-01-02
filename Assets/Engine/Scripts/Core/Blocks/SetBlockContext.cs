using System;
using Assets.Engine.Scripts.Core.Chunks;

namespace Assets.Engine.Scripts.Core.Blocks
{
    public struct SetBlockContext: IComparable<SetBlockContext>
    {        
        // Chunk section coordinates
		public readonly Chunk Chunk;
        // Block which is to be worked with
        public readonly BlockData Block;
		// Block position within chunk
		public readonly int BX;
		public readonly int BY;
		public readonly int BZ;

		public SetBlockContext(Chunk chunk, int bx, int by, int bz, BlockData block)
        {
			Chunk = chunk;
            Block = block;
            BX = bx;
            BY = by;
            BZ = bz;
        }

        public int CompareTo(SetBlockContext other)
        {
			return (Chunk==other.Chunk && BX==other.BX && BY==other.BY && BZ==other.BZ && Block.BlockType==other.Block.BlockType) ? 0 : 1;
        }
    };
}