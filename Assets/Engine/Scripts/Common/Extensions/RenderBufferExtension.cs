using Assets.Engine.Scripts.Rendering;
using UnityEngine;
using RenderBuffer = Assets.Engine.Scripts.Rendering.RenderBuffer;

namespace Assets.Engine.Scripts.Common.Extensions
{
    public static class RenderBufferExtension
    {
        /// <summary>
        ///     Adds triangle indices for a quad
        /// </summary>
        public static void AddIndices(this RenderBuffer target, bool backFace)
        {
            int offset = target.Vertices.Count;

            // 0--1
            // |\ |
            // | \|
            // 3--2

            if (backFace)
            {
                target.Triangles.Add(offset + 2);
                target.Triangles.Add(offset + 0);
                target.Triangles.Add(offset + 1);

                target.Triangles.Add(offset + 3);
                target.Triangles.Add(offset + 0);
                target.Triangles.Add(offset + 2);
            }
            else
            {
                target.Triangles.Add(offset + 2);
                target.Triangles.Add(offset + 1);
                target.Triangles.Add(offset + 0);

                target.Triangles.Add(offset + 3);
                target.Triangles.Add(offset + 2);
                target.Triangles.Add(offset + 0);
            }
        }

        /// <summary>
        ///     Adds the vertices to the render buffer.
        /// </summary>
        public static void AddVertices(this RenderBuffer target, ref VertexData[] vertices)
        {
            target.Vertices.AddRange(vertices);
        }
    }
}
