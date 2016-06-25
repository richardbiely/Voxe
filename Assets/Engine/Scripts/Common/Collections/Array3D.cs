namespace Engine.Scripts.Common.Collections
{
    public sealed class Array3D<T>
    {
        private readonly int m_width;
        private readonly int m_height;
        private readonly T[] m_items;

        public Array3D(int x, int y, int z)
        {
            m_width = x;
            m_height = y;
            m_items = Helpers.CreateArray1D<T>(x * y * z);
        }

        // Access by passing a 3D position
        public T this [int x, int y, int z] {
            get { return m_items[x + m_width * (y + (z * m_height))]; }
            set { m_items[x + m_width * (y + (z * m_height))] = value; }
        }

        // Sequential access
        public T this[int x]
        {
            get { return m_items[x]; }
            set { m_items[x] = value; }
        }
    }
}