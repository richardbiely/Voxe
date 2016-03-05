using System;
using Assets.Engine.Scripts.Core;
using Assets.Engine.Scripts.Core.Chunks;
using UnityEngine;
using RenderBuffer = Assets.Engine.Scripts.Rendering.RenderBuffer;

namespace Assets.Engine.Scripts.Builders.Geometry
{
    public abstract class AVoxelGeometryBuilder: MonoBehaviour, IVoxelGeometryBuilder
    {
        public virtual void BuildMesh(
            Map map, RenderBuffer renderBuffer, int offsetX, int offsetY, int offsetZ,
            int minX, int maxX, int minY, int maxY, int minZ, int maxZ, int lod,
            LocalPools pools
            )
        {
            throw new NotImplementedException();
        }
    }
}
