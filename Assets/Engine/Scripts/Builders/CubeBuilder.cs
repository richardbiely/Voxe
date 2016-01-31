using System.Collections.Generic;
using Assets.Engine.Scripts.Atlas;
using Assets.Engine.Scripts.Common.DataTypes;
using Assets.Engine.Scripts.Common.Extensions;
using Assets.Engine.Scripts.Core;
using Assets.Engine.Scripts.Core.Blocks;
using Assets.Engine.Scripts.Utils;
using UnityEngine;
using RenderBuffer = Assets.Engine.Scripts.Rendering.RenderBuffer;

namespace Assets.Engine.Scripts.Builders
{
    /// <summary>
    ///     Builds solid cubic blocks
    /// </summary>
    public class CubeBuilder: IBlockBuilder
    {
        #region Private vars

        private readonly Rect[] m_faceTextures;

        #endregion Private vars

        #region Constructor

        public CubeBuilder(IList<BlockTexture> textures)
        {
            m_faceTextures = new Rect[textures.Count];
            for (int i = 0; i<textures.Count; i++)
            {
                m_faceTextures[i] = AtlasUtils.GetRectangle((int)textures[i]);
            }
        }

        #endregion Constructor
        
        #region Public Statics

        public static readonly CubeFace[] SFaces =
        {
            CubeFace.Front,
            CubeFace.Back,
            CubeFace.Right,
            CubeFace.Left,
            CubeFace.Top,
            CubeFace.Bottom
        };

        public static readonly Vector3Int[] SDirections =
        {
            Vector3Int.Back,
            Vector3Int.Forward,
            Vector3Int.Right,
            Vector3Int.Left,
            Vector3Int.Up,
            Vector3Int.Down
        };

        public static readonly Vector3[][] SVertices =
        {
            //Front
            new[]
            {
                new Vector3(1.0f, 0.0f, 0.0f), new Vector3(1.0f, 1.0f, 0.0f),
                new Vector3(0.0f, 1.0f, 0.0f), new Vector3(0.0f, 0.0f, 0.0f)
            },
            //Back
            new[]
            {
                new Vector3(0.0f, 0.0f, 1.0f), new Vector3(0.0f, 1.0f, 1.0f),
                new Vector3(1.0f, 1.0f, 1.0f), new Vector3(1.0f, 0.0f, 1.0f)
            },
            //Right
            new[]
            {
                new Vector3(1.0f, 0.0f, 1.0f), new Vector3(1.0f, 1.0f, 1.0f),
                new Vector3(1.0f, 1.0f, 0.0f), new Vector3(1.0f, 0.0f, 0.0f)
            },
            //Left
            new[]
            {
                new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0.0f, 1.0f, 0.0f),
                new Vector3(0.0f, 1.0f, 1.0f), new Vector3(0.0f, 0.0f, 1.0f)
            },
            //Top
            new[]
            {
                new Vector3(1.0f, 1.0f, 0.0f), new Vector3(1.0f, 1.0f, 1.0f),
                new Vector3(0.0f, 1.0f, 1.0f), new Vector3(0.0f, 1.0f, 0.0f)
            },
            //Bottom
            new[]
            {
                new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0.0f, 0.0f, 1.0f),
                new Vector3(1.0f, 0.0f, 1.0f), new Vector3(1.0f, 0.0f, 0.0f)
            }
        };

        private static readonly Vector3[][] SNormals =
        {
            new[]
            {
                Vector3.forward, Vector3.forward, Vector3.forward, Vector3.forward
            },
            new[]
            {
                Vector3.back, Vector3.back, Vector3.back, Vector3.back
            },
            new[]
            {
                Vector3.right, Vector3.right, Vector3.right, Vector3.right
            },
            new[]
            {
                Vector3.left, Vector3.left, Vector3.left, Vector3.left
            },
            new[]
            {
                Vector3.up, Vector3.up, Vector3.up, Vector3.up
            },
            new[]
            {
                Vector3.down, Vector3.down, Vector3.down, Vector3.down
            }
        };

        #endregion Public Statics

        #region IBlockBuilder implementation

        public Rect GetTexture(int face)
        {
            return m_faceTextures[face];
        }

        public void Build(Map map, RenderBuffer targetBuffer, ref BlockData block, int face, bool backFace,
            ref Vector3[] vecs)
        {
            int iface = (int)SFaces[face];

            float dmg = block.GetDamagePercent();
            Color32 color = BlockDatabase.GetBlockInfo(block.BlockType).Color;

            // Empty block found, create a face
            targetBuffer.AddFaceIndices(backFace);
            targetBuffer.AddVertices(ref vecs);
            targetBuffer.AddFaceColors(ref color);
            targetBuffer.AddFaceUv(GetTexture(iface));
            targetBuffer.AddDamageUVs(dmg);
            targetBuffer.AddNormals(ref SNormals[iface]);
        }

        public void Build(Map map, RenderBuffer targetBuffer, ref BlockData block, ref Vector3Int worldPos,
            ref Vector3Int localPos)
        {
            float dmg = block.GetDamagePercent();
            Color32 color = BlockDatabase.GetBlockInfo(block.BlockType).Color;
            
            for (int i = 0; i<SFaces.Length; i++)
            {
                int yy = worldPos.Y+SDirections[i].Y;
                if (yy<0)
                    continue; // Nothing to do at the bottom of the world

                int xx = worldPos.X+SDirections[i].X;
                int zz = worldPos.Z+SDirections[i].Z;
                
                // Building of faces is only necessary if there's solid geometry around
                BlockData b = map.GetBlock(xx, yy, zz);
                if (!b.IsEmpty())
                    continue;

                int iface = (int)SFaces[i];

                // Fill buffers with data
                {
                    // Add indices
                    int pom = targetBuffer.Positions.Count;
                    targetBuffer.Triangles.Add(pom+2);
                    targetBuffer.Triangles.Add(pom+1);
                    targetBuffer.Triangles.Add(pom+0);
                    targetBuffer.Triangles.Add(pom+3);
                    targetBuffer.Triangles.Add(pom+2);
                    targetBuffer.Triangles.Add(pom+0);

                    // Add vertices
                    targetBuffer.Positions.Add(new Vector3(SVertices[i][0].x+localPos.X,
                                                           SVertices[i][0].y+localPos.Y,
                                                           SVertices[i][0].z+localPos.Z));
                    targetBuffer.Positions.Add(new Vector3(SVertices[i][1].x+localPos.X,
                                                           SVertices[i][1].y+localPos.Y,
                                                           SVertices[i][1].z+localPos.Z));
                    targetBuffer.Positions.Add(new Vector3(SVertices[i][2].x+localPos.X,
                                                           SVertices[i][2].y+localPos.Y,
                                                           SVertices[i][2].z+localPos.Z));
                    targetBuffer.Positions.Add(new Vector3(SVertices[i][3].x+localPos.X,
                                                           SVertices[i][3].y+localPos.Y,
                                                           SVertices[i][3].z+localPos.Z));

                    // Add colors
                    targetBuffer.Colors.Add(color);
                    targetBuffer.Colors.Add(color);
                    targetBuffer.Colors.Add(color);
                    targetBuffer.Colors.Add(color);

                    // Add UVs
                    Rect texCoords = GetTexture(iface);
                    targetBuffer.UVs.Add(new Vector2(texCoords.xMax, 1f-texCoords.yMax));
                    targetBuffer.UVs.Add(new Vector2(texCoords.xMax, 1f-texCoords.yMin));
                    targetBuffer.UVs.Add(new Vector2(texCoords.xMin, 1f-texCoords.yMin));
                    targetBuffer.UVs.Add(new Vector2(texCoords.xMin, 1f-texCoords.yMax));

                    // Add damage UVs
                    float damage = dmg*(RenderBufferExtension.DamageFrames-1);
                    int dmgFrame = Mathf.FloorToInt(damage);
                    texCoords = new Rect(dmgFrame*RenderBufferExtension.DamageMultiplier, 0f,
                                         RenderBufferExtension.DamageMultiplier, 1f);
                    targetBuffer.UV2.Add(new Vector2(texCoords.xMax, 1f-texCoords.yMin));
                    targetBuffer.UV2.Add(new Vector2(texCoords.xMax, 1f-texCoords.yMax));
                    targetBuffer.UV2.Add(new Vector2(texCoords.xMin, 1f-texCoords.yMax));
                    targetBuffer.UV2.Add(new Vector2(texCoords.xMin, 1f-texCoords.yMin));

                    // Add normals
                    targetBuffer.Normals.AddRange(SNormals[iface]);
                }
            }
        }

        #endregion IBlockBuilder implementation
    }
}