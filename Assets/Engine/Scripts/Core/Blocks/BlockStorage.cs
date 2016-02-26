using System;
using Assets.Engine.Scripts.Common.IO.RLE;
using UnityEngine;

namespace Assets.Engine.Scripts.Core.Blocks
{
    public class BlockStorage : IBlockStorage
    {
        private BlockStorageArray m_arrStorage;
        private BlockStorageRLE m_rleStorage;
        private IBlockStorage m_currStorage;
        private bool m_isCompressed;

        public RLE<BlockData> RLE { get { return m_rleStorage.RLE; } }
        
        public BlockStorage()
        {
            m_arrStorage = new BlockStorageArray();
            m_rleStorage = new BlockStorageRLE();
            m_currStorage = m_arrStorage;
            m_isCompressed = false;
        }

        public bool IsCompressed
        {
            get { return m_isCompressed; }
            set
            {
                // Ignore if there's no change
                if (value==m_isCompressed)
                    return;

                // Compression requested
                if (value)
                {
                    try
                    {
                        m_rleStorage.Set(ref m_arrStorage.Blocks);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError(ex.Message);
                    }

                    // Change current storage method to RLE and release the old one
                    m_currStorage = m_rleStorage;
                    m_arrStorage = null;
                }
                else
                // Decompression requested
                {
                    m_arrStorage = new BlockStorageArray();
                    m_rleStorage.ToArray(ref m_arrStorage.Blocks);

                    // Change current storage method to array and reset RLE
                    m_rleStorage.Reset();
                    m_currStorage = m_arrStorage;
                }

                m_isCompressed = value;
            }
        }

        #region IBlockStorage implementation
        

        public BlockData this[int x, int y, int z]
        {
            get { return m_currStorage[x, y, z]; }
            set { m_currStorage[x, y, z] = value; }
        }

        public void Set(ref BlockData[] data)
        {
            m_currStorage.Set(ref data);
        }

        public void Reset()
        {
            m_currStorage.Reset();
        }

        public BlockData[] ToArray()
        {
            return m_currStorage.ToArray();
        }

        public void ToArray(ref BlockData[] outData)
        {
            m_currStorage.ToArray(ref outData);
        }

        #endregion
    }
}
