using System.Collections.Generic;
using Assets.Engine.Scripts.Atlas;
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

        public void Build(RenderBuffer targetBuffer, ref BlockData block, BlockFace face, bool backFace,
            ref Vector3[] vecs)
        {
            int iface = (int)face;

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

        #endregion IBlockBuilder implementation
    }
}