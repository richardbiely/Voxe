using System;

namespace Assets.Engine.Scripts.Common.Collections
{
    public sealed class ObjectPool<T> where T: class
    {
        // Object storage
        readonly T[] m_objects;
        // Index to the first available object in object pool
        int m_objectIndex;

        public ObjectPool (Func<T> objectAllocator, int size)
        {
            m_objectIndex = size;
		
            m_objects = Helpers.CreateArray1D<T> (size);
            for (int i=0; i<size; i++)
                m_objects [i] = objectAllocator ();
        }

        // Retrieves an object from the object pool (if it already exists) or else
        // creates an instance of an object and returns it (if if does not exist)
        public T Pop ()
        {
            int index = --m_objectIndex;
            if (index < 0)
                throw new Exception ("No objects left in pool");

            return m_objects [index];
        }

        // Returns an object back to pool.
        public void Push (T item)
        {
            int index = ++m_objectIndex;
            if (index > m_objects.Length)
                throw new Exception ("Object pool is full");

            m_objects [--index] = item;
        }
    }
}