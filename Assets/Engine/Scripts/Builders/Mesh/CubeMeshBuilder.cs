using Assets.Engine.Scripts.Core;
using Assets.Engine.Scripts.Rendering;
using UnityEngine;
using RenderBuffer = Assets.Engine.Scripts.Rendering.RenderBuffer;

namespace Assets.Engine.Scripts.Builders.Mesh
{
    public class CubeMeshBuilder: IMeshBuilder
    {
        #region IMeshBuilder implementation

        /// <summary>
        ///     Copy the data to a Unity mesh
        /// </summary>
        public void BuildMesh(UnityEngine.Mesh mesh, RenderBuffer buffer)
        {
            int size = buffer.Vertices.Count;

            // Avoid allocations by retrieving buffers from the pool
            Vector3[] vertices = Globals.Pools.PopVector3Array(size);
            Vector2[] uvs = Globals.Pools.PopVector2Array(size);
            Color32[] colors = Globals.Pools.PopColor32Array(size);
            Vector3[] normals = Globals.Pools.PopVector3Array(size);
            Vector4[] tangents = Globals.Pools.PopVector4Array(size);

            // Fill buffers with data
            for (int i = 0; i<size; i++)
            {
                VertexData vertexData = buffer.Vertices[i];
                vertices[i] = vertexData.Vertex;
                uvs[i] = vertexData.UV;
                colors[i] = vertexData.Color;
                normals[i] = vertexData.Normal;
                tangents[i] = vertexData.Tangent;
            }

            // Due to the way the memory pools work we might have received more
            // data than necessary. This little overhead is well worth it, though.
            // Fill unused data with "zeroes"
            for (int i = size; i<vertices.Length; i++)
            {
                vertices[i] = Vector3.zero;
                uvs[i] = Vector2.zero;
                colors[i] = Color.clear;
                normals[i] = Vector3.zero;
                tangents[i] = Vector4.zero;
            }

            // Prepare mesh
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.colors32 = colors;
            mesh.normals = normals;
            mesh.tangents = tangents;
            mesh.SetTriangles(buffer.Triangles, 0);
            mesh.Optimize();

            // Return memory back to pool
            Globals.Pools.PushVector3Array(vertices);
            Globals.Pools.PushVector2Array(uvs);
            Globals.Pools.PushColor32Array(colors);
            Globals.Pools.PushVector3Array(normals);
            Globals.Pools.PushVector4Array(tangents);
        }

        #endregion
    }
}
