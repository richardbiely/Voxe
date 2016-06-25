using System.Collections.Generic;
using Engine.Scripts.Core.Chunks.Providers;
using Engine.Scripts.Core.Chunks.States;
using UnityEngine;

namespace Engine.Scripts.Core.Chunks.Managers
{
    public class ChunkManager: MonoBehaviour, IChunkManager
    {
        #region Public Fields

        //! Provider of chunks
        public AChunkProvider ChunkProvider;

        #endregion Public Fields

        public int Blocks { get; private set; }

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
            Blocks = 0;

            // Process removal requests
            for (int i = 0; i<m_updateRequests.Count;)
            {
                Chunk chunk = m_updateRequests[i];

                OnProcessChunk(chunk);

                // Process chunk events
                chunk.UpdateChunk();

                Blocks += chunk.NonEmptyBlocks;

                // Automatically collect chunks which are ready to be removed form the world
                if (chunk.StateManager.IsStateCompleted(ChunkState.Remove))
                {
                    // Remove the chunk from our provider and unregister it from chunk storage
                    ChunkProvider.ReleaseChunk(chunk);
                    m_chunks.Remove(chunk.Pos.X, chunk.Pos.Y, chunk.Pos.Z);

                    // Unregister from updates
                    m_updateRequests.RemoveAt(i);
                    continue;
                }

                ++i;
            }
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
        public Chunk GetChunk(int cx, int cy, int cz)
        {
            return m_chunks[cx, cy, cz];
        }

        public void ProcessChunks()
        {
            OnPreProcessChunks();

            ProcessUpdateRequests();

            OnPostProcessChunks();
        }

        public void RegisterChunk(int cx, int cy, int cz)
        {
            Chunk chunk = m_chunks[cx, cy, cz];
            if (chunk!=null)
                return;

            // Let chunk provider hand us a new chunk
            chunk = ChunkProvider.RequestChunk(this, cx, cy, cz);

            // Add the chunk to chunk storage
            m_chunks[cx, cy, cz] = chunk;

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