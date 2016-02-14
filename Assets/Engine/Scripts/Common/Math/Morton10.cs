using UnityEngine.Assertions;

namespace Assets.Engine.Scripts.Common.Math
{
    // Morton encoding class capable of encoding 3 numbers each of which are at most 10 bits long
    public static class Morton10
    {
        private static readonly uint[] Values = new uint[1024];

        //! Initiates morton keys
        public static void Init()
        {
            for (int i = 0; i<Values.Length; i++)
                Values[i] = Encode1D((uint)i);
        }

        private static uint Encode1D(uint value)
        {
            value = value & 0x3FFFFFFF;
            value = (value | (value << 16)) & 0x030000FF;
            value = (value | (value << 8)) & 0x0300F00F;
            value = (value | (value << 4)) & 0x030C30C3;
            value = (value | (value << 2)) & 0x09249249;
            return value;
        }

        private static uint Decode1D(uint value)
        {
            value = value & 0x09249249;
            value = (value | (value >> 2)) & 0x030C30C3;
            value = (value | (value >> 4)) & 0x0300F00F;
            value = (value | (value >> 8)) & 0x030000FF;
            value = (value | (value >> 16)) & 0x3FFFFFFF;
            return value;
        }
        
        public static uint Encode(int x, int y, int z)
        {
            return (Values[x])|(Values[y]<<1)|(Values[z]<<2);
        }

        public static void Decode(uint value, out int x, out int y, out int z)
        {
            x = (int)Decode1D(value);
            y = (int)Decode1D(value >> 1);
            z = (int)Decode1D(value >> 2);
            Assert.IsTrue(
                (x|y|z)>=0 && (x|y|z)<1024, string.Format("Value {0} can't be encoded in morton code 10b", value)
                );
        }
    }
}
