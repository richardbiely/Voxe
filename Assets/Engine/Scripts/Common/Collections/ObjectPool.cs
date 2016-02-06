using System;
using System.Collections.Generic;

namespace Assets.Engine.Scripts.Common.Collections
{
    public sealed class ObjectPool<T> where T: class
    {
        // Allocator used to allocate new memory
        private readonly Func<T> m_objectAllocator;
        // Object storage
        private readonly List<T> m_objects;
        // Index to the first available object in object pool
        private int m_objectIndex;

        public ObjectPool (Func<T> objectAllocator, int initialSize)
        {
            m_objectAllocator = objectAllocator;
            m_objectIndex = 0;

            m_objects = new List<T>(initialSize);
            for (int i=0; i< initialSize; i++)
                m_objects.Add(objectAllocator());
        }

        // Retrieves an object from pool and returns it
        public T Pop()
        {
            if (m_objectIndex >= m_objects.Count)
            {
                // Capacity limit has been reached, allocate new elemets
                m_objects.Add(m_objectAllocator());
                // Let Unity handle how much memory is going to be preallocated
                for (int i = m_objects.Count; i < m_objects.Capacity; i++)
                    m_objects.Add(m_objectAllocator());
            }
            
            return m_objects[m_objectIndex++];
        }

        // Returns an object back to pool
        public void Push (T item)
        {
            if (m_objectIndex<=0)
                throw new Exception("Object pool is full");

            m_objects [--m_objectIndex] = item;
        }
    }
}