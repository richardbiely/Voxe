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
            Vector3[] vertices;
            Vector2[] uvs;
            Color32[] colors;
            Vector3[] normals;
            Vector4[] tangents;

            // Avoid allocations by retrieving buffers from the pool
            Globals.Pools.PopVector3Array(size, out vertices);
            Globals.Pools.PopVector2Array(size, out uvs);
            Globals.Pools.PopColor32Array(size, out colors);
            Globals.Pools.PopVector3Array(size, out normals);
            Globals.Pools.PopVector4Array(size, out tangents);

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
            Globals.Pools.PushVector3Array(ref vertices);
            Globals.Pools.PushVector2Array(ref uvs);
            Globals.Pools.PushColor32Array(ref colors);
            Globals.Pools.PushVector3Array(ref normals);
            Globals.Pools.PushVector4Array(ref tangents);
        }

        #endregion
    }
}
