using Assets.Engine.Scripts.Core;
using Assets.Engine.Scripts.Core.Chunks;
using Assets.Engine.Scripts.Rendering;

namespace Assets.Engine.Scripts.Builders.Geometry
{
    public interface IVoxelGeometryBuilder
    {
        void BuildMesh(
            Map map, DrawCallBatcher batcher, int offsetX, int offsetY, int offsetZ,
            int minX, int maxX, int minY, int maxY, int minZ, int maxZ, int lod,
            LocalPools pools
            );
    }
}
