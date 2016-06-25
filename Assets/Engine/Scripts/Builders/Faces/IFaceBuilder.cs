using Engine.Scripts.Core.Blocks;
using Engine.Scripts.Core.Pooling;
using Engine.Scripts.Rendering.Batchers;
using UnityEngine;

namespace Engine.Scripts.Builders.Faces
{
    /// <summary>
    ///     Interface for building static meshes for blocks
    /// </summary>
    public interface IFaceBuilder
    {
        Rect GetTexture(int face);

        void Build(RenderGeometryBatcher drawCallBatcher, ref BlockData block, BlockFace face, bool backFace, ref Vector3[] vecs, LocalPools pools);        
    }
}