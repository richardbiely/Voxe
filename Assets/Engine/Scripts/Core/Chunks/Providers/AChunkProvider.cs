using Engine.Scripts.Core.Chunks.Managers;
using UnityEngine;

namespace Engine.Scripts.Core.Chunks.Providers
{
    public abstract class AChunkProvider: MonoBehaviour, IChunkProvider
    {
        public virtual Chunk RequestChunk(ChunkManager map, int cx, int cy, int cz)
        {
            throw new System.NotImplementedException();
        }

        public virtual bool ReleaseChunk(Chunk chunk)
        {
            throw new System.NotImplementedException();
        }
    }
}
