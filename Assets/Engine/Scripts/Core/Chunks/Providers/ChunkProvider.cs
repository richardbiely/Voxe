using System;
using System.IO;
using System.Text;
using Assets.Engine.Scripts.Common.IO;
using Assets.Engine.Scripts.Common.IO.RLE;
using Assets.Engine.Scripts.Core.Blocks;
using UnityEngine;
using UnityEngine.Assertions;

namespace Assets.Engine.Scripts.Core.Chunks.Providers
{
    /// <summary>
    ///     A purely local chunk provider
    /// </summary>
    public class ChunkProvider: AChunkProvider
    {
        #region Private vars

        //! Persistent data to be stored in <disk>:/Users/<user>/AppData/LocalLow/RBiely/Voxe/VoxelData
        private string m_dataPath;

        //! String builder used in the main thread to determine a file path for a given chunk
        private StringBuilder m_filePathStringBuilder;

        #endregion Private vars

        public string Path;

        #region Unity overrides

        private void Awake()
        {
            m_dataPath = string.Format("{0}/{1}", Application.persistentDataPath, Path.Length==0 ? "VoxelData" : Path);
            m_filePathStringBuilder = new StringBuilder(m_dataPath.Length + 21);
        }

        #endregion

        #region Public Methods

        public string GetFilePathFromIndex(int cx, int cy, int cz)
        {
            // E.g. D:\VoxelData\0FF21_22F00_00001.chn
            m_filePathStringBuilder.Remove(0, m_filePathStringBuilder.Length);
            m_filePathStringBuilder.AppendFormat(@"{0}\{1}_{2}_{3}.chn", m_dataPath, cx.ToString("X8"), cy.ToString("X8"), cz.ToString("X8"));
            return m_filePathStringBuilder.ToString();
        }

        #endregion Public Methods

        #region Threading

        private static bool LoadChunkFromDisk(Chunk chunk, string filePath)
        {
            try
            {
                // Read data from file
                byte[] filedata;

                FileStream fs = null;
                try
                {
                    fs = new FileStream(filePath, FileMode.Open);
                    using (var br = new BinaryReader(fs))
                    {
                        // Read filled block count
                        chunk.NonEmptyBlocks = br.ReadInt16();

                        // Read section offsets
                        chunk.MaxRenderY = br.ReadInt16();
                        chunk.MinRenderY = br.ReadInt16();

                        // Read chunk data
                        filedata = br.ReadBytes((int)(fs.Length - 6)); // 6 = size of previous data (NonEmptyBlocks + MaxRenderY + MinRenderY)

                        fs = null;
                    }
                }
                finally
                {
                    if (fs!=null)
                        fs.Dispose();
                }

                // Convert byte array to array of BlockData structs
                chunk.Blocks.RLE.Assign(StructSerializers.DeserializeArrayToList<RLEDataPair<BlockData>>(ref filedata));
                // Decompress data
                //chunk.Blocks.IsCompressed = false;
                var decompressedData = chunk.Blocks.RLE.Decompress();
                chunk.Blocks.Set(ref decompressedData);
            }
            catch (Exception ex)
            {
                string s = string.Format("Cannot load chunk '{0}' ([{1},{2},{3}]): {4}", filePath, chunk.Pos.X, chunk.Pos.Y, chunk.Pos.Z, ex);
                Debug.LogError(s);
                return false;
            }
            
            return true;
        }

        

        public static bool StoreChunkToDisk(Chunk chunk, string filePath)
        {
            try
            {
                // Make sure the data is compressed
                //chunk.Blocks.IsCompressed = true;
                // Serialize compressed data
                var buff = StructSerializers.SerializeArray(chunk.Blocks.RLE.List);

                FileStream fs = null;
                try
                {
                    fs = new FileStream(filePath, FileMode.Create);
                    using (BinaryWriter bw = new BinaryWriter(fs))
                    {
                        fs = null;

                        // Store number of filled block for each section. Using short limits max section size to 16x16x16
                        bw.Write((short)chunk.NonEmptyBlocks);

                        // Store chunk offsets
                        bw.Write((short)chunk.MaxRenderY);
                        bw.Write((short)chunk.MinRenderY);

                        // Store block data
                        bw.Write(buff);
                    }
                }
                finally
                {
                    if (fs!=null)
                        fs.Dispose();
                }
            }
            catch (Exception ex)
            {
                Debug.LogErrorFormat("Cannot save chunk '{0}' ([{1},{2},{3}]): {4}", filePath, chunk.Pos.X, chunk.Pos.Y, chunk.Pos.Z, ex);
                return false;
            }

            return true;
        }
        
        private static void RequestChunkFromDisk(Chunk chunk, string filePath)
        {
            Globals.IOPool.AddItem(arg =>
                {
                    if (LoadChunkFromDisk(chunk, filePath))
                    {
                        chunk.MarkAsLoaded();
                        chunk.RegisterNeighbors();
                        return;
                    }

                    // File could not be read for some reason, delete it
                    File.Delete(filePath);

                    // Request new data from the internet
                    GenerateChunk(chunk);
                },
                chunk);
        }

        #endregion Threading

        #region IChunkProvider implementation
        
        // load or generate a chunk
        public override Chunk RequestChunk(ChunkManager map, int cx, int cy, int cz)
        {
            Chunk chunk = Globals.Pools.ChunkPool.Pop();
#if DEBUG
            Assert.IsTrue(!chunk.IsUsed, "Popped a chunk which is still in use!");
#endif

            chunk.Init((Map)map, cx, cy, cz);

            if (EngineSettings.WorldConfig.Streaming)
            {
                string filePath = GetFilePathFromIndex(chunk.Pos.X, chunk.Pos.Y, chunk.Pos.Z);
                if (File.Exists(filePath))
                {
                    // Data is on disk
                    RequestChunkFromDisk(chunk, filePath);
                }
                else
                {
                    // Data is not on disk, it cannot be open or decompressed or timestamp is too old
                    GenerateChunk(chunk);
                }

            }
            else
            {
                GenerateChunk(chunk);
            }

            return chunk;
        }

        // save chunk to disk
        public override bool ReleaseChunk(Chunk chunk)
        {
            chunk.Reset();
#if DEBUG
            chunk.IsUsed = false;
#endif
            Globals.Pools.ChunkPool.Push(chunk);

            return true;
        }

        #endregion IChunkProvider implementation

        private static void GenerateChunk(Chunk chunk)
        {
            chunk.RegisterNeighbors();
        }
    }
}