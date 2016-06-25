using System.Collections.Generic;

namespace Engine.Scripts.Core.Chunks
{
    public interface IChunkStorage
    {
        Chunk this[int x, int y, int z] { get; set; }

        bool Check(int x, int y, int z);
        void Remove(int x, int y, int z);

        IEnumerable<Chunk> Values { get; }
    }
}
