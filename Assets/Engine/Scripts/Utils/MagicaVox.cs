using System.IO;
using UnityEngine;

namespace Engine.Scripts.Utils
{
    class MagicaVox
    {
        // this is the default palette of voxel colors (the RGBA chunk is only included if the palette is differe)
        private static readonly uint[] SVoxColors =
        {
            0x00000000, 0xffffffff, 0xffccffff, 0xff99ffff, 0xff66ffff, 0xff33ffff, 0xff00ffff, 0xffffccff, 0xffccccff, 0xff99ccff, 0xff66ccff, 0xff33ccff, 0xff00ccff, 0xffff99ff, 0xffcc99ff, 0xff9999ff,
            0xff6699ff, 0xff3399ff, 0xff0099ff, 0xffff66ff, 0xffcc66ff, 0xff9966ff, 0xff6666ff, 0xff3366ff, 0xff0066ff, 0xffff33ff, 0xffcc33ff, 0xff9933ff, 0xff6633ff, 0xff3333ff, 0xff0033ff, 0xffff00ff,
            0xffcc00ff, 0xff9900ff, 0xff6600ff, 0xff3300ff, 0xff0000ff, 0xffffffcc, 0xffccffcc, 0xff99ffcc, 0xff66ffcc, 0xff33ffcc, 0xff00ffcc, 0xffffcccc, 0xffcccccc, 0xff99cccc, 0xff66cccc, 0xff33cccc,
            0xff00cccc, 0xffff99cc, 0xffcc99cc, 0xff9999cc, 0xff6699cc, 0xff3399cc, 0xff0099cc, 0xffff66cc, 0xffcc66cc, 0xff9966cc, 0xff6666cc, 0xff3366cc, 0xff0066cc, 0xffff33cc, 0xffcc33cc, 0xff9933cc,
            0xff6633cc, 0xff3333cc, 0xff0033cc, 0xffff00cc, 0xffcc00cc, 0xff9900cc, 0xff6600cc, 0xff3300cc, 0xff0000cc, 0xffffff99, 0xffccff99, 0xff99ff99, 0xff66ff99, 0xff33ff99, 0xff00ff99, 0xffffcc99,
            0xffcccc99, 0xff99cc99, 0xff66cc99, 0xff33cc99, 0xff00cc99, 0xffff9999, 0xffcc9999, 0xff999999, 0xff669999, 0xff339999, 0xff009999, 0xffff6699, 0xffcc6699, 0xff996699, 0xff666699, 0xff336699,
            0xff006699, 0xffff3399, 0xffcc3399, 0xff993399, 0xff663399, 0xff333399, 0xff003399, 0xffff0099, 0xffcc0099, 0xff990099, 0xff660099, 0xff330099, 0xff000099, 0xffffff66, 0xffccff66, 0xff99ff66,
            0xff66ff66, 0xff33ff66, 0xff00ff66, 0xffffcc66, 0xffcccc66, 0xff99cc66, 0xff66cc66, 0xff33cc66, 0xff00cc66, 0xffff9966, 0xffcc9966, 0xff999966, 0xff669966, 0xff339966, 0xff009966, 0xffff6666,
            0xffcc6666, 0xff996666, 0xff666666, 0xff336666, 0xff006666, 0xffff3366, 0xffcc3366, 0xff993366, 0xff663366, 0xff333366, 0xff003366, 0xffff0066, 0xffcc0066, 0xff990066, 0xff660066, 0xff330066,
            0xff000066, 0xffffff33, 0xffccff33, 0xff99ff33, 0xff66ff33, 0xff33ff33, 0xff00ff33, 0xffffcc33, 0xffcccc33, 0xff99cc33, 0xff66cc33, 0xff33cc33, 0xff00cc33, 0xffff9933, 0xffcc9933, 0xff999933,
            0xff669933, 0xff339933, 0xff009933, 0xffff6633, 0xffcc6633, 0xff996633, 0xff666633, 0xff336633, 0xff006633, 0xffff3333, 0xffcc3333, 0xff993333, 0xff663333, 0xff333333, 0xff003333, 0xffff0033,
            0xffcc0033, 0xff990033, 0xff660033, 0xff330033, 0xff000033, 0xffffff00, 0xffccff00, 0xff99ff00, 0xff66ff00, 0xff33ff00, 0xff00ff00, 0xffffcc00, 0xffcccc00, 0xff99cc00, 0xff66cc00, 0xff33cc00,
            0xff00cc00, 0xffff9900, 0xffcc9900, 0xff999900, 0xff669900, 0xff339900, 0xff009900, 0xffff6600, 0xffcc6600, 0xff996600, 0xff666600, 0xff336600, 0xff006600, 0xffff3300, 0xffcc3300, 0xff993300,
            0xff663300, 0xff333300, 0xff003300, 0xffff0000, 0xffcc0000, 0xff990000, 0xff660000, 0xff330000, 0xff0000ee, 0xff0000dd, 0xff0000bb, 0xff0000aa, 0xff000088, 0xff000077, 0xff000055, 0xff000044,
            0xff000022, 0xff000011, 0xff00ee00, 0xff00dd00, 0xff00bb00, 0xff00aa00, 0xff008800, 0xff007700, 0xff005500, 0xff004400, 0xff002200, 0xff001100, 0xffee0000, 0xffdd0000, 0xffbb0000, 0xffaa0000,
            0xff880000, 0xff770000, 0xff550000, 0xff440000, 0xff220000, 0xff110000, 0xffeeeeee, 0xffdddddd, 0xffbbbbbb, 0xffaaaaaa, 0xff888888, 0xff777777, 0xff555555, 0xff444444, 0xff222222, 0xff111111
        };

