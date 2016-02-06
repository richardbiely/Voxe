namespace Assets.Engine.Scripts.Core.Blocks
{
    /// <summary>
    /// Storage for blocks
    /// </summary>
    public interface IBlockStorage
    {
        BlockData this[int index] { get; set; }
        BlockData this[int x, int y, int z] { get; set; }

        void Set(BlockData[] data);

        void Reset();
        BlockData[] ToArray();
    }
}
