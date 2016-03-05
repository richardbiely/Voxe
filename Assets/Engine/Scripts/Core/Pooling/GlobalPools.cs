using Assets.Engine.Scripts.Common;
using Assets.Engine.Scripts.Common.Collections;
using Assets.Engine.Scripts.Common.Memory;
using Assets.Engine.Scripts.Core.Chunks;
using System.Collections.Generic;
using Assets.Engine.Scripts.Core.Blocks;
using Assets.Engine.Scripts.Rendering;
using UnityEngine;

namespace Assets.Engine.Scripts.Core
{
    /// <summary>
    ///     Global object pools for often used heap objects.
    /// </summary>
    public class GlobalPools
    {
        private const int RoundSizeBy = 100;

        public readonly ObjectPool<Chunk> ChunkPool =
            new ObjectPool<Chunk>(ch => new Chunk(), 128, false);

        public readonly ObjectPool<Mesh> MeshPool =
            new ObjectPool<Mesh>(m => new Mesh(), 128, false);

        private readonly Dictionary<int, IArrayPool<VertexData>> m_vertexDataArrayPools =
            new Dictionary<int, IArrayPool<VertexData>>();

        private readonly Dictionary<int, IArrayPool<BlockData>> m_blockDataArrayPools =
            new Dictionary<int, IArrayPool<BlockData>>();

        private readonly Dictionary<int, IArrayPool<Vector2>> m_vector2ArrayPools =
            new Dictionary<int, IArrayPool<Vector2>>(128);

        private readonly Dictionary<int, IArrayPool<Vector3>> m_vector3ArrayPools =
            new Dictionary<int, IArrayPool<Vector3>>(128);

        private readonly Dictionary<int, IArrayPool<Vector4>> m_vector4ArrayPools =
            new Dictionary<int, IArrayPool<Vector4>>(128);

        private readonly Dictionary<int, IArrayPool<Color32>> m_color32ArrayPools =
            new Dictionary<int, IArrayPool<Color32>>(128);

        public void PopVertexDataArray(int size, out VertexData[] arr)
        {
            PopArray(size, m_vertexDataArrayPools, out arr);
        }

        public void PopBlockDataArray(int size, out BlockData[] arr)
        {
            PopArray(size, m_blockDataArrayPools, out arr);
        }

        public void PopVector2Array(int size, out Vector2[] arr)
        {
            PopArray(size, m_vector2ArrayPools, out arr);
        }

        public void PopVector3Array(int size, out Vector3[] arr)
        {
            PopArray(size, m_vector3ArrayPools, out arr);
        }

        public void PopVector4Array(int size, out Vector4[] arr)
        {
            PopArray(size, m_vector4ArrayPools, out arr);
        }

        public void PopColor32Array(int size, out Color32[] arr)
        {
            PopArray(size, m_color32ArrayPools, out arr);
        }

        public void PushVertexData(ref VertexData[] arr)
        {
            PushArray(ref arr, m_vertexDataArrayPools);
        }

        public void PushBlockDataArray(ref BlockData[] arr)
        {
            PushArray(ref arr, m_blockDataArrayPools);
        }

        public void PushVector2Array(ref Vector2[] arr)
        {
            PushArray(ref arr, m_vector2ArrayPools);
        }

        public void PushVector3Array(ref Vector3[] arr)
        {
            PushArray(ref arr, m_vector3ArrayPools);
        }

        public void PushVector4Array(ref Vector4[] arr)
        {
            PushArray(ref arr, m_vector4ArrayPools);
        }

        public void PushColor32Array(ref Color32[] arr)
        {
            PushArray(ref arr, m_color32ArrayPools);
        }

        private static int GetRoundedSize(int size)
        {
            int rounded = size/RoundSizeBy*RoundSizeBy;
            return rounded==size ? rounded : rounded+RoundSizeBy;
        }
        

        private static void PopArray<T>(int size, IDictionary<int, IArrayPool<T>> pools, out T[] item)
        {
            int length = GetRoundedSize(size);

            IArrayPool<T> pool;
            if (!pools.TryGetValue(length, out pool))
            {
                pool = new ArrayPool<T>(length, 16, 1);
                pools.Add(length, pool);
            }

            pool.Pop(out item);
        }

        private static void PushArray<T>(ref T[] array, IDictionary<int, IArrayPool<T>> pools)
        {
            int length = array.Length;

            IArrayPool<T> pool;
            if (!pools.TryGetValue(length, out pool))
                throw new VoxeException("Couldn't find an array pool of length "+length);

            pool.Push(ref array);
        }
    }
}
