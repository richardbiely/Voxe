using System.Collections.Generic;

namespace Assets.Engine.Scripts.Core.Chunks
{
    public class ChunkStorage: IChunkStorage
    {
        private static readonly long Stride = 1000000;//int.MaxValue;

        private readonly Dictionary<long, Chunk> m_chunks;

        public ChunkStorage()
        {
            m_chunks = new Dictionary<long, Chunk>();
        }

        public Chunk this[int x, int z]
        {
            get
            {
                Chunk chunk;
                long pos = x + z* Stride;
                m_chunks.TryGetValue(pos, out chunk);
                return chunk;
            }
            set
            {
                long pos = x + z * Stride;
                m_chunks.Add(pos, value);
            }
        }

        public bool Check(int x, int z)
        {
            long pos = x + z * Stride;
            return m_chunks.ContainsKey(pos);
        }

        public void Remove(int x, int z)
        {
            long pos = x + z * Stride;
            m_chunks.Remove(pos);
        }

        public IEnumerable<Chunk> Values
        {
            get
            {
                return m_chunks.Values;
            }

        }
    }
}
