using System;
using Engine.Scripts.Core.Chunks.Managers;
using Engine.Scripts.Core.Pooling;
using Engine.Scripts.Rendering.Batchers;
using UnityEngine;

namespace Engine.Scripts.Builders.Geometry
{
    public abstract class AVoxelGeometryBuilder: MonoBehaviour, IVoxelGeometryBuilder
    {
        public virtual void BuildMesh(
            Map map, RenderGeometryBatcher batcher, int offsetX, int offsetY, int offsetZ,
            int minX, int maxX, int minY, int maxY, int minZ, int maxZ, int lod,
            LocalPools pools
            )
        {
            throw new NotImplementedException();
        }
    }
}
