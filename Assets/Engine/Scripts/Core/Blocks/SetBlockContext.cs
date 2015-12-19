using System;

namespace Assets.Engine.Scripts.Core.Blocks
{
    struct SetBlockContext: IComparable<SetBlockContext>
    {
        // Block position within chunk
        public readonly int BX;
        public readonly int BY;
        public readonly int BZ;
        // Chunk section coordinates
        public readonly int CX;
        public readonly int CY;
        public readonly int CZ;
        // Block which is to be worked with
        public readonly BlockData Block;

        public SetBlockContext(BlockData block, int bx, int by, int bz, int cx, int cy, int cz)
        {
            Block = block;
            BX = bx;
            BY = by;
            BZ = bz;
            CX = cx;
            CY = cy;
            CZ = cz;
        }

        public int CompareTo(SetBlockContext other)
        {
            return (BX == other.BX && BY == other.BY && BZ == other.BZ && Block.BlockType == other.Block.BlockType) ? 0 : 1;
        }
    };
}