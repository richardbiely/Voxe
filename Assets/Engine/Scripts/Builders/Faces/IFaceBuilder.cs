using Assets.Engine.Scripts.Core;
using Assets.Engine.Scripts.Core.Blocks;
using Assets.Engine.Scripts.Rendering;
using Assets.Engine.Scripts.Utils;
using UnityEngine;

namespace Assets.Engine.Scripts.Builders.Faces
{
    /// <summary>
    ///     Interface for building static meshes for blocks
    /// </summary>
    public interface IFaceBuilder
    {
        Rect GetTexture(int face);

        void Build(DrawCallBatcher drawCallBatcher, ref BlockData block, BlockFace face, bool backFace, ref Vector3[] vecs, LocalPools pools);        
    }
}