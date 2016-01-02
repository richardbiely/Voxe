using Assets.Engine.Scripts.Core.Chunks;

namespace Assets.Engine.Scripts.Generators
{
    /// <summary>
    /// Base interface for chunk generators.
    /// </summary>
    public interface IChunkGenerator
    {
        /// <summary>
        /// Generate the specified chunk.
        /// </summary>
        void Generate (Chunk chunk);
    }
}