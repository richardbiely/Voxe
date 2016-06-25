#if UNITY_STANDALONE_WIN

using System;
using System.IO;
using Ionic.BZip2;

namespace Engine.Plugins.Compression
{
    public static class DataCompressor
    {
        // First 4 bytes stand for uncompressed data length
        // Everything else is compressed data

        public static byte[] Compress(byte[] buffer)
        {
            MemoryStream ms = new MemoryStream();

            // Write compressed data to memory stream
            using (BZip2OutputStream gzs = new BZip2OutputStream(ms, true))
            {
                gzs.Write(buffer, 0, buffer.Length);
            }

            int decompressedLength = buffer.Length;

            // Write compressed data length and compressed data to output buffer
            ms.Position = 0;

            var gz = new byte[ms.Length+4];
            Buffer.BlockCopy(BitConverter.GetBytes(decompressedLength), 0, gz, 0, 4);
            Buffer.BlockCopy(ms.GetBuffer(), 0, gz, 4, (int)ms.Length);
            return gz;
        }

        public static byte[] Decompress(byte[] gz)
        {
            MemoryStream ms = new MemoryStream();

            int len = BitConverter.ToInt32(gz, 0);
            ms.Write(gz, 4, gz.Length-4);

            var buffer = new byte[len];

            ms.Position = 0;
            using (BZip2InputStream zip = new BZip2InputStream(ms))
            {
                zip.Read(buffer, 0, buffer.Length);
            }

            return buffer;
        }

        public static void CompressToFile(string targetFilePath, MemoryStream streamData)
        {
            // Write compressed data length and compressed data to file
            FileStream fs = null;
            try
            {
                fs = new FileStream(targetFilePath, FileMode.Create);

                // Write compressed data length
                fs.Write(BitConverter.GetBytes(streamData.Length), 0, 4);

                // Compress data
                using (BZip2OutputStream gzs = new BZip2OutputStream(fs, true))
                    //ICSharpCode.SharpZipLib.BZip2.BZip2OutputStream(ms))
                {
                    fs = null;

                    gzs.Write(streamData.GetBuffer(), 0, (int)streamData.Length);
                }
            }
            finally
            {
                if (fs!=null)
                    fs.Dispose();
            }
        }

        public static MemoryStream DecompressFromFile(string targetFilePath, bool isWritable)
        {
            MemoryStream ms;
            FileStream fs = null;

            try
            {
                fs = new FileStream(targetFilePath, FileMode.Open);
                var tmp = new byte[4];
                fs.Read(tmp, 0, 4);

                int decompressedLength = BitConverter.ToInt32(tmp, 0);
                var b = new byte[decompressedLength];

                using (BZip2InputStream gzs = new BZip2InputStream(fs))
                    //ICSharpCode.SharpZipLib.BZip2.BZip2InputStream(fs))
                {
                    fs = null;

                    // Read decompressed data
                    gzs.Read(b, 0, b.Length);
                }

                ms = new MemoryStream(b, 0, b.Length, isWritable, true);
            }
            finally
            {
                if (fs!=null)
                    fs.Dispose();
            }

            return ms;
        }
    }
}

#endif