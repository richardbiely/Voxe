using System;
using System.IO;
using System.Runtime.InteropServices;
using Engine.Scripts.Common.IO;

namespace Engine.Scripts.Core.Blocks
{
    /// <summary>
    /// Represents data about a block
    /// </summary>
    [StructLayout(LayoutKind.Sequential,Pack=1)]
    [Serializable]
    public struct BlockData : IComparable<BlockData>, IBinarizable
    {
        /// <summary>
        /// Template for air blocks
        /// </summary>
        public static readonly BlockData Air = new BlockData (BlockType.None);

        /// <summary>
        /// The type of this block
        /// </summary>
        public readonly BlockType BlockType;
	
        /// <summary>
        /// Meta data for this block
        /// First nibble is general meta
        /// Second nibble is damage
        /// </summary>
        private byte MetaData;
        
        public BlockData (BlockType blockType)
        {
            BlockType = blockType;
            MetaData = 0;
        }

        public BlockData(BlockType blockType, byte meta)
        {
            BlockType = blockType;
            MetaData = meta;
        }
	
        /// <summary>
        /// Gets the general-purpose meta data.
        /// </summary>
        public byte GetMeta ()
        {
            return (byte)(MetaData & 0x0f);
        }
	
        /// <summary>
        /// Sets the general-purpose meta data.
        /// </summary>
        public BlockData SetMeta (byte meta)
        {
            MetaData |= (byte)(meta & 0x0f);
            return this;
        }
	
        /// <summary>
        /// Gets the damage of this block.
        /// </summary>
        public byte GetDamage ()
        {
            return (byte)((MetaData & 0xf0) >> 4);
        }
	
        /// <summary>
        /// Gets the damage as a percent percent from 0 to 1.
        /// </summary>
        public float GetDamagePercent ()
        {
            const float mult = 1f / 15f; // damage is a nibble from 0-15
		
            byte rawDmg = GetDamage ();
            return rawDmg * mult;
        }
	
        /// <summary>
        /// Sets the damage of this block
        /// </summary>
        public BlockData SetDamage (byte damage)
        {
            MetaData &= 0x0f;
            MetaData |= (byte)( ( damage << 4 ) & 0xf0 );
            return this;
        }

        /// <summary>
        /// Whether this block is empty
        /// </summary>
        public bool IsEmpty()
        {
            return BlockType == BlockType.None;
        }

        public bool IsSolid()
        {
            return BlockDatabase.GetBlockInfo(BlockType).IsSolid;
        }

        #region IComparable implementation

        public int CompareTo(BlockData data)
        {
            if (BlockType == data.BlockType)
                return 0;
            if ((int)BlockType < (int)data.BlockType)
                return -1;
            return 1;
        }

        #endregion
        
        public void Binarize(BinaryWriter bw)
        {
            bw.Write((byte)BlockType);
            bw.Write(MetaData);
        }

        public void Debinarize(BinaryReader br)
        {
            BlockType bt = (BlockType) br.ReadByte();
            byte meta = br.ReadByte();
            this = new BlockData(bt,meta);
        }
    }
}