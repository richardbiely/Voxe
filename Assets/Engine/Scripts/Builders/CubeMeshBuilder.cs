//using Assets.Engine.Scripts.Common.Extensions;
using UnityEngine;
using RenderBuffer = Assets.Engine.Scripts.Rendering.RenderBuffer;

namespace Assets.Engine.Scripts.Builders
{
    public class CubeMeshBuilder: IMeshBuilder
    {
        #region IMeshBuilder implementation

        /// <summary>
        ///     Copy the data to a Unity mesh
        /// </summary>
        public void BuildMesh(Mesh mesh, RenderBuffer buffer)
        {
            // !TODO Need to figure out how to do this without ToArray because
            // !TODO it results in too many memory allocations (GC performance hit)
            mesh.vertices = buffer.Positions.ToArray();
            mesh.uv = buffer.UV1.ToArray();
            mesh.uv2 = buffer.UV2.ToArray();
            mesh.colors32 = buffer.Colors.ToArray();
            mesh.normals = buffer.Normals.ToArray();
            //MeshExtensions.GenerateTangents(mesh);
            mesh.triangles = buffer.Triangles.ToArray();

            mesh.Optimize();
        }

        #endregion
    }
}
