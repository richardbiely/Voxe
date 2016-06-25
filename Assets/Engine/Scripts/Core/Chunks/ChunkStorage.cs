using System.Collections.Generic;
using Engine.Scripts.Common.DataTypes;

namespace Engine.Scripts.Core.Chunks
{
    public class ChunkStorage: IChunkStorage
    {
        private readonly Dictionary<Vector3Int, Chunk> m_chunks;

        public int Count
        {
            get { return m_chunks.Count; }
        }

        public ChunkStorage()
        {
            m_chunks = new Dictionary<Vector3Int, Chunk>();
        }

        public Chunk this[int x, int y, int z]
        {
            get
            {
                Chunk chunk;
                m_chunks.TryGetValue(new Vector3Int(x,y,z), out chunk);
                return chunk;
            }
            set
            {
                m_chunks.Add(new Vector3Int(x, y, z), value);
            }
        }

        public bool Check(int x, int y, int z)
        {
            return m_chunks.ContainsKey(new Vector3Int(x, y, z));
        }

        public void Remove(int x, int y, int z)
        {
            m_chunks.Remove(new Vector3Int(x, y, z));
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
