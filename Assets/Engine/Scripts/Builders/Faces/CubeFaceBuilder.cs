using System;
using System.Collections.Generic;
using Assets.Engine.Scripts.Atlas;
using Assets.Engine.Scripts.Common.Extensions;
using Assets.Engine.Scripts.Core;
using Assets.Engine.Scripts.Core.Blocks;
using Assets.Engine.Scripts.Rendering;
using Assets.Engine.Scripts.Utils;
using UnityEngine;
using RenderBuffer = Assets.Engine.Scripts.Rendering.RenderBuffer;

namespace Assets.Engine.Scripts.Builders.Faces
{
    /// <summary>
    ///     Builds solid cubic blocks
    /// </summary>
    public class CubeFaceBuilder: IFaceBuilder
    {
        #region Private vars
        
        private readonly Rect[] m_faceTextures;

        #endregion Private vars
        
        #region Constructor

        public CubeFaceBuilder(IList<BlockTexture> textures)
        {
            m_faceTextures = new Rect[textures.Count];
            for (int i = 0; i<textures.Count; i++)
            {
                m_faceTextures[i] = TextureAtlas.GetRectangle((int)textures[i]);
            }
        }

        #endregion Constructor
        
        #region Public Statics
      
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

        public void Build(RenderBuffer targetBuffer, ref BlockData block, BlockFace face, bool backFace, ref Vector3[] vecs, LocalPools pools)
        {
            int iface = (int)face;
            Color32 color = BlockDatabase.GetBlockInfo(block.BlockType).Color;

            targetBuffer.AddIndices(backFace);

            VertexData[] vertexData;
            pools.PopVertexDataArray(4, out vertexData);

            for (int i = 0; i<4; i++)
            {
                VertexData data = vertexData[i] ?? new VertexData();
                data.Vertex = vecs[i];
                data.Color = color;
                data.UV = AddFaceUV(i, GetTexture(iface), backFace);
                data.Normal = SNormals[iface][i];

                targetBuffer.Vertices.Add(data);
            }

            pools.PushVertexDataArray(ref vertexData);
        }

        /// <summary>
        ///     Adds the face UVs.
        /// </summary>
        private static Vector2 AddFaceUV(int vertex, Rect texCoords, bool backFace)
        {
            // 0--1
            // |  |
            // |  |
            // 3--2 --> 0, 1, 3, 2
            switch (vertex)
            {
                case 0: return new Vector2(texCoords.xMax, 1f-texCoords.yMax);
                case 1: return new Vector2(texCoords.xMax, 1f-texCoords.yMin);
                case 2: return new Vector2(texCoords.xMin, 1f-texCoords.yMin);
                case 3: return new Vector2(texCoords.xMin, 1f-texCoords.yMax);
            }

            return Vector2.zero;
        }

        #endregion IBlockBuilder implementation
    }
}