using Assets.Engine.Scripts.Common;
using Assets.Engine.Scripts.Common.Collections;
using Assets.Engine.Scripts.Common.Memory;
using Assets.Engine.Scripts.Core.Chunks;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Engine.Scripts.Core
{
    /// <summary>
    ///     Global object pools for often used heap objects.
    /// </summary>
    public static class GlobalPools
    {
        private const int RoundSizeBy = 100;

        public static readonly ObjectPool<Chunk> ChunkPool = new ObjectPool<Chunk>(ch => new Chunk(), 128, false);
        public static readonly ObjectPool<Mesh> MeshPool = new ObjectPool<Mesh>(m => new Mesh(), 128, false);

        private static readonly Dictionary<int, ArrayPool<Vector2>> Vector2ArrayPools =
            new Dictionary<int, ArrayPool<Vector2>>(128);

        private static readonly Dictionary<int, ArrayPool<Vector3>> Vector3ArrayPools =
            new Dictionary<int, ArrayPool<Vector3>>(128);

        private static readonly Dictionary<int, ArrayPool<Vector4>> Vector4ArrayPools =
            new Dictionary<int, ArrayPool<Vector4>>(128);

        private static readonly Dictionary<int, ArrayPool<Color32>> Color32ArrayPools =
            new Dictionary<int, ArrayPool<Color32>>(128);

        internal static Vector2[] PopVector2Array(int size)
        {
            return PopArray(size, Vector2ArrayPools);
        }

        internal static Vector3[] PopVector3Array(int size)
        {
            return PopArray(size, Vector3ArrayPools);
        }

        internal static Vector4[] PopVector4Array(int size)
        {
            return PopArray(size, Vector4ArrayPools);
        }

        internal static Color32[] PopColor32Array(int size)
        {
            return PopArray(size, Color32ArrayPools);
        }

        internal static void PushVector2Array(Vector2[] arr)
        {
            PushArray(arr, Vector2ArrayPools);
        }

        internal static void PushVector3Array(Vector3[] arr)
        {
            PushArray(arr, Vector3ArrayPools);
        }

        internal static void PushVector4Array(Vector4[] arr)
        {
            PushArray(arr, Vector4ArrayPools);
        }

        internal static void PushColor32Array(Color32[] arr)
        {
            PushArray(arr, Color32ArrayPools);
        }

        private static int GetRoundedSize(int size)
        {
            int rounded = size/RoundSizeBy*RoundSizeBy;
            return rounded==size ? rounded : rounded+RoundSizeBy;
        }

        private static T[] PopArray<T>(int size, IDictionary<int, ArrayPool<T>> pools)
        {
            int length = GetRoundedSize(size);

            ArrayPool<T> pool;
            if (!pools.TryGetValue(length, out pool))
            {
                pool = new ArrayPool<T>(length, 16, 1);
                pools.Add(length, pool);
            }

            return pool.Pop();
        }

        private static void PushArray<T>(T[] arrayOfT, IDictionary<int, ArrayPool<T>> pools)
        {
            int length = arrayOfT.Length;

            ArrayPool<T> pool;
            if (!pools.TryGetValue(length, out pool))
                throw new VoxeException("Couldn't find an array pool of length "+length);

            pool.Push(arrayOfT);
        }
    }
}
