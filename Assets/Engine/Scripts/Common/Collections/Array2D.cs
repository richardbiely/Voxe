namespace Assets.Engine.Scripts.Common.Collections
{
    public sealed class Array2D<T>
    {
		    private readonly T[] m_items;
        private readonly int m_width;

        public Array2D(int x, int y, T defaultValue)
        {
            m_width = x;
            m_items = Helpers.CreateArray1D<T>(x*y);

            for (int i = 0; i < m_items.Length; i++)
                m_items[i] = defaultValue;
        }

        // Access by passing a 2D position
        public T this[int x, int y]
        {
            get { return m_items[x + y*m_width]; }
            set { m_items[x + y*m_width] = value; }
        }

        // Sequential access
        public T this[int x]
        {
            get { return m_items[x]; }
            set { m_items[x] = value; }
        }
    }
}