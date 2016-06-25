using System.Collections;

namespace Engine.Scripts.Common.Collections
{
    /// <summary>
    ///     Represents a circular 2D array
    /// </summary>
    public sealed class CircularArray2D<T>: IEnumerable
    {
        // size X and Y of the array
        private readonly T[] m_items;

        // Helpers values used for calculation of position inside the item list

        public CircularArray2D(int width, int height)
        {
            Width = width;
            Height = height;

            OffsetX = 0;
            OffsetZ = 0;

            m_items = Helpers.CreateArray1D<T>(width*height);
        }

        /// <summary>
        ///     Get number of elements of the array
        /// </summary>
        public int Size
        {
            get
            {
                return m_items.Length;
            }
        }

        public int Width { get; private set; }

        public int Height { get; private set; }

        /// <summary>
        ///     Offset for X index
        /// </summary>
        public int OffsetX { get; private set; }

        /// <summary>
        ///     Offset for Y index
        /// </summary>
        public int OffsetZ { get; private set; }
        
        /// <summary>
        ///     Access internal array in a circular way
        /// </summary>
        public T this[int x, int z]
        {
            get
            {
                int realX = Helpers.Mod(x + OffsetX, Width);
                int realZ = Helpers.Mod(z + OffsetZ, Height);
                int pos = Helpers.GetIndex1DFrom2D(realX, realZ, Width);
                return m_items[pos];
            }
            set
            {
                int realX = Helpers.Mod(x + OffsetX, Width);
                int realZ = Helpers.Mod(z + OffsetZ, Height);
                int pos = Helpers.GetIndex1DFrom2D(realX, realZ, Width);
                m_items[pos] = value;
            }
        }

        /// <summary>
        ///     Access circular array as tough it was a regular array. No safety checks.
        /// </summary>
        public T this[int i]
        {
            get { return m_items[i]; }
            set { m_items[i] = value; }
        }

        public void SetOffset(int x, int z)
        {
            OffsetX = x;
            OffsetZ = z;
        }

        public IEnumerator GetEnumerator()
        {
            return m_items.GetEnumerator();
        }
    }
}