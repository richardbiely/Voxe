using System.Collections.Generic;
using Assets.Engine.Scripts.Builders;
using UnityEngine;

namespace Assets.Engine.Scripts.Rendering
{
    /// <summary>
    ///     A simple intermediate container for mesh data
    /// </summary>
    public class RenderBuffer
    {
        private readonly IMeshBuilder m_meshBuilder;

        public readonly List<Vector3> Vertices = new List<Vector3>();
        public readonly List<Vector3> Normals = new List<Vector3>();
        public readonly List<Vector2> UV1 = new List<Vector2>();
        public readonly List<Vector2> UV2 = new List<Vector2>();
        public readonly List<Color32> Colors = new List<Color32>();
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
            Normals.Clear();
            UV1.Clear();
            UV2.Clear();
            Colors.Clear();
            Triangles.Clear();
        }

        public bool IsEmpty()
        {
            return (Vertices.Count <= 0);
        }

        public void Combine(RenderBuffer renderBuffer)
        {
            Vertices.AddRange(renderBuffer.Vertices);
            Normals.AddRange(renderBuffer.Normals);
            UV1.AddRange(renderBuffer.UV1);
            UV2.AddRange(renderBuffer.UV2);
            Colors.AddRange(renderBuffer.Colors);
            Triangles.AddRange(renderBuffer.Triangles);
        }

        public void BuildMesh(Mesh mesh)
        {
            m_meshBuilder.BuildMesh(mesh, this);
        }
    }
}