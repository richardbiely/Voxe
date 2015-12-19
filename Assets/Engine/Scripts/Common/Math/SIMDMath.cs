#if UNITY_STANDALONE_WIN
using Mono.Simd;

namespace Assets.Engine.Scripts.Common.Math
{
    public static class SimdMath
    {
        public static float Dot (ref Vector4f vector1, ref Vector4f vector2)
        {
            Vector4f t = vector1 * vector2;
		
            t = t.HorizontalAdd (t);		
            t = t.HorizontalAdd (t);
		
            return t.X;
        }
		
        public static void Cross (ref Vector4f a, ref Vector4f b, out Vector4f result)
        {
            result = (a * b.Shuffle ((ShuffleSel)0xc9) - b * a.Shuffle ((ShuffleSel)0xc9)).Shuffle ((ShuffleSel)0xc9);		
        }
    }
}
#endif