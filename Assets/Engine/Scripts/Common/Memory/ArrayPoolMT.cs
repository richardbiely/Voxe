using System.Collections.Generic;

namespace Assets.Engine.Scripts.Common.Memory
{
    public sealed class ArrayPoolMT<T> : IArrayPool<T>
    {
        private readonly object m_lock = new object();
        //! Stack of arrays
        private readonly Stack<T[]> m_arrays;
        //! Length of array to allocate
        private readonly int m_arrLength;

        public ArrayPoolMT(int length, int initialCapacity, int initialSize)
        {
            m_arrLength = length;

            if (initialSize > 0)
            {
                // Init
                m_arrays = new Stack<T[]>(initialSize < initialCapacity ? initialCapacity : initialSize);

                for (int i = 0; i < initialSize; ++i)
                {
                    var item = new T[length];
                    m_arrays.Push(item);
                }
            }
            else
            {
                // Init
                m_arrays = new Stack<T[]>(initialCapacity);
            }
        }

        /// <summary>
        ///     Retrieves an array from the top of the pool
        /// </summary>
        public T[] Pop()
        {
            lock (m_lock)
            {
                return m_arrays.Count == 0 ? new T[m_arrLength] : m_arrays.Pop();
            }
        }

        /// <summary>
        ///     Retrieves an array from the top of the pool
        /// </summary>
        public void Pop(out T[] item)
        {
            lock (m_lock)
            {
                item = m_arrays.Count == 0 ? new T[m_arrLength] : m_arrays.Pop();
            }
        }

        /// <summary>
        ///     Returns an array back to the pool
        /// </summary>
        public void Push(ref T[] item)
        {
            if (item == null)
                return;

            lock (m_lock)
            {
                m_arrays.Push(item);
            }
        }
    }
}