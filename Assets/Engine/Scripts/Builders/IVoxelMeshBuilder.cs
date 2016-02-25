using Assets.Engine.Scripts.Core;
using RenderBuffer = Assets.Engine.Scripts.Rendering.RenderBuffer;

namespace Assets.Engine.Scripts.Builders
{
    public interface IVoxelMeshBuilder
    {
        void BuildMesh(
            Map map, RenderBuffer renderBuffer, int offsetX, int offsetY, int offsetZ,
            int minX, int maxX, int minY, int maxY, int minZ, int maxZ, int lod
            );
    }
}
