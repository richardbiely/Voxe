using Assets.Engine.Scripts.Core.Chunks;
using Assets.Engine.Scripts.Generators;

namespace Assets.Engine.Scripts.Provider
{
    /// <summary>
    /// 	Interface for chunk providers
    /// </summary>
    public interface IChunkProvider
    {
        IChunkGenerator GetGenerator();

        Chunk RequestChunk(int cx, int cz, int lod);
        bool ReleaseChunk(Chunk chunk);
        
    }
}