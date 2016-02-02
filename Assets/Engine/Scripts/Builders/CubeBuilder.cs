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

        #endregion IBlockBuilder implementation
    }
}