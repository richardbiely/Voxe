using System;
using System.Collections.Generic;
using Engine.Plugins.CoherentNoise.Scripts.Generation;
using Engine.Scripts.Core.Blocks;
using Engine.Scripts.Core.Pooling;
using Engine.Scripts.Rendering;
using Engine.Scripts.Rendering.Batchers;
using Engine.Scripts.Utils;
using UnityEngine;

namespace Engine.Scripts.Builders.Faces
{
    /// <summary>
    ///     Builds solid cubic blocks
    /// </summary>
    public class CubeFaceBuilder: IFaceBuilder
    {
        #region Private vars
        
        private readonly Rect[] m_faceTextures;
        private readonly ValueNoise m_noise = new ValueNoise(0);

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

        public void Build(RenderGeometryBatcher batcher, ref BlockData block, BlockFace face, bool backFace, ref Vector3[] vecs, LocalPools pools)
        {
            int iface = (int)face;

            // Randomize the color a bit
            Color32 color = BlockDatabase.GetBlockInfo(block.BlockType).Color;

            if (block.BlockType!=BlockType.None)
            {
                float value = m_noise.GetValue(vecs[0]+vecs[1]+vecs[2]+vecs[3]); // -1.0f..1.0f
                float noise = (255.0f*value)*0.02f; // Deviation of 0.02f points from the original
                int n = (int)noise;
                byte r = (byte)Math.Max(0, Math.Min(color.r+n, 255));
                byte g = (byte)Math.Max(0, Math.Min(color.g+n, 255));
                byte b = (byte)Math.Max(0, Math.Min(color.b+n, 255));
                color = new Color32(r, g, b, color.a);
            }
            
            VertexData[] vertexData = pools.PopVertexDataArray(4);
            VertexDataFixed[] vertexDataFixed = pools.PopVertexDataFixedArray(4);
            {
                for (int i = 0; i < 4; i++)
                    vertexData[i] = pools.PopVertexData();

                for (int i = 0; i<4; i++)
                {
                    vertexData[i].Vertex = vecs[i];
                    vertexData[i].Color = color;
                    vertexData[i].UV = AddFaceUV(i, GetTexture(iface), backFace);
                    vertexData[i].Normal = SNormals[iface][i];
                }

                for (int i = 0; i < 4; i++)
                    vertexDataFixed[i] = VertexDataUtils.ClassToStruct(vertexData[i]);
                batcher.AddFace(vertexDataFixed, backFace);

                for (int i = 0; i < 4; i++)
                    pools.PushVertexData(vertexData[i]);
            }
            pools.PushVertexDataFixedArray(vertexDataFixed);
            pools.PushVertexDataArray(vertexData);
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