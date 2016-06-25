
using Engine.Scripts.Rendering;

namespace Engine.Scripts.Builders.Mesh
{
    public interface IMeshGeometryBuilder
    {
        void BuildMesh(UnityEngine.Mesh mesh, GeometryBuffer buffer);
    }
}