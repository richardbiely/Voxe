using System;
using System.Text;
using System.Threading;
using Engine.Scripts.Common.DataTypes;
using Engine.Scripts.Common.Events;
using Engine.Scripts.Common.Extensions;
using Engine.Scripts.Common.Threading;
using Engine.Scripts.Core.Blocks;
using Engine.Scripts.Core.Chunks.Providers;
using Engine.Scripts.Core.Chunks.States;
using Engine.Scripts.Core.Threading;
using UnityEngine.Assertions;

namespace Engine.Scripts.Core.Chunks.Managers
{
    /// <summary>
    /// Handles state changes for chunks from a client's perspective.
    /// This means there chunk geometry rendering and chunk neighbors
    /// need to be taken into account.
    /// </summary>
    public class ChunkStateManagerClient : ChunkStateManager
    {
        //! State to notify external listeners about
        private ChunkStateExternal m_stateExternal;

        public ChunkStateManagerClient(Chunk chunk) : base(chunk)
        {
        }

        public override void Init()
        {
            base.Init();

            // Subscribe with neighbors
            SubscribeNeighbors(true);
        }

        public override void Reset()
        {
            base.Reset();

            // Unsubscribe from neighbors
            SubscribeNeighbors(false);

            m_stateExternal = ChunkStateExternal.None;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("N=");
            sb.Append(m_nextState);
            sb.Append(", P=");
            sb.Append(m_pendingStates);
            sb.Append(", C=");
            sb.Append(m_completedStates);
            return sb.ToString();
        }

        public override void SetMeshBuilt()
        {
            m_completedStates = m_completedStatesSafe = m_completedStates.Reset(CurrStateGenerateVertices);
        }

        public override void Update()
        {
            if (m_stateExternal != ChunkStateExternal.None)
            {
                // Notify everyone listening
                NotifyAll(m_stateExternal);

                m_stateExternal = ChunkStateExternal.None;
            }

            // If removal was requested before we got to generating the chunk at all we can safely mark
            // it as removed right away
            if (m_removalRequested && !m_completedStates.Check(ChunkState.Generate))
            {
                m_completedStates = m_completedStates.Set(ChunkState.Remove);
                return;
            }

            // If there is no pending task, there is nothing for us to do
            ProcessNotifyState();
            if (m_pendingStates == 0)
                return;

            // Go from the least important bit to most important one. If a given bit it set
            // we execute the task tied with it
            {
                // In order to save performance, we generate chunk data on-demand - when the chunk can be seen
                if (chunk.PossiblyVisible)
                {
                    if (m_pendingStates.Check(ChunkState.Generate) && GenerateData())
                        return;

                    ProcessNotifyState();
                }

                ProcessNotifyState();
                if (m_pendingStates.Check(ChunkState.FinalizeData) && FinalizeData())
                    return;

                ProcessNotifyState();
                if (m_pendingStates.Check(ChunkState.GenericWork) && PerformGenericWork())
                    return;

                ProcessNotifyState();
                if (m_pendingStates.Check(ChunkState.SaveData) && SaveData())
                    return;

                ProcessNotifyState();
                if (m_pendingStates.Check(ChunkState.Remove) && RemoveChunk())
                    return;

                // In order to save performance, we generate geometry on-demand - when the chunk can be seen
                if (chunk.PossiblyVisible)
                {
                    ProcessNotifyState();
                    if (m_pendingStates.Check(CurrStateGenerateVertices) && GenerateVertices())
                        return;
                }
            }
        }

        private void ProcessNotifyState()
        {
            if (m_nextState == ChunkState.Idle)
                return;

            OnNotified(this, m_nextState);
            m_nextState = ChunkState.Idle;
        }

        public override void OnNotified(IEventSource<ChunkState> source, ChunkState state)
        {
            // Enqueue the request
            m_pendingStates = m_pendingStates.Set(state);
        }

        #region Generic work

        private struct SGenericWorkItem
        {
            public readonly ChunkStateManagerClient Chunk;
            public readonly Action Action;

            public SGenericWorkItem(ChunkStateManagerClient chunk, Action action)
            {
                Chunk = chunk;
                Action = action;
            }
        }

        private static readonly ChunkState CurrStateGenericWork = ChunkState.GenericWork;
        private static readonly ChunkState NextStateGenericWork = ChunkState.Idle;

        private static void OnGenericWork(ref SGenericWorkItem item)
        {
            ChunkStateManagerClient chunk = item.Chunk;

            // Perform the action
            item.Action();

            int cnt = Interlocked.Decrement(ref chunk.m_genericWorkItemsLeftToProcess);
            if (cnt <= 0)
            {
                // Something is very wrong if we go below zero
                Assert.IsTrue(cnt == 0);

                // All generic work is done
                OnGenericWorkDone(chunk);
            }
        }

