using Assets.Engine.Scripts.Core.Chunks;

namespace Assets.Engine.Scripts.Generators
{
    /// <summary>
    /// Base interface for chunk generators.
    /// </summary>
    public interface IMiniChunkGenerator
    {
        /// <summary>
        /// Generate the specified chunk.
        /// </summary>
        void Generate (MiniChunk chunk);
    }
}