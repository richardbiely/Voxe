using System.Collections.Generic;
using Assets.Engine.Scripts.Common;
using Assets.Engine.Scripts.Common.Memory;

namespace Assets.Engine.Scripts.Core.Pooling
{
    public abstract class AObjectPool
    {
        private const int RoundSizeBy = 100;

        protected static int GetRoundedSize(int size)
        {
            int rounded = size / RoundSizeBy * RoundSizeBy;
            return rounded == size ? rounded : rounded + RoundSizeBy;
        }

        protected static void PopArray<T>(int size, IDictionary<int, IArrayPool<T>> pools, out T[] item)
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

        protected static void PushArray<T>(ref T[] array, IDictionary<int, IArrayPool<T>> pools)
        {
            int length = array.Length;

            IArrayPool<T> pool;
            if (!pools.TryGetValue(length, out pool))
                throw new VoxeException("Couldn't find an array pool of length " + length);

            pool.Push(ref array);
        }
    }
}
