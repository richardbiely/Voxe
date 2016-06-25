using System.Runtime.InteropServices;
using System.Text;

namespace Engine.Scripts.Common.IO.RLE
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct RLEDataPair<T>
    {
        private readonly int _key;
        private readonly T _value;

        public int Key
        {
            get { return _key; }
        }

        public T Value
        {
            get { return _value; }
        }

        public RLEDataPair(int k, T v)
        {
            _key = k;
            _value = v;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendFormat("[{0},{1}]", _key, _value);
            return sb.ToString();
        }
    }
}