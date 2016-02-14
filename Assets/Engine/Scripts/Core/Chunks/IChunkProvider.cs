using Assets.Engine.Scripts.Core.Chunks;

namespace Assets.Engine.Scripts.Provider
{
    /// <summary>
    /// 	Interface for chunk providers
    /// </summary>
    public interface IChunkProvider
    {
        Chunk RequestChunk(int cx, int cz, int lod);
        bool ReleaseChunk(Chunk chunk);
        
    }
}