        private static void OnGenericWorkDone(ChunkStateManagerClient chunk)
        {
            chunk.m_completedStates = chunk.m_completedStates.Set(CurrStateGenericWork);
            chunk.m_nextState = NextStateGenericWork;
            chunk.m_taskRunning = false;
        }

        private bool PerformGenericWork()
        {
            // When we get here we expect all generic tasks to be processed
            Assert.IsTrue(Interlocked.CompareExchange(ref m_genericWorkItemsLeftToProcess, 0, 0) == 0);

            m_pendingStates = m_pendingStates.Reset(CurrStateGenericWork);
            m_completedStates = m_completedStates.Reset(CurrStateGenericWork);
            m_completedStatesSafe = m_completedStates;

            // If there's nothing to do we can skip this state
            if (m_genericWorkItems.Count <= 0)
            {
                m_genericWorkItemsLeftToProcess = 0;
                OnGenericWorkDone(this);
                return false;
            }

            m_taskRunning = true;
            m_genericWorkItemsLeftToProcess = m_genericWorkItems.Count;

            for (int i = 0; i < m_genericWorkItems.Count; i++)
            {
                SGenericWorkItem workItem = new SGenericWorkItem(this, m_genericWorkItems[i]);

                WorkPoolManager.Add(
                    new ThreadPoolItem(
                        chunk.ThreadID,
                        arg =>
                        {
                            SGenericWorkItem item = (SGenericWorkItem)arg;
                            OnGenericWork(ref item);
                        },
                        workItem)
                    );
            }
            m_genericWorkItems.Clear();

            return true;
        }

        public void EnqueueGenericTask(Action action)
        {
            Assert.IsTrue(action != null);
            m_genericWorkItems.Add(action);
            RequestState(ChunkState.GenericWork);
        }

        #endregion

        #region Chunk generation

        private static readonly ChunkState CurrStateGenerateData = ChunkState.Generate;
        private static readonly ChunkState NextStateGenerateData = ChunkState.FinalizeData;

        private static void OnGenerateData(ChunkStateManagerClient stateManager)
        {
            Chunk chunk = stateManager.chunk;
            chunk.Map.ChunkGenerator.Generate(chunk);

            OnGenerateDataDone(stateManager);
        }

        private static void OnGenerateDataDone(ChunkStateManagerClient stateManager)
        {
            stateManager.m_completedStates = stateManager.m_completedStates.Set(CurrStateGenerateData);
            stateManager.m_nextState = NextStateGenerateData;
            stateManager.m_taskRunning = false;
        }

        private bool GenerateData()
        {
            m_pendingStates = m_pendingStates.Reset(CurrStateGenerateData);
            m_completedStates = m_completedStates.Reset(CurrStateGenerateData | CurrStateFinalizeData);
            m_completedStatesSafe = m_completedStates;

            m_taskRunning = true;

            // Let server generate chunk data
            WorkPoolManager.Add(
                new ThreadPoolItem(
                    chunk.ThreadID,
                    arg =>
                    {
                        ChunkStateManagerClient stateManager = (ChunkStateManagerClient)arg;
                        OnGenerateData(stateManager);
                    },
                    this)
                );

            return true;
        }

        #endregion Chunk generation

        #region Chunk finalization

        private static readonly ChunkState CurrStateFinalizeData = ChunkState.FinalizeData;
        private static readonly ChunkState NextStateFinalizeData = ChunkState.BuildVertices;

        private static void OnFinalizeData(ChunkStateManagerClient stateManager)
        {
            Chunk chunk = stateManager.chunk;

            // Generate height limits
            chunk.CalculateProperties();
            // Compress chunk data
            //chunk.Blocks.IsCompressed = true;
            // Compress chunk data
            // Only do this when streaming is enabled for now
            if (EngineSettings.WorldConfig.Streaming)
            {
                chunk.Blocks.RLE.Reset();
                BlockData[] compressedData = chunk.Blocks.ToArray();
                chunk.Blocks.RLE.Compress(ref compressedData);
            }

            OnFinalizeDataDone(stateManager);
        }

        private static void OnFinalizeDataDone(ChunkStateManagerClient stateManager)
        {
            stateManager.m_completedStates = stateManager.m_completedStates.Set(CurrStateFinalizeData);
            stateManager.m_nextState = NextStateFinalizeData;
            stateManager.m_taskRunning = false;
        }