        private const int LogX = 5;
        private const int LogY = 7;
        private const int LogZ = 5;

        private const int SizeX = 1 << LogX;
        private const int SizeY = 1 << LogY;
        private const int SizeZ = 1 << LogZ;

        private struct MagicaVoxelData
        {
            public readonly byte X;
            public readonly byte Y;
            public readonly byte Z;
            public readonly byte Color;

            public MagicaVoxelData(BinaryReader stream, bool subsample)
            {
                X = (byte)(subsample ? (stream.ReadByte() >> 1) : stream.ReadByte());
                Y = (byte)(subsample ? (stream.ReadByte() >> 1) : stream.ReadByte());
                Z = (byte)(subsample ? (stream.ReadByte() >> 1) : stream.ReadByte());
                Color = stream.ReadByte();
            }
        }

        /// <summary>
        /// Load a MagicaVoxel .vox format file into the custom ushort[] structure that we use for voxel chunks.
        /// </summary>
        /// <param name="stream">An open BinaryReader stream that is the .vox file.</param>
        /// <param name="overrideColors">Optional color lookup table for converting RGB values into my internal engine color format.</param>
        /// <returns>The voxel chunk data for the MagicaVoxel .vox file.</returns>
        private static Color32[] FromMagica(BinaryReader stream)
        {
            // a MagicaVoxel .vox file starts with a 'magic' 4 character 'VOX ' identifier
            string magic = new string(stream.ReadChars(4));            
            if (magic != "VOX ")
                return null;

            int version = stream.ReadInt32();
            if (version != 150)
            {
                Debug.LogWarning("Vox file version does not match 150. Issues possible.");
            }

            // check out http://voxel.codeplex.com/wikipage?title=VOX%20Format&referringTitle=Home for the file format used below
            // we're going to return a voxel chunk worth of data
            Color32[] data = new Color32[SizeX * SizeY * SizeZ];
            Color32[] colors = null;
            MagicaVoxelData[] voxelData = null;

            bool subsample = false;

            while (stream.BaseStream.Position < stream.BaseStream.Length)
            {
                // each chunk has an ID, size and child chunks
                char[] chunkId = stream.ReadChars(4);
                int chunkSize = stream.ReadInt32();
                int childChunks = stream.ReadInt32();
                if (childChunks < 0)
                {
                    Debug.LogError("childChunks < 0");
                    return null;
                }

                string chunkName = new string(chunkId);
                switch (chunkName)
                {
                    // Chunk dimensions
                    case "SIZE":
                    {
                        int sizeX = stream.ReadInt32();
                        int sizeY = stream.ReadInt32();
                        int sizeZ = stream.ReadInt32();

                        if (sizeX > SizeX || sizeZ > SizeZ || sizeY > SizeY)
                            subsample = true;

                        stream.ReadBytes(chunkSize - 4 * 3);
                        break;
                    }                        
                    // Voxel data
                    case "XYZI":
                    {
                        // XYZI contains n voxels
                        int numVoxels = stream.ReadInt32();
                        if (numVoxels <= 0)
                            return null;

                        //int div = (subsample ? 2 : 1);

                        // Each voxel has x, y, z and color index values
                        voxelData = new MagicaVoxelData[numVoxels];
                        for (int i = 0; i < voxelData.Length; i++)
                            voxelData[i] = new MagicaVoxelData(stream, subsample);

                        break;
                    }
                    case "RGBA":
                    {
                        colors = new Color32[256];
                        for (int i = 0; i < 256; i++)
                        {
                            byte r = stream.ReadByte();
                            byte g = stream.ReadByte();
                            byte b = stream.ReadByte();
                            byte a = stream.ReadByte();

                            // Convert RGBA to our custom voxel format (16 bits, 0RRR RRGG GGGB BBBB)
                            colors[i] = new Color32(r,g,b,a);
                                //(ushort) (((r & 0x1f) << 10) | ((g & 0x1f) << 5) | (b & 0x1f));
                        }

                        break;
                    }

                    default:
                        // Read any excess bytes
                        stream.ReadBytes(chunkSize);
                        break;
                }
            }

            // Failed to read any valid voxel data
            if (voxelData == null || voxelData.Length == 0)
                return null;

            // Push the voxel data into our voxel chunk structure
            for (int i = 0; i < voxelData.Length; i++)
            {
                // Do not store this voxel if it lies out of range of the voxel chunk (32x128x32)
                if (voxelData[i].X >= SizeX || voxelData[i].Y >= SizeY || voxelData[i].Z >= SizeZ)
                    continue;

                // Use the voxColors array by default, or overrideColor if it is available
                int voxel = (voxelData[i].X + (voxelData[i].Z << LogX) + (voxelData[i].Y << LogY << LogZ));
                if (colors == null)
                {
                    uint col = SVoxColors[voxelData[i].Color - 1];
                    data[voxel] = new Color32((byte)((col >> 16) & 0xFf), (byte)((col >> 8) & 0xFf), (byte)(col & 0xFf), 0xFf);
                }
                else
                {
                    data[voxel] = colors[voxelData[i].Color - 1];
                }
            }

            return data;
        }
    }
}
