using System.IO;

namespace Engine.Scripts.Common.IO
{
    public static class FileHelpers
    {
        public static void SaveToFile(string targetFilePath, byte[] data)
        {
            using (FileStream fs = new FileStream(targetFilePath, FileMode.Create))
            {
                fs.Write(data, 0, data.Length);
            }
        }

        public static void SaveToFile(string targetFilePath, MemoryStream streamData)
        {
            using (FileStream fs = new FileStream(targetFilePath, FileMode.Create))
            {
                fs.Write(streamData.GetBuffer(), 0, (int)streamData.Length);
            }
        }

        public static void LoadFromFile(string targetFilePath, out byte[] data)
        {
            using (FileStream fs = new FileStream(targetFilePath, FileMode.Open))
            {
                data = new byte[fs.Length];
                fs.Read(data, 0, data.Length);
            }
        }

        public static void LoadFromFile(string targetFilePath, out MemoryStream ms)
        {
            using (FileStream fs = new FileStream(targetFilePath, FileMode.Open))
            {
                ms = new MemoryStream((int)fs.Length);
                fs.Read(ms.GetBuffer(), 0, (int)fs.Length);
            }
        }

        public static void WriteAllBytes(string path, byte[] data)
        {
            FileStream fs = null;
            try
            {
                fs = new FileStream(path, FileMode.Create);
                using (BinaryWriter bw = new BinaryWriter(fs))
                {
                    fs = null;
                    bw.Write(data);
                }
            }
            finally
            {
                if (fs!=null)
                    fs.Dispose();
            }
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
                if (fs != null)
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
                if (fs != null)
                    fs.Dispose();
            }
        }
    }
}