namespace Assets.Engine.Scripts.Core.Chunks
{
    public interface IChunkManager
    {
        Chunk GetChunk(int cx, int cy, int cz);
    }
}
