using System;
using Assets.Engine.Scripts.Common;

namespace Assets.Engine.Scripts.Core.Blocks
{
    public class BlockStorage: IBlockStorage
    {
        //public static readonly int StrideX = ((EngineSettings.WorldConfig.CachedRange*2) + 1)*EngineSettings.ChunkConfig.SizeX;
        //public static readonly int StrideZ = ((EngineSettings.WorldConfig.CachedRange*2) + 1)*EngineSettings.ChunkConfig.SizeZ;

        //public static readonly int XStep = StrideZ * EngineSettings.ChunkConfig.SizeY;
        //public static readonly int ZStep = EngineSettings.ChunkConfig.SizeY;

        public BlockData[] Blocks { get; set; }

        public BlockStorage()
        {
            Blocks = Helpers.CreateArray1D<BlockData>(EngineSettings.ChunkConfig.SizeX*EngineSettings.ChunkConfig.SizeZ*EngineSettings.ChunkConfig.SizeYTotal);
            //Blocks = Helpers.CreateArray1D<BlockData>(StrideX * StrideZ * EngineSettings.ChunkConfig.SizeY);
        }

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

        public void Set(BlockData[] data)
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

        //public static int BlockIndexByWorldPosition(int x, int z)
        //{
        //    int wrapX = x%StrideX;
        //    if (wrapX < 0)
        //        wrapX += StrideX;

        //    int wrapZ = z%StrideZ;
        //    if (wrapZ < 0)
        //        wrapZ += StrideZ;

        //    int index = wrapX * XStep + wrapZ * ZStep;
        //    return index;
        //}
    }
}
