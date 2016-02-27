using System;
using Assets.Engine.Scripts.Core.Chunks;

namespace Assets.Engine.Scripts.Core.Blocks
{
    public struct SetBlockContext: IComparable<SetBlockContext>, IEquatable<SetBlockContext>
    {
        //! Chunk section coordinates
        public readonly Chunk Chunk;

        //! Block which is to be worked with
        public readonly BlockData Block;

        //! Block position within chunk
        public readonly int BX;

        public readonly int BY;
        public readonly int BZ;

        //! Mask of subscriber indexes to notify
        public readonly int SubscribersMask;

        public readonly int SectionsMask;

        public SetBlockContext(Chunk chunk, int bx, int by, int bz, BlockData block, int subscribersMask,
            int sectionsMask)
        {
            Chunk = chunk;
            Block = block;
            BX = bx;
            BY = by;
            BZ = bz;
            SubscribersMask = subscribersMask;
            SectionsMask = sectionsMask;
        }

        private static bool AreEqual(ref SetBlockContext a, ref SetBlockContext b)
        {
            return a.Chunk.Equals(b.Chunk) && a.BX==b.BX && a.BY==b.BY && a.BZ==b.BZ && a.Block.BlockType==b.Block.BlockType;
        }

        public static bool operator==(SetBlockContext lhs, SetBlockContext rhs)
        {
            return AreEqual(ref lhs, ref rhs);
        }

        public static bool operator!=(SetBlockContext lhs, SetBlockContext rhs)
        {
            return !AreEqual(ref lhs, ref rhs);
        }

        public int CompareTo(SetBlockContext other)
        {
            return AreEqual(ref this, ref other) ? 0 : 1;
        }

        public override bool Equals(object other)
        {
            if (!(other is SetBlockContext))
                return false;

            SetBlockContext vec = (SetBlockContext)other;
            return AreEqual(ref this, ref vec);
        }

        public bool Equals(SetBlockContext other)
        {
            return AreEqual(ref this, ref other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (Chunk!=null ? Chunk.GetHashCode() : 0);
                hashCode = (hashCode*397)^(int)Block.BlockType;
                hashCode = (hashCode*397)^BX;
                hashCode = (hashCode*397)^BY;
                hashCode = (hashCode*397)^BZ;
                return hashCode;
            }
        }
    }
}