using Assets.Engine.Scripts.Common.DataTypes;
using Assets.Engine.Scripts.Core;
using Assets.Engine.Scripts.Core.Blocks;
using UnityEngine;
using RenderBuffer = Assets.Engine.Scripts.Rendering.RenderBuffer;

namespace Assets.Engine.Scripts.Builders
{
    /// <summary>
    /// Interface for building static meshes for blocks
    /// </summary>
    public interface IBlockBuilder
    {
        Rect GetTexture(int face);

        void Build(Map map, RenderBuffer targetMesh, ref BlockData block, int face, bool backFace, ref Vector3[] vecs, ref Vector3Int worldPos);

        void Build(Map map, RenderBuffer targetMesh, ref BlockData block, ref Vector3Int worldPos, ref Vector3Int localPos, int lod, int lod2);
    }
}