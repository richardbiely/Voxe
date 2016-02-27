using System.Collections.Generic;
using Assets.Engine.Scripts.Common.DataTypes;
using Assets.Engine.Scripts.Core.Chunks.Providers;
using Assets.Engine.Scripts.Core.Threading;
using UnityEngine;

namespace Assets.Engine.Scripts.Core.Chunks
{
    public class ChunkManager: MonoBehaviour, IChunkManager
    {
        #region Public Fields

        //! Provider of chunks
        public AChunkProvider ChunkProvider;

        #endregion Public Fields

        public int Chunks
        {
            get { return m_chunks.Count; }
        }

        public bool IsEmpty
        {
            get { return m_chunks.Count<=0; }
        }

        #region Helper methods

        private void ProcessUpdateRequests()
        {
            // Process removal requests
            for (int i = 0; i<m_updateRequests.Count;)
            {
                Chunk chunk = m_updateRequests[i];

                OnProcessChunk(chunk);

                // Process chunk events
                chunk.UpdateChunk();

                // Automatically collect chunks which are ready to be removed form the world
                if (chunk.IsFinished())
                {
                    // Remove the chunk from our provider and unregister it from chunk storage
                    ChunkProvider.ReleaseChunk(chunk);
                    m_chunks.Remove(chunk.Pos.X, chunk.Pos.Z);

                    // Unregister from updates
                    m_updateRequests.RemoveAt(i);
                    continue;
                }

                ++i;
            }

            // Commit collected work items
            WorkPoolManager.Commit();
            IOPoolManager.Commit();
        }

        #endregion

        #region Private vars

        //! Chunk storage
        protected readonly ChunkStorage m_chunks = new ChunkStorage();
        
        //! A list of chunks to update
        private readonly List<Chunk> m_updateRequests = new List<Chunk>();

        #endregion Private vars

        #region Unity overrides

        private void Awake()
        {
            OnAwake();
        }

        private void Start()
        {
            OnStart();
        }

        #endregion Unity overrides

        #region Public methods

        /// <summary>
        ///     Returns a chunk at given chunk coordinates.
        /// </summary>
        public Chunk GetChunk(int cx, int cz)
        {
            return m_chunks[cx, cz];
        }

        public void ProcessChunks()
        {
            OnPreProcessChunks();

            ProcessUpdateRequests();

            OnPostProcessChunks();
        }

        public void RegisterChunk(Vector2Int pos)
        {
            Chunk chunk = m_chunks[pos.X, pos.Z];
            if (chunk!=null)
                return;

            // Let chunk provider hand us a new chunk
            chunk = ChunkProvider.RequestChunk(this, pos.X, pos.Z);

            // Add the chunk to chunk storage
            m_chunks[pos.X, pos.Z] = chunk;

            // Register for updates
            m_updateRequests.Add(chunk);
        }

        public void UnregisterAll()
        {
            foreach (Chunk chunk in m_chunks.Values)
                chunk.Finish();
        }

        #endregion

        #region Methods to override by a child class

        protected virtual void OnAwake()
        {
        }

        protected virtual void OnStart()
        {
        }

        protected virtual void OnPreProcessChunks()
        {
        }

        protected virtual void OnProcessChunk(Chunk chunk)
        {
        }

        protected virtual void OnPostProcessChunks()
        {
        }

        #endregion
    }
}