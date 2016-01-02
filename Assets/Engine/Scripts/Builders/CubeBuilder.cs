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

        #region Private methods

        public Rect GetTexture(int face)
        {
            return m_faceTextures[face];
        }

        #endregion Private methods

        #region Public Statics

        private static readonly CubeFace[] SFaces =
        {
            CubeFace.Front,
            CubeFace.Back,
            CubeFace.Right,
            CubeFace.Left,
            CubeFace.Top,
            CubeFace.Bottom
        };

        private static readonly Vector3Int[] SDirections =
        {
            Vector3Int.Back,
            Vector3Int.Forward,
            Vector3Int.Right,
            Vector3Int.Left,
            Vector3Int.Up,
            Vector3Int.Down
        };

        private static readonly Vector3[][] SVertices =
        {
            // Level of detail - normal
            //Front
            new[]
            {
                new Vector3(1.0f, 0.0f, 0.0f), new Vector3(1.0f, 1.0f, 0.0f), new Vector3(0.0f, 1.0f, 0.0f),
                new Vector3(0.0f, 0.0f, 0.0f)
            },
            //Back
            new[]
            {
                new Vector3(0.0f, 0.0f, 1.0f), new Vector3(0.0f, 1.0f, 1.0f), new Vector3(1.0f, 1.0f, 1.0f),
                new Vector3(1.0f, 0.0f, 1.0f)
            },
            //Right
            new[]
            {
                new Vector3(1.0f, 0.0f, 1.0f), new Vector3(1.0f, 1.0f, 1.0f), new Vector3(1.0f, 1.0f, 0.0f),
                new Vector3(1.0f, 0.0f, 0.0f)
            },
            //Left
            new[]
            {
                new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0.0f, 1.0f, 0.0f), new Vector3(0.0f, 1.0f, 1.0f),
                new Vector3(0.0f, 0.0f, 1.0f)
            },
            //Top
            new[]
            {
                new Vector3(1.0f, 1.0f, 0.0f), new Vector3(1.0f, 1.0f, 1.0f), new Vector3(0.0f, 1.0f, 1.0f),
                new Vector3(0.0f, 1.0f, 0.0f)
            },
            //Bottom
            new[]
            {
                new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0.0f, 0.0f, 1.0f), new Vector3(1.0f, 0.0f, 1.0f),
                new Vector3(1.0f, 0.0f, 0.0f)
            },
            // Level of detail - double size
            //Front
            new[]
            {
                new Vector3(2.0f, 0.0f, 0.0f), new Vector3(2.0f, 2.0f, 0.0f), new Vector3(0.0f, 2.0f, 0.0f),
                new Vector3(0.0f, 0.0f, 0.0f)
            },
            //Back
            new[]
            {
                new Vector3(0.0f, 0.0f, 2.0f), new Vector3(0.0f, 2.0f, 2.0f), new Vector3(2.0f, 2.0f, 2.0f),
                new Vector3(2.0f, 0.0f, 2.0f)
            },
            //Right
            new[]
            {
                new Vector3(2.0f, 0.0f, 2.0f), new Vector3(2.0f, 2.0f, 2.0f), new Vector3(2.0f, 2.0f, 0.0f),
                new Vector3(2.0f, 0.0f, 0.0f)
            },
            //Left
            new[]
            {
                new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0.0f, 2.0f, 0.0f), new Vector3(0.0f, 2.0f, 2.0f),
                new Vector3(0.0f, 0.0f, 2.0f)
            },
            //Top
            new[]
            {
                new Vector3(2.0f, 2.0f, 0.0f), new Vector3(2.0f, 2.0f, 2.0f), new Vector3(0.0f, 2.0f, 2.0f),
                new Vector3(0.0f, 2.0f, 0.0f)
            },
            //Bottom
            new[]
            {
                new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0.0f, 0.0f, 2.0f), new Vector3(2.0f, 0.0f, 2.0f),
                new Vector3(2.0f, 0.0f, 0.0f)
            },
            // Level of detail - quadruple size
            //Front
            new[]
            {
                new Vector3(4.0f, 0.0f, 0.0f), new Vector3(4.0f, 4.0f, 0.0f), new Vector3(0.0f, 4.0f, 0.0f),
                new Vector3(0.0f, 0.0f, 0.0f)
            },
            //Back
            new[]
            {
                new Vector3(0.0f, 0.0f, 4.0f), new Vector3(0.0f, 4.0f, 4.0f), new Vector3(4.0f, 4.0f, 4.0f),
                new Vector3(4.0f, 0.0f, 4.0f)
            },
            //Right
            new[]
            {
                new Vector3(4.0f, 0.0f, 4.0f), new Vector3(4.0f, 4.0f, 4.0f), new Vector3(4.0f, 4.0f, 0.0f),
                new Vector3(4.0f, 0.0f, 0.0f)
            },
            //Left
            new[]
            {
                new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0.0f, 4.0f, 0.0f), new Vector3(0.0f, 4.0f, 4.0f),
                new Vector3(0.0f, 0.0f, 4.0f)
            },
            //Top
            new[]
            {
                new Vector3(4.0f, 4.0f, 0.0f), new Vector3(4.0f, 4.0f, 4.0f), new Vector3(0.0f, 4.0f, 4.0f),
                new Vector3(0.0f, 4.0f, 0.0f)
            },
            //Bottom
            new[]
            {
                new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0.0f, 0.0f, 4.0f), new Vector3(4.0f, 0.0f, 4.0f),
                new Vector3(4.0f, 0.0f, 0.0f)
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

        public void Build(Map map, RenderBuffer targetBuffer, ref BlockData block, int face, bool backFace,
            ref Vector3[] vecs, ref Vector3Int worldPos)
        {
            if (worldPos.Y<0)
                return;

            int i = face;
            int iface = (int)SFaces[i];

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
            const int lodCurr = 1;

            float dmg = block.GetDamagePercent();
            Color32 color = BlockDatabase.GetBlockInfo(block.BlockType).Color;

            int lodShifted = lodCurr>>1;

            // Check neighbor blocks
            for (int i = 0; i<SFaces.Length; i++)
            {
                int yy = worldPos.Y+SDirections[i].Y*lodCurr;
                if (yy<0)
                    continue;

                int xx = worldPos.X+SDirections[i].X*lodCurr;
                int zz = worldPos.Z+SDirections[i].Z*lodCurr;

                int iface = (int)SFaces[i];

                // !TODO Only process all LOD block if LOD changes
                // No change in LOD, check just a single block
                //if (lodCurr==lodPrev)
                {
                    // Only need to build face if there is no solid geometry around
                    BlockData b = map.GetBlock(xx, yy, zz);
                    if (!b.IsAlpha())
                        continue;
                }
                /*else
                // LOD changed, some additional steps need to be done
                {
                    // Get neighbor block. Skip visible neighbors (no need to generate geometry for them)
                    bool foundAlpha = false;

                    // One face is not enough. Check remaining blocks
                    switch (i)
                    {
                        case 0:
                        case 1: // front, back
                            {
                                int minY = Mathf.Max(yy, 0);
                                int maxY = Mathf.Min(yy + lodCurr, EngineSettings.ChunkConfig.MaskYTotal);
                                for (int y = minY; y < maxY; y++)
                                {
                                    for (int x = xx; x < xx + lodCurr; x++)
                                    {
                                        // look for an alpha block
                                        BlockData b = map.GetBlock(x, y, zz);
                                        if (!b.IsAlpha())
                                            continue;

                                        y = maxY;
                                        foundAlpha = true;
                                        break;
                                    }
                                }
                                break;
                            }

                        case 2:
                        case 3: // left, right
                            {
                                int minY = Mathf.Max(yy, 0);
                                int maxY = Mathf.Min(yy + lodCurr, EngineSettings.ChunkConfig.MaskYTotal);
                                for (int y = minY; y < maxY; y++)
                                {
                                    for (int z = zz; z < zz + lodCurr; z++)
                                    {
                                        // look for an alpha block
                                        BlockData b = map.GetBlock(xx, y, z);
                                        if (!b.IsAlpha())
                                            continue;

                                        y = maxY;
                                        foundAlpha = true;
                                        break;
                                    }
                                }
                                break;
                            }

                        case 4:
                        case 5: // top, bottom
                            {
                                for (int z = zz; z < zz + lodCurr; z++)
                                {
                                    for (int x = xx; x < xx + lodCurr; x++)
                                    {
                                        // look for an alpha block
                                        BlockData b = map.GetBlock(x, yy, z);
                                        if (!b.IsAlpha())
                                            continue;

                                        z = zz + lodCurr;
                                        foundAlpha = true;
                                        break;
                                    }
                                }
                                break;
                            }
                    }

                    // No air found, do not generate a face
                    if (!foundAlpha)
                        continue;
                }*/

                // Fill buffers with data
                //lock (targetBuffer)
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
                    pom = lodShifted*6+i;
                    targetBuffer.Positions.Add(new Vector3(SVertices[pom][0].x+localPos.X,
                                                           SVertices[pom][0].y+localPos.Y,
                                                           SVertices[pom][0].z+localPos.Z));
                    targetBuffer.Positions.Add(new Vector3(SVertices[pom][1].x+localPos.X,
                                                           SVertices[pom][1].y+localPos.Y,
                                                           SVertices[pom][1].z+localPos.Z));
                    targetBuffer.Positions.Add(new Vector3(SVertices[pom][2].x+localPos.X,
                                                           SVertices[pom][2].y+localPos.Y,
                                                           SVertices[pom][2].z+localPos.Z));
                    targetBuffer.Positions.Add(new Vector3(SVertices[pom][3].x+localPos.X,
                                                           SVertices[pom][3].y+localPos.Y,
                                                           SVertices[pom][3].z+localPos.Z));

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