using System.Collections;
using UnityEngine;

namespace Assets.Engine.Scripts.Common.Collections
{
    /// <summary>
    ///     Represents a circular 2D array
    /// </summary>
    public sealed class CircularArray2D<T>: IEnumerable
    {
        // size X and Y of the array
        private readonly T[] m_items;

        // Helpers values used for calculation of position inside the item list
        private readonly int m_maskX;
        private readonly int m_maskZ;
        private readonly int m_logX;

        //private readonly object m_lock = new object();
        
        public CircularArray2D(int width, int height)
        {
            width = Mathf.NextPowerOfTwo(width);
            height = Mathf.NextPowerOfTwo(height);

            m_logX = Mathf.CeilToInt(Mathf.Log(width, 2));
            m_maskX = width - 1;
            m_maskZ = height - 1;

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

        public int Width
        {
            get
            {
                return m_maskX+1;
            }
        }

        public int Height
        {
            get
            {
                return m_maskZ+1;
            }
        }

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
                int realX = (x + OffsetX) & m_maskX;
                int realZ = (z + OffsetZ) & m_maskZ;
                int pos = realX+(realZ<<m_logX);
                return this[pos];
            }
            set
            {
                int realX = (x + OffsetX) & m_maskX;
                int realZ = (z + OffsetZ) & m_maskZ;
                int pos = realX+(realZ<<m_logX);
                this[pos] = value;
            }
        }

        /// <summary>
        ///     Access circular array as tough it were a regular array. No safety checks.
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