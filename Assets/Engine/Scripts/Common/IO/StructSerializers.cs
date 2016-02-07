using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace Assets.Engine.Scripts.Common.IO
{
    public static class StructSerializers
    {
        // Convert a struct to byte array
        public static byte[] Serialize<T>(ref T data) where T : struct
        {
            var dummyValue = default(T);
            int objSize = Marshal.SizeOf(dummyValue);
            byte[] ret = new byte[objSize];

            IntPtr buffer = Marshal.AllocHGlobal(objSize);

            Marshal.StructureToPtr(data, buffer, true);
            Marshal.Copy(buffer, ret, 0, objSize);

            Marshal.FreeHGlobal(buffer);

            return ret;
        }      

        // Convert a struct array to byte array
        public static byte[] SerializeArray<T>(ref T[] data) where T : struct
        {
            var dummyValue = default(T);
            int objSize = Marshal.SizeOf(dummyValue);
            int objArrSize = objSize * data.Length;
            byte[] ret = new byte[objArrSize];

            IntPtr buffer = Marshal.AllocHGlobal(objArrSize);

            for (int i = 0; i < data.Length; i++)
            {
                Marshal.StructureToPtr(data[i], buffer, true);// should be false in case struct uses pointers
                Marshal.Copy(buffer, ret, i * objSize, objSize);
            }

            Marshal.FreeHGlobal(buffer);

            return ret;
        }

        public static byte[] SerializeArray<T>(IList<T> data) where T : struct
        {
            var dummyValue = default(T);
            int objSize = Marshal.SizeOf(dummyValue);
            int objArrSize = objSize * data.Count;
            byte[] ret = new byte[objArrSize];

            IntPtr buffer = Marshal.AllocHGlobal(objArrSize);

            for (int i = 0; i < data.Count; i++)
            {
                Marshal.StructureToPtr(data[i], buffer, true);// should be false in case struct uses pointers
                Marshal.Copy(buffer, ret, i * objSize, objSize);
            }

            Marshal.FreeHGlobal(buffer);

            return ret;
        }

        // Convert a byte array to a struct
        public static T Deserialize<T>(ref byte[] data) where T : struct
        {
            //if(Marshal.SizeOf(typeof (T))<data.Length)
            //    throw new Exception("Input data too small");

            int objSize = data.Length;
            IntPtr buffer = Marshal.AllocHGlobal(objSize);

            Marshal.Copy(data, 0, buffer, objSize);
            T ret = (T) Marshal.PtrToStructure(buffer, typeof (T));

            Marshal.FreeHGlobal(buffer);

            return ret;
        }

        // Convert a byte array to a struct array
        public static T[] DeserializeArray<T>(ref byte[] data) where T : struct
        {
            //if (Marshal.SizeOf(typeof(T)) < data.Length)
            //    throw new Exception("Input data too small");

            int elemSize = Marshal.SizeOf(typeof(T));
            int elemLen = data.Length / elemSize;

            IntPtr buffer = Marshal.AllocHGlobal(data.Length);

            Marshal.Copy(data, 0, buffer, data.Length);
            T[] ret = new T[elemLen];

            int pBuffer = (int)buffer;
            for (int i = 0; i < elemLen; i++, pBuffer += elemSize)
                ret[i] = (T)Marshal.PtrToStructure((IntPtr)pBuffer, typeof(T));

            Marshal.FreeHGlobal(buffer);

            return ret;
        }

        public static List<T> DeserializeArrayToList<T>(ref byte[] data) where T : struct
        {
            //if (Marshal.SizeOf(typeof(T)) < data.Length)
            //    throw new Exception("Input data too small");

            int elemSize = Marshal.SizeOf(typeof(T));
            int elemLen = data.Length / elemSize;
            if (elemLen*elemSize != data.Length)
            {
                UnityEngine.Debug.LogError("Wrong length");
                return null;
            }

            IntPtr buffer = Marshal.AllocHGlobal(data.Length);

            Marshal.Copy(data, 0, buffer, data.Length);
            List<T> ret = new List<T>(elemLen);

            int pBuffer = (int)buffer;
            for (int i = 0; i < elemLen; i++, pBuffer += elemSize)
                ret.Add((T) Marshal.PtrToStructure((IntPtr) pBuffer, typeof (T)));

            Marshal.FreeHGlobal(buffer);

            return ret;
        }

        public static void BinarizeToFile(string targetFilePath, IBinarizable stream)
        {
            FileStream fs = null;
            try
            {
                fs = new FileStream(targetFilePath, FileMode.Create);
                using (var bw = new BinaryWriter(fs))
                {
                    fs = null;
                    stream.Binarize(bw);
                }
            }
            finally
            {
                if (fs!=null)
                    fs.Dispose();
            }
        }

        public static void DebinarizeFromFile(string targetFilePath, IBinarizable stream)
        {
            FileStream fs = null;
            try
            {
                fs = new FileStream(targetFilePath, FileMode.Open);
                using (var br = new BinaryReader(fs))
                {
                    fs = null;
                    stream.Debinarize(br);
                }
            }
            finally
            {
                if (fs!=null)
                    fs.Dispose();
            }
        }
    }
}