using System.Collections.Concurrent;
using Assets.Engine.Scripts.Common;
using Assets.Engine.Scripts.Common.Collections;
using Assets.Engine.Scripts.Common.Memory;
using Assets.Engine.Scripts.Core.Chunks;
using System.Collections.Generic;
using Assets.Engine.Scripts.Core.Blocks;
using UnityEngine;

namespace Assets.Engine.Scripts.Core
{
    /// <summary>
    ///     Global object pools for often used heap objects.
    /// </summary>
    public static class GlobalPools
    {
        private const int RoundSizeBy = 100;

        public static readonly ObjectPool<Chunk> ChunkPool =
            new ObjectPool<Chunk>(ch => new Chunk(), 128, false);

        public static readonly ObjectPool<Mesh> MeshPool =
            new ObjectPool<Mesh>(m => new Mesh(), 128, false);
        
        private static readonly ConcurrentDictionary<int, IArrayPool<BlockData>> BlockDataArrayPoolsMT =
            new ConcurrentDictionary<int, IArrayPool<BlockData>>();

        private static readonly Dictionary<int, IArrayPool<Vector2>> Vector2ArrayPools =
            new Dictionary<int, IArrayPool<Vector2>>(128);

        private static readonly Dictionary<int, IArrayPool<Vector3>> Vector3ArrayPools =
            new Dictionary<int, IArrayPool<Vector3>>(128);

        private static readonly Dictionary<int, IArrayPool<Vector4>> Vector4ArrayPools =
            new Dictionary<int, IArrayPool<Vector4>>(128);

        private static readonly Dictionary<int, IArrayPool<Color32>> Color32ArrayPools =
            new Dictionary<int, IArrayPool<Color32>>(128);
        

        internal static void PopBlockDataArrayMT(int size, out BlockData[] arr)
        {
            PopArrayMT(size, BlockDataArrayPoolsMT, out arr);
        }
        
        internal static void PopVector2Array(int size, out Vector2[] arr)
        {
            PopArray(size, Vector2ArrayPools, out arr);
        }

        internal static void PopVector3Array(int size, out Vector3[] arr)
        {
            PopArray(size, Vector3ArrayPools, out arr);
        }

        internal static void PopVector4Array(int size, out Vector4[] arr)
        {
            PopArray(size, Vector4ArrayPools, out arr);
        }

        internal static void PopColor32Array(int size, out Color32[] arr)
        {
            PopArray(size, Color32ArrayPools, out arr);
        }

        internal static void PushBlockDataArrayMT(ref BlockData[] arr)
        {
            PushArray(ref arr, BlockDataArrayPoolsMT);
        }

        internal static void PushVector2Array(ref Vector2[] arr)
        {
            PushArray(ref arr, Vector2ArrayPools);
        }

        internal static void PushVector3Array(ref Vector3[] arr)
        {
            PushArray(ref arr, Vector3ArrayPools);
        }

        internal static void PushVector4Array(ref Vector4[] arr)
        {
            PushArray(ref arr, Vector4ArrayPools);
        }

        internal static void PushColor32Array(ref Color32[] arr)
        {
            PushArray(ref arr, Color32ArrayPools);
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
        
        private static void PopArrayMT<T>(int size, IDictionary<int, IArrayPool<T>> pools, out T[] item)
        {
            int length = GetRoundedSize(size);

            IArrayPool<T> pool;
            if (!pools.TryGetValue(length, out pool))
            {
                pool = new ArrayPoolMT<T>(length, 16, 1);
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
