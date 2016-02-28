using System;
using Assets.Engine.Scripts.Common;

namespace Assets.Engine.Scripts.Core.Blocks
{
    public class BlockStorageArray: IBlockStorage
    {
        public BlockData[] Blocks;

        public BlockStorageArray()
        {
            Blocks = Helpers.CreateArray1D<BlockData>(EngineSettings.ChunkConfig.Size*EngineSettings.ChunkConfig.Size*EngineSettings.ChunkConfig.Size);
        }

        #region IBlockStorage implementation
        
        public BlockData this[int x, int y, int z]
        {
            /* NOTE:
                Chunk generation which takes the most time currently accesses the memory
                in y->z->x fashion where y is constantly decreasing. Therefore, instead
                of accessing memory as [x,y,z], [x,MaskYTotal-y,z] would be more suitable
                for performance.
                Benchmarking showed that although small there's a noticable increase in
                performance. However, as this part is later going to be written completely
                differently, I decided to let it be for now.
            */
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
