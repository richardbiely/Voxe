using Engine.Scripts.Common;
using Engine.Scripts.Common.IO.RLE;

namespace Engine.Scripts.Core.Blocks
{
    public class BlockStorageRLE: IBlockStorage
    {
        public RLE<BlockData>  RLE { get; private set; }

        public BlockStorageRLE()
        {
            RLE = new RLE<BlockData>();
        }

        #region IBlockStorage implementation

        public BlockData this[int index]
        {
            get { return RLE[index]; }
            set { RLE[index] = value; }
        }

        public BlockData this[int x, int y, int z]
        {
            get { return RLE[Helpers.GetIndex1DFrom3D(x, y, z)]; }
            set { RLE[Helpers.GetIndex1DFrom3D(x, y, z)] = value; }
        }

        public void Set(ref BlockData[] data)
        {
            RLE.Compress(ref data);
        }

        public void Reset()
        {
            RLE.Reset();
        }

        public BlockData[] ToArray()
        {
            return RLE.Decompress();
        }

        public void ToArray(ref BlockData[] outData)
        {
            RLE.Decompress(ref outData);
        }

        #endregion
    }
}
