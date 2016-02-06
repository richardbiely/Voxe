using UnityEngine;
using RenderBuffer = Assets.Engine.Scripts.Rendering.RenderBuffer;

namespace Assets.Engine.Scripts.Builders
{
    public interface IMeshBuilder
    {
        void BuildMesh(Mesh mesh, RenderBuffer buffer);
    }
}