        private bool FinalizeData()
        {
            if (!m_completedStates.Check(ChunkState.Generate))
                return true;

            m_pendingStates = m_pendingStates.Reset(CurrStateFinalizeData);
            m_completedStatesSafe = m_completedStates = m_completedStates.Reset(CurrStateFinalizeData);

            m_taskRunning = true;
            WorkPoolManager.Add(new ThreadPoolItem(
                chunk.ThreadID,
                arg =>
                {
                    ChunkStateManagerClient stateManager = (ChunkStateManagerClient)arg;
                    OnFinalizeData(stateManager);
                },
                this)
            );

            return true;
        }

        #endregion Chunk finalization

        #region Save chunk data

        private struct SSerializeWorkItem
        {
            public readonly ChunkStateManagerClient StateManager;
            public readonly string FilePath;

            public SSerializeWorkItem(ChunkStateManagerClient stateManager, string filePath)
            {
                StateManager = stateManager;
                FilePath = filePath;
            }
        }

        private static readonly ChunkState CurrStateSaveData = ChunkState.SaveData;

        private static void OnSaveData(ChunkStateManagerClient stateManager, string filePath)
        {
            Chunk chunk = stateManager.chunk;

            // !TODO: Handle failure
            ChunkProvider.StoreChunkToDisk(chunk, filePath);

            OnSaveDataDone(stateManager);
        }

        private static void OnSaveDataDone(ChunkStateManagerClient stateManager)
        {
            stateManager.m_stateExternal = ChunkStateExternal.Saved;
            stateManager.m_completedStates = stateManager.m_completedStates.Set(CurrStateSaveData);
            stateManager.m_taskRunning = false;
        }

        private bool SaveData()
        {
            // This state should only be set it streaming is enabled
            Assert.IsTrue(EngineSettings.WorldConfig.Streaming);

            // If chunk was generated...
            if (m_completedStates.Check(ChunkState.Generate))
            {
                // ...  we need to wait until blueprints are generated and chunk is finalized
                if (!m_completedStates.Check(
                    ChunkState.FinalizeData
                    ))
                    return true;
            }

            m_pendingStates = m_pendingStates.Reset(CurrStateSaveData);
            m_completedStates = m_completedStates.Reset(CurrStateSaveData);
            m_completedStatesSafe = m_completedStates;

            ChunkProvider provider = (ChunkProvider)chunk.Map.ChunkProvider;
            SSerializeWorkItem workItem = new SSerializeWorkItem(
                this,
                provider.GetFilePathFromIndex(chunk.Pos.X, chunk.Pos.Y, chunk.Pos.Z)
            );

            m_taskRunning = true;
            IOPoolManager.Add(
                new TaskPoolItem(
                    arg =>
                    {
                        SSerializeWorkItem item = (SSerializeWorkItem)arg;
                        OnSaveData(item.StateManager, item.FilePath);
                    },
                    workItem)
                );

            return true;
        }

        #endregion Save chunk data

        private bool SynchronizeChunk()
        {
            // 6 neighbors are necessary
            if (ListenerCount != 6)
                return false;

            if (!m_completedStates.Check(ChunkState.FinalizeData))
                return false;

            // All neighbors have to have their data loaded
            foreach (var chunkEvent in Listeners)
            {
                var stateManager = (ChunkStateManagerClient)chunkEvent;
                if (!stateManager.m_completedStates.Check(ChunkState.FinalizeData))
                    return false;
            }

            return true;
        }

        #region Generate vertices
        
        private struct SGenerateVerticesWorkItem
        {
            public readonly ChunkStateManagerClient StateManager;
            public readonly int MinX;
            public readonly int MaxX;
            public readonly int MinY;
            public readonly int MaxY;
            public readonly int MinZ;
            public readonly int MaxZ;
            public readonly int LOD;

            public SGenerateVerticesWorkItem(ChunkStateManagerClient stateManager, int minX, int maxX, int minY, int maxY, int minZ, int maxZ, int lod)
            {
                StateManager = stateManager;
                MinX = minX;
                MaxX = maxX;
                MinY = minY;
                MaxY = maxY;
                MinZ = minZ;
                MaxZ = maxZ;
                LOD = lod;
            }
        }

        private static readonly ChunkState CurrStateGenerateVertices = ChunkState.BuildVertices | ChunkState.BuildVerticesNow;

        private static void OnGenerateVerices(ChunkStateManagerClient stateManager, int minX, int maxX, int minY, int maxY, int minZ, int maxZ, int lod)
        {
            Chunk chunk = stateManager.chunk;
            Map map = chunk.Map;

            int offsetX = (chunk.Pos.X << EngineSettings.ChunkConfig.LogSize) << map.VoxelLogScaleX;
            int offsetY = (chunk.Pos.Y << EngineSettings.ChunkConfig.LogSize) << map.VoxelLogScaleY;
            int offsetZ = (chunk.Pos.Z << EngineSettings.ChunkConfig.LogSize) << map.VoxelLogScaleZ;

            map.MeshBuilder.BuildMesh(map, chunk.RenderGeometryBatcher, offsetX, offsetY, offsetZ, minX, maxX, minY, maxY, minZ, maxZ, lod, chunk.Pools);

            OnGenerateVerticesDone(stateManager);
        }

        private static void OnGenerateVerticesDone(ChunkStateManagerClient stateManager)
        {
            stateManager.m_completedStates = stateManager.m_completedStates.Set(CurrStateGenerateVertices);
            stateManager.m_taskRunning = false;
        }

        /// <summary>
        ///     Build this chunk's geometry
        /// </summary>
        private bool GenerateVertices()
        {
            if (!SynchronizeChunk())
                return true;

            bool priority = m_pendingStates.Check(ChunkState.BuildVerticesNow);

            m_pendingStates = m_pendingStates.Reset(CurrStateGenerateVertices);
            m_completedStates = m_completedStates.Reset(CurrStateGenerateVertices);
            m_completedStatesSafe = m_completedStates;

            if (chunk.NonEmptyBlocks > 0)
            {
                var workItem = new SGenerateVerticesWorkItem(
                    this,
                    chunk.MinRenderX, chunk.MaxRenderX,
                    chunk.MinRenderY, chunk.MaxRenderY,
                    chunk.MinRenderZ, chunk.MaxRenderZ,
                    chunk.LOD
                    );

                m_taskRunning = true;
                WorkPoolManager.Add(
                    new ThreadPoolItem(
                        chunk.ThreadID,
                        arg =>
                        {
                            SGenerateVerticesWorkItem item = (SGenerateVerticesWorkItem)arg;
                            OnGenerateVerices(item.StateManager, item.MinX, item.MaxX, item.MinY, item.MaxY, item.MinZ, item.MaxZ, item.LOD);
                        },
                        workItem,
                        priority ? Globals.Watch.ElapsedTicks : long.MaxValue)
                    );
            }
            else
            {
                OnGenerateVerticesDone(this);
            }

            return true;
        }

        #endregion Generate vertices

        #region Remove chunk

        private static readonly ChunkState CurrStateRemoveChunk = ChunkState.Remove;

        private bool RemoveChunk()
        {
            // Wait until all generic tasks are processed
            if (Interlocked.CompareExchange(ref m_genericWorkItemsLeftToProcess, 0, 0) != 0)
            {
                Assert.IsTrue(false);
                return true;
            }

            // If chunk was generated we need to wait for other states with higher priority to finish first
            if (m_completedStates.Check(ChunkState.Generate))
            {
                // With streaming enabled we have to wait for serialization to finish as well
                if (EngineSettings.WorldConfig.Streaming)
                {
                    // Data needs to be finalized
                    if (!m_completedStates.Check(ChunkState.FinalizeData))
                        return true;

                    // Wait for serialization to finish as well
                    if (!m_completedStates.Check(ChunkState.SaveData))
                        return true;
                }
                else
                {
                    // Data needs to be finalized
                    if (!m_completedStates.Check(ChunkState.FinalizeData))
                        return true;
                }
            }

            m_completedStatesSafe = m_completedStates = m_completedStates.Set(CurrStateRemoveChunk);
            return true;
        }

        #endregion Remove chunk

        private void SubscribeNeighbors(bool subscribe)
        {
            Vector3Int pos = chunk.Pos;
            SubscribeTwoNeighbors(pos.X + 1, pos.Y, pos.Z, subscribe);
            SubscribeTwoNeighbors(pos.X - 1, pos.Y, pos.Z, subscribe);
            SubscribeTwoNeighbors(pos.X, pos.Y + 1, pos.Z, subscribe);
            SubscribeTwoNeighbors(pos.X, pos.Y - 1, pos.Z, subscribe);
            SubscribeTwoNeighbors(pos.X, pos.Y, pos.Z + 1, subscribe);
            SubscribeTwoNeighbors(pos.X, pos.Y, pos.Z - 1, subscribe);
        }

        private void SubscribeTwoNeighbors(int cx, int cy, int cz, bool subscribe)
        {
            Chunk neighbor = chunk.Map.GetChunk(cx, cy, cz);
            if (neighbor != null)
            {
                ChunkStateManagerClient stateManager = neighbor.StateManager;
                // Subscribe with each other. Passing Idle as event - it is ignored in this case anyway
                stateManager.Subscribe(this, ChunkState.Idle, subscribe);
                Subscribe(stateManager, ChunkState.Idle, subscribe);
            }
        }
    }
}
