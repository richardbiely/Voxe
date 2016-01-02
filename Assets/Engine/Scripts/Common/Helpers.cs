using System;
using UnityEngine;

namespace Assets.Engine.Scripts.Common
{
    public static class Helpers
    {
        public static Vector3 ZeroVec3 = Vector3.zero;
        public static Vector2 ZeroVec2 = Vector2.zero;
        public static Quaternion ZeroQuat = Quaternion.identity;
        
        public static int GetIndex1DFrom2D(int x, int z, int sizeX)
        {
            return x + z*sizeX;
        }
        
        public static int GetIndex1DFrom2D(int x, int z)
        {
            return x + (z << EngineSettings.ChunkConfig.LogSizeX);
        }

        public static int GetIndex1DFrom3D(int x, int y, int z, int sizeX, int sizeZ)
        {
            return x + z*sizeX + y*sizeX*sizeZ;
        }

        public static int GetIndex1DFrom3D(int x, int y, int z)
        {
            return x +
                (z << EngineSettings.ChunkConfig.LogSizeX) +
                (y << EngineSettings.ChunkConfig.LogSizeXZ);
        }

        public static void GetIndex2DFrom1D(int index, out int x, out int z, int sizeX)
        {
            x = index%sizeX;
            z = index/sizeX;
        }

        public static void GetIndex2DFrom1D(int index, out int x, out int z)
        {
            x = index & EngineSettings.ChunkConfig.MaskX;
            z = index >> EngineSettings.ChunkConfig.LogSizeX;
        }

        public static void GetIndex3DFrom1D(int index, out int x, out int y, out int z, int sizeX, int sizeZ)
        {
            x = index % sizeX;
            y = index / (sizeX * sizeZ);
            z = (index / sizeX) % sizeZ;
        }

        public static void GetIndex3DFrom1D(int index, out int x, out int y, out int z)
        {
            x = index & EngineSettings.ChunkConfig.MaskX;
            y = index >> EngineSettings.ChunkConfig.LogSizeXZ;
            z = (index >> EngineSettings.ChunkConfig.LogSizeX) & EngineSettings.ChunkConfig.MaskZ;
        }

        public static T[] CreateArray1D<T>(int size)
        {
            return new T[size];
        }

        public static T[] CreateAndInitArray1D<T>(int size) where T: new()
        {
            var arr = new T[size];
            for (int i = 0; i<size; i++)
                arr[i] = new T();

            return arr;
        }

        public static T[][] CreateArray2D<T>(int sizeX, int sizeY)
        {
            var arr = new T[sizeX][];

            for (int i = 0; i<sizeX; i++)
                arr[i] = new T[sizeY];

            return arr;
        }

        public static T[][] CreateAndInitArray2D<T>(int sizeX, int sizeY) where T: new()
        {
            var arr = new T[sizeX][];

            for (int i = 0; i<sizeX; i++)
            {
                arr[i] = new T[sizeY];
                for(int j=0; j<sizeY; j++)
                    arr[i][j] = new T();
            }

            return arr;
        }

        public static float Interpolate(float x0, float x1, float alpha)
        {
            return (x0 * (1 - alpha)) + (x1 * alpha);
        }

        public static float IntBound(float s, float ds)
        {
            /* Recursive version
			        if (ds < 0)
			        {
				        return IntBound(-s, -ds);
			        }
			        else
			        {
				        s = Mod(s, 1);
				        return (1 - s) / ds;
			        }
             */
            while (true)
            {
                if (ds<0)
                {
                    s = -s;
                    ds = -ds;
                    continue;
                }

                s = Mod(s, 1);
                return (1-s)/ds;
            }
        }

        public static int Signum(float x)
        {
            return (x>0) ? 1 : ((x<0) ? -1 : 0);
        }

        // custom modulo, handles negative numbers
        public static int Mod(int value, int modulus)
        {
            int r = value%modulus;
            return (r<0) ? (r+modulus) : r;
        }

        public static float Mod(float value, int modulus)
        {
            return (value%modulus+modulus)%modulus;
        }

        public static T Clamp<T>(this T val, T min, T max) where T: IComparable<T>
        {
            if (val.CompareTo(min)<0)
                return min;

            return val.CompareTo(max)>0 ? max : val;
        }
    }
}