using System.Collections.Generic;
using Assets.Engine.Scripts.Builders.Mesh;
using Assets.Engine.Scripts.Core;
using UnityEngine;

namespace Assets.Engine.Scripts.Rendering
{
    /// <summary>
    ///     A simple intermediate container for mesh data
    /// </summary>
    public class RenderBuffer
    {
        private readonly IMeshBuilder m_meshBuilder;

        public readonly List<VertexData> Vertices = new List<VertexData>();
        public readonly List<int> Triangles = new List<int>();

        public RenderBuffer(IMeshBuilder builder)
        {
            m_meshBuilder = builder;
        }

        /// <summary>
        ///     Clear the render buffer
        /// </summary>
        public void Clear()
        {
            Vertices.Clear();
            Triangles.Clear();
        }

        public bool IsEmpty()
        {
            return (Vertices.Count <= 0);
        }

        public void Combine(RenderBuffer renderBuffer)
        {
            Vertices.AddRange(renderBuffer.Vertices);
            Triangles.AddRange(renderBuffer.Triangles);
        }

        public void BuildMesh(Mesh mesh)
        {
            m_meshBuilder.BuildMesh(mesh, this);
        }
    }
}