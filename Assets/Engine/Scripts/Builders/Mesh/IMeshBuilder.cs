using RenderBuffer = Assets.Engine.Scripts.Rendering.RenderBuffer;

namespace Assets.Engine.Scripts.Builders.Mesh
{
    public interface IMeshBuilder
    {
        void BuildMesh(UnityEngine.Mesh mesh, RenderBuffer buffer);
    }
}
