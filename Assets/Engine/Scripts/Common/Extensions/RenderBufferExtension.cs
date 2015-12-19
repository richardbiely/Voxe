using Assets.Engine.Scripts.Common.DataTypes;
using UnityEngine;
using RenderBuffer = Assets.Engine.Scripts.Rendering.RenderBuffer;

namespace Assets.Engine.Scripts.Common.Extensions
{
    public static class RenderBufferExtension
    {
        /// <summary>
        ///     Adds triangle indices for a quad
        /// </summary>
        public static void AddFaceIndices(this RenderBuffer target, bool backFace)
        {
            int offset = target.Positions.Count;

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
        public static void AddVertices(this RenderBuffer target, ref Vector3[] vertices, ref Vector3Int offset)
        {
            for (int i = 0; i < vertices.Length; i++)
            {
                target.Positions.Add(
                    new Vector3(vertices[i].x + offset.X, vertices[i].y + offset.Y, vertices[i].z + offset.Z)
                    );
            }
        }

        public static void AddVertices(this RenderBuffer target, ref Vector3[] vertices)
        {
            target.Positions.AddRange(vertices);
        }

        /// <summary>
        ///     Adds the face colors.
        /// </summary>
        public static void AddFaceColors(this RenderBuffer target, ref Color32 color)
        {
            target.Colors.Add(color);
            target.Colors.Add(color);
            target.Colors.Add(color);
            target.Colors.Add(color);
        }

        /// <summary>
        ///     Adds the face UVs.
        /// </summary>
        public static void AddFaceUv(this RenderBuffer target, Rect texCoords)
        {
            target.UVs.Add(new Vector2(texCoords.xMax, 1f - texCoords.yMax));
            target.UVs.Add(new Vector2(texCoords.xMax, 1f - texCoords.yMin));
            target.UVs.Add(new Vector2(texCoords.xMin, 1f - texCoords.yMin));
            target.UVs.Add(new Vector2(texCoords.xMin, 1f - texCoords.yMax));
        }

        public const int DamageFrames = 10;
        // frames of damage. First frame is no damage, frame 1 is minimum damage and 10 is maximum damage
        public const float DamageMultiplier = 1f / DamageFrames;

        /// <summary>
        ///     Adds secondary UVs for showing damage.
        /// </summary>
        public static void AddDamageUVs(this RenderBuffer target, float damage)
        {
            damage *= (DamageFrames - 1);
            int dmgFrame = Mathf.FloorToInt(damage);
            Rect texCoords = new Rect(dmgFrame * DamageMultiplier, 0f, DamageMultiplier, 1f);

            target.UV2.Add(new Vector2(texCoords.xMax, 1f - texCoords.yMin));
            target.UV2.Add(new Vector2(texCoords.xMax, 1f - texCoords.yMax));
            target.UV2.Add(new Vector2(texCoords.xMin, 1f - texCoords.yMax));
            target.UV2.Add(new Vector2(texCoords.xMin, 1f - texCoords.yMin));
        }

        public static void AddNormals(this RenderBuffer target, ref Vector3[] normals)
        {
            target.Normals.AddRange(normals);
        }
    }
}
