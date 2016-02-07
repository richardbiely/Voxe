using System;
using Assets.Engine.Scripts.Common;

namespace Assets.Engine.Scripts.Core.Blocks
{
    public class BlockStorageArray: IBlockStorage
    {
        public BlockData[] Blocks;

        public BlockStorageArray()
        {
            Blocks = Helpers.CreateArray1D<BlockData>(EngineSettings.ChunkConfig.SizeX*EngineSettings.ChunkConfig.SizeZ*EngineSettings.ChunkConfig.SizeYTotal);
        }

        #region IBlockStorage implementation

        public BlockData this[int index]
        {
            get { return Blocks[index]; }
            set { Blocks[index] = value; }
        }

        public BlockData this[int x, int y, int z]
        {
            get { return Blocks[Helpers.GetIndex1DFrom3D(x, y, z)]; }
            set { Blocks[Helpers.GetIndex1DFrom3D(x, y, z)] = value; }
        }

        public void Set(ref BlockData[] data)
        {
            Blocks = data;
        }

        public void Reset()
        {
            Array.Clear(Blocks, 0, Blocks.Length);
        }

        public BlockData[] ToArray()
        {
            return Blocks;
        }

        public void ToArray(ref BlockData[] outData)
        {
            Array.Copy(Blocks, outData, Blocks.Length);
        }

        #endregion
    }
}
