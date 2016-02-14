﻿using System.Collections.Generic;

namespace Assets.Engine.Scripts.Common.Memory
{
    public sealed class ArrayPool<T>
    {
        //! Stack of arrays
        private readonly Stack<T[]> m_arrays;
        //! Length of array to allocate
        private readonly int m_arrLength;
        
        public ArrayPool(int length, int initialCapacity, int initialSize)
        {
            m_arrLength = length;

            if (initialSize>0)
            {
                // Init
                m_arrays = new Stack<T[]>(initialSize<initialCapacity ? initialCapacity : initialSize);

                for (int i = 0; i<initialSize; ++i)
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

        public T[] Pop()
        {
            if (m_arrays.Count==0)
                return new T[m_arrLength];

            return m_arrays.Pop();
        }

        public void Push(T[] item)
        {
            if (item==null)
                return;

            m_arrays.Push(item);
        }
    }
}