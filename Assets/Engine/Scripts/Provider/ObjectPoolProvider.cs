using Assets.Engine.Scripts.Common.Collections;
using Assets.Engine.Scripts.Core.Chunks;
using UnityEngine;

namespace Assets.Engine.Scripts.Provider
{
    /// <summary>
    /// Memory provider for often used heap objects.
    /// </summary>
    public static class ObjectPoolProvider
    {
        private const int ChunkCnt = 4096;
        private const int MeshCnt = 4096;

        public static readonly ObjectPool<Chunk> Chunks = new ObjectPool<Chunk> (() => new Chunk (), ChunkCnt);
        public static readonly ObjectPool<Mesh> Meshes = new ObjectPool<Mesh> (() => new Mesh (), MeshCnt);
    }
}