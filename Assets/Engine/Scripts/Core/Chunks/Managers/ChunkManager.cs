using System;
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

        #region Private vars

        //! Chunk storage
        protected Dictionary<Vector2Int, ChunkController> m_chunks;

        //! A list of chunks to load
        private List<ChunkController> m_chunksToLoad;

        //! A list of chunks to remove
        private List<ChunkController> m_chunksToRemove;

        [Flags]
        protected enum RequestFlags: byte
        {
            Load = 0x01,
            Unload = 0x02
        }

        protected class ChunkController
        {
            public RequestFlags Flags { get; set; }
            public Chunk Chunk { get; set; }
            public Vector2Int Pos { get; set; }
        }

        #endregion Private vars

        #region Unity overrides

        private void Awake()
        {
            m_chunks = new Dictionary<Vector2Int, ChunkController>();
            m_chunksToLoad = new List<ChunkController>();
            m_chunksToRemove = new List<ChunkController>();

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
            ChunkController controller;
            if (!m_chunks.TryGetValue(new Vector2Int(cx, cz), out controller))
                return null;

            Debug.Assert(controller!=null);
            return controller.Chunk;
        }

        public void ProcessChunks()
        {
            OnPreProcessChunks();

            ProcessUnloadRequests();
            ProcessLoadRequests();

            // Commit collected work items
            WorkPoolManager.Commit();
            IOPoolManager.Commit();

            foreach (ChunkController controller in m_chunks.Values)
            {
                Chunk chunk = controller.Chunk;
                OnProcessChunk(chunk);

                // Automatically collect chunks which are ready to be removed form the world
                if (chunk.IsFinished())
                {
                    // Reset loading flag
                    controller.Flags &= ~RequestFlags.Load;
                    // Request removal
                    controller.Flags |= RequestFlags.Unload;
                    // Register our request
                    m_chunksToRemove.Add(controller);
                }
            }

            // Commit collected work items
            WorkPoolManager.Commit();
            IOPoolManager.Commit();

            OnPostProcessChunks();
        }

        public void RegisterChunk(Vector2Int pos)
        {
            ChunkController controller;

            if (m_chunks.TryGetValue(pos, out controller))
            {
                // There should always be a valid controller inside!
                Debug.Assert(controller!=null);

                // Reset the flags
                controller.Flags &= ~RequestFlags.Load;
                controller.Flags &= ~RequestFlags.Unload;

                Debug.Assert(controller.Chunk != null);

                // Make sure the chunk will no longer want to be unloaded
                //controller.Chunk.Restore();
            }
            else
            {
                controller = new ChunkController
                {
                    Chunk = null,
                    Flags = RequestFlags.Load,
                    Pos = pos
                };

                m_chunksToLoad.Add(controller);
            }
        }

        public void UnregisterChunk(Vector2Int pos)
        {
            ChunkController controller;

            // Make sure that a chunk exists
            if (!m_chunks.TryGetValue(pos, out controller))
                return;

            // There should always be a valid controller inside!
            Debug.Assert(controller!=null);

            // Reset loading flag
            controller.Flags &= ~RequestFlags.Load;

            // Ignore the request if there already has been one
            if ((controller.Flags&RequestFlags.Unload)==RequestFlags.Unload)
                return;

            controller.Flags |= RequestFlags.Unload;

            // Chunk has to exist
            Debug.Assert(controller.Chunk!=null);

            m_chunksToRemove.Add(controller);
        }

        public void UnregisterAll()
        {
            m_chunksToLoad.Clear();
            m_chunksToRemove.Clear();

            foreach (ChunkController controller in m_chunks.Values)
            {
                controller.Flags &= ~RequestFlags.Load;
                controller.Flags |= RequestFlags.Unload;

                m_chunksToRemove.Add(controller);
            }
        }

        public int ChunkCount
        {
            get { return m_chunks.Count; }
        }

        public bool IsEmpty()
        {
            return m_chunks.Count<=0;
        }

        #endregion

        #region Methods to override by a child class

        protected virtual void OnAwake()
        {
        }

        protected virtual void OnStart()
        {
        }

        protected virtual void OnPostProcessChunks()
        {
        }

        protected virtual void OnProcessChunk(Chunk chunk)
        {
        }

        protected virtual void OnPreProcessChunks()
        {
        }

        #endregion

        #region Helper methods

        private void ProcessLoadRequests()
        {
            // Process loading requests
            for (int i = 0; i<m_chunksToLoad.Count; i++)
            {
                ChunkController controller = m_chunksToLoad[i];

                // An unload request might have overriden this load request
                if ((controller.Flags&RequestFlags.Load)!=RequestFlags.Load)
                    continue;

                // Request a new chunk from our provider and register it in chunk storage
                controller.Chunk = ChunkProvider.RequestChunk(this, controller.Pos.X, controller.Pos.Z);
                m_chunks.Add(controller.Pos, controller);
            }
            m_chunksToLoad.Clear();
        }

        private void ProcessUnloadRequests()
        {
            // Process removal requests
            for (int i = 0; i<m_chunksToRemove.Count;)
            {
                ChunkController controller = m_chunksToRemove[i];

                // A load request might have overriden this unload request
                if ((controller.Flags&RequestFlags.Unload)!=RequestFlags.Unload)
                {
                    m_chunksToRemove.RemoveAt(i);
                    continue;
                }

                Chunk chunk = controller.Chunk;

                // We need to make sure that the chunk is ready to be released
                if (!chunk.Finish())
                {
                    ++i;
                    continue;
                }

                // Remove the chunk from our provider and unregister it from chunk storage
                ChunkProvider.ReleaseChunk(chunk);
                m_chunks.Remove(chunk.Pos);

                m_chunksToRemove.RemoveAt(i);
            }
        }

        #endregion
    }
}