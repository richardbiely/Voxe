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
#if UNITY_STANDALONE_WIN
        private const int ChunkCnt = 4096;
        private const int MeshCnt = 4096;
#else//if UNITY_WP8
        private const int ChunkCnt = 200;
        private const int MESH_CNT = 200;
#endif

        public static readonly ObjectPool<Chunk> Chunks = new ObjectPool<Chunk> (() => new Chunk (), ChunkCnt);
        public static readonly ObjectPool<Mesh> Meshes = new ObjectPool<Mesh> (() => new Mesh (), MeshCnt);
    }
}