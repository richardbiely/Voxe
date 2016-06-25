namespace Engine.Scripts.Core.Chunks.Managers
{
    public interface IChunkManager
    {
        Chunk GetChunk(int cx, int cy, int cz);
    }
}
