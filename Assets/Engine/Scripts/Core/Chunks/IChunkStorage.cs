using System.Collections.Generic;

namespace Assets.Engine.Scripts.Core.Chunks
{
    interface IChunkStorage
    {
        Chunk this[int x, int z] { get; set; }

        bool Check(int x, int z);
        void Remove(int x, int z);

        IEnumerable<Chunk> Values { get; }
    }
}
