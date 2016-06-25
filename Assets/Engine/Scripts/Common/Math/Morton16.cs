using UnityEngine.Assertions;

namespace Engine.Scripts.Common.Math
{
    // Morton encoding class capable of encoding 3 numbers each of which are at most 16 bits long
    public static class Morton16
    {
        private static readonly ulong[] Values = new ulong[65535];

        //! Initiates morton keys
        public static void Init()
        {
            for (int i = 0; i<Values.Length; i++)
                Values[i] = Encode1D((ulong)i);
        }

        private static ulong Encode1D(ulong value)
        {
            value = value & 0x0000FFFFFFFFFFFF;
            value = (value | (value << 16)) & 0x00000000FF0000FF;
            value = (value | (value << 8)) & 0x000000F00F00F00F;
            value = (value | (value << 4)) & 0x00000C30C30C30C3;
            value = (value | (value << 2)) & 0x0000249249249249;
            return value;
        }

        private static ulong Decode1D(ulong value)
        {
            value = value & 0x0000249249249249;
            value = (value | (value >> 2)) & 0x00000C30C30C30C3;
            value = (value | (value >> 4)) & 0x000000F00F00F00F;
            value = (value | (value >> 8)) & 0x00000000FF0000FF;
            value = (value | (value >> 16)) & 0x0000FFFFFFFFFFFF;
            return value;
        }
        
        public static ulong Encode(int x, int y, int z)
        {
            return (Values[x])|(Values[y]<<1)|(Values[z]<<2);
        }

        public static void Decode(ulong value, out int x, out int y, out int z)
        {
            x = (int)Decode1D(value);
            y = (int)Decode1D(value >> 1);
            z = (int)Decode1D(value >> 2);
            Assert.IsTrue(
                (x|y|z)>=0 && (x|y|z)<65535, string.Format("Value {0} can't be encoded in morton code 16b", value)
                );
        }
    }
}
