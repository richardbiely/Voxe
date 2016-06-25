#if DEBUG
using System;
using System.IO;
using Engine.Scripts.Common.IO;
using Engine.Scripts.Common.IO.RLE;
using NUnit.Framework;

namespace Assets.Engine.Scripts.UnitTesting
{
    [TestFixture]
    public class UTRLE
    {
        public struct RLEData: IComparable<RLEData>, IBinarizable
        {
            private readonly short m_data;

            public RLEData(short data)
            {
                m_data = data;
            }

            public int CompareTo(RLEData other)
            {
                return m_data==other.m_data ? 0 : 1;
            }

            public void Binarize(BinaryWriter bw)
            {
                bw.Write((short)m_data);
            }

            public void Debinarize(BinaryReader br)
            {
                short val = br.ReadInt16();
                this = new RLEData(val);
            }
        }

        [Test]
        public void CompressDecompress()
        {
            Random r = new Random(0);

            const int n = 1600;
            RLEData[] rledata = new RLEData[n];
            for (int i = 0; i<n; i++)
                rledata[i] = new RLEData((short)r.Next(0, 100));

            
            RLE<RLEData> rle = new RLE<RLEData>();
            rle.Compress(ref rledata);


            RLEData[] decompressed = rle.Decompress();

            for (int i = 0; i<n; i++)
            {
                Assert.AreEqual(rledata[i], decompressed[i]);
            }
        }
    }
}
#endif