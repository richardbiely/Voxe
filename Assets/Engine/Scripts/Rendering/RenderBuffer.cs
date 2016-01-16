using System.Collections.Generic;
using Assets.Engine.Scripts.Common.Extensions;
using UnityEngine;

namespace Assets.Engine.Scripts.Rendering
{
    /// <summary>
    ///     A simple intermediate container for mesh data
    /// </summary>
    public class RenderBuffer
    {
        public readonly List<Vector3> Positions = new List<Vector3>();
        public readonly List<Vector3> Normals = new List<Vector3>();
        public readonly List<Vector2> UVs = new List<Vector2>();
        public readonly List<Vector2> UV2 = new List<Vector2>();
        public readonly List<int> Triangles = new List<int>();
        public readonly List<Color32> Colors = new List<Color32>();

        /// <summary>
        ///     Clear the render buffer
        /// </summary>
        public void Clear()
        {
            Positions.Clear();
            Normals.Clear();
            UVs.Clear();
            UV2.Clear();
            Colors.Clear();
            Triangles.Clear();
        }

        public bool IsEmpty()
        {
            return (Positions.Count <= 0);
        }

        /// <summary>
        ///     Copy the data to a Unity Mesh
        /// </summary>
        public void CopyToMesh(Mesh mesh, bool doTangents)
        {
            // !TODO Need to figure out how to do this without ToArray because
            // !TODO it results in too many memory allocations (GC performance hit)
            mesh.vertices = Positions.ToArray();                                
            mesh.uv = UVs.ToArray();
            mesh.uv2 = UV2.ToArray();
            mesh.colors32 = Colors.ToArray();
            mesh.normals = Normals.ToArray();
            if(doTangents)
                MeshExtensions.GenerateTangents(mesh);
            mesh.triangles = Triangles.ToArray();

            mesh.Optimize();
        }
    }
}