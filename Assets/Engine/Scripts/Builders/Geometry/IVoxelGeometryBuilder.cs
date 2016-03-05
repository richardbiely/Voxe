using Assets.Engine.Scripts.Core;
using Assets.Engine.Scripts.Core.Chunks;
using RenderBuffer = Assets.Engine.Scripts.Rendering.RenderBuffer;

namespace Assets.Engine.Scripts.Builders.Geometry
{
    public interface IVoxelGeometryBuilder
    {
        void BuildMesh(
            Map map, RenderBuffer renderBuffer, int offsetX, int offsetY, int offsetZ,
            int minX, int maxX, int minY, int maxY, int minZ, int maxZ, int lod,
            GlobalPools pools
            );
    }
}
