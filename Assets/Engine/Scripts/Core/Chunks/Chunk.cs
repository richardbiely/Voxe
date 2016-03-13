using System;
using Assets.Engine.Scripts.Common.DataTypes;
using Assets.Engine.Scripts.Common.Extensions;
using Assets.Engine.Scripts.Common.Threading;
using Assets.Engine.Scripts.Core.Blocks;
using Assets.Engine.Scripts.Core.Threading;
using UnityEngine;
using Assert = UnityEngine.Assertions.Assert;
using System.Collections.Generic;
using System.Threading;
using Assets.Engine.Scripts.Builders;
using Assets.Engine.Scripts.Core.Chunks.Providers;
using Assets.Engine.Scripts.Rendering;

namespace Assets.Engine.Scripts.Core.Chunks
{
    /// <summary>
    ///     Represents a chunk consisting of several even sized mini chunks
    /// </summary>
	public class Chunk: ChunkEvent, IOcclusionEntity
    {
        private static int s_id = 0;

        #region Public variables
        
        public Map Map { get; private set; }
        
        public readonly BlockStorage Blocks;
        
        //! Bounding box in world coordinates
        public Bounds WorldBounds { get; private set; }

        // Chunk coordinates
        public Vector3Int Pos { get; private set; }

        //! Chunk bound in terms of geometry
        public int MaxRenderY { get; set; }
        public int MinRenderY { get; set; }
        public int MaxRenderX { get; private set; }
        public int MinRenderX { get; private set; }
        public int MinRenderZ { get; private set; }
        public int MaxRenderZ { get; private set; }
        private int m_lowestEmptyBlock;
        
        //! Chunk's level of detail. 0=max detail. Every other LOD half the detail of a previous one
        public int LOD {
            get
            {
                return m_lod;
            }
            set
            {
                if (value!=m_lod)
                {
                    // Request new lod
                    m_lod = value;

                    // Request an update of geometry
                    RefreshState(ChunkState.BuildVertices);
                }
            }
        }

        //! True if MiniSection has already been built
        public bool IsBuilt { get; set; }

        //! Number of blocks which not air (non-empty blocks)
        public int NonEmptyBlocks { get; set; }

        public bool PossiblyVisible { get; private set; }

        public bool RequestedRemoval;

#if     DEBUG
        public bool IsUsed = false;
#endif

        #endregion Public variables

        #region Private variables

        //! ID of a thread from a thread pool - each chunk is associated with a specific thread pool thread
        private readonly int m_threadID;

        //! Object pools used by this chunk
        public LocalPools Pools { get; private set; }

        //! A list of event requiring counter
        private readonly int [] m_eventCnt = {0, 0};
        
        //! Queue of setBlock operations to execute
        private readonly List<SetBlockContext> m_setBlockQueue = new List<SetBlockContext>();

        //! Next state after currently finished state
        private ChunkState m_notifyState;
		//! Tasks waiting to be executed
		private ChunkState m_pendingTasks;
        //! States to be refreshed
        private ChunkState m_refreshTasks;
		//! Tasks already executed
		private ChunkState m_completedTasks;

        //! First finalization differs from subsequent ones
        private bool m_firstFinalization;

        //! Chunk's current level of detail
        private int m_lod;

		//! Specifies whether there's a task running
		private bool m_taskRunning;
		private readonly object m_lock = new object();

        //! A list of generic tasks a chunk has to perform
        private readonly List<Action> m_genericWorkItems = new List<Action>();
        //! Number of generic tasks waiting to be finished
        private int m_genericWorkItemsLeftToProcess;

        //! Manager taking care of render calls
        private readonly DrawCallBatcher m_drawCallBatcher;

        #endregion Private variables

        #region Constructors

        public Chunk():
			base(6)
        {
            Blocks = new BlockStorage();

            m_threadID = Globals.WorkPool.GetThreadIDFromIndex(s_id++);
            Pools = Globals.WorkPool.GetPool(m_threadID);

            m_drawCallBatcher = new DrawCallBatcher(Globals.CubeMeshBuilder, this);
            BBoxVertices = new List<Vector3>();
            BBoxVerticesTransformed = new List<Vector3>();

            Reset();
        }

        #endregion Constructors

        #region Accessors
        
        /// <summary>
        ///     Access a block using 3D coordinates
        /// </summary>
        public BlockData this[int x, int y, int z]
        {
            get
            {
                return Blocks[x,y,z];
            }
            set
            {
                Blocks[x,y,z] = value;
            }
        }

        #endregion Accessors
        
        public void Init(Map map, int cx, int cy, int cz)
        {
            Map = map;
            Pos = new Vector3Int(cx, cy, cz);
            m_lod = 0;

            int sizeX = EngineSettings.ChunkConfig.Size<<map.VoxelLogScaleX;
            int sizeY = EngineSettings.ChunkConfig.Size<<map.VoxelLogScaleY;
            int sizeZ = EngineSettings.ChunkConfig.Size<<map.VoxelLogScaleZ;
            WorldBounds = new Bounds(
                new Vector3(sizeX*(cx+0.5f), sizeY*(cy+0.5f), sizeZ*(cz+0.5f)),
                new Vector3(sizeX, sizeY, sizeZ)
                );
        }

		public void RegisterNeighbors()
		{
            // Retrieve neighbors
			Chunk left = Map.GetChunk(Pos.X - 1, Pos.Y, Pos.Z);
			Chunk right = Map.GetChunk(Pos.X + 1, Pos.Y, Pos.Z);
			Chunk front = Map.GetChunk(Pos.X, Pos.Y, Pos.Z - 1);
			Chunk behind = Map.GetChunk(Pos.X, Pos.Y, Pos.Z + 1);
		    Chunk top = Map.GetChunk(Pos.X, Pos.Y+1, Pos.Z);
		    Chunk bottom = Map.GetChunk(Pos.X, Pos.Y-1, Pos.Z);

			// Register neighbors
			RegisterNeighbor(left);
			RegisterNeighbor(right);
			RegisterNeighbor(front);
			RegisterNeighbor(behind);
            RegisterNeighbor(top);
            RegisterNeighbor(bottom);
        }

        #region Public Methods
        
        public void UpdateChunk()
        {
            if (!m_pendingTasks.Check(ChunkState.Remove))
                Restore();

			ProcessSetBlockQueue();
			ProcessPendingTasks(PossiblyVisible);

            if (m_completedTasks.Check(ChunkState.BuildVertices))
                Build();
        }

        public void Reset()
        {
            // Make sure the number of items left to process is zero!
            while (0!=Interlocked.CompareExchange(ref m_genericWorkItemsLeftToProcess, 0, 0))
            {
                Assert.IsTrue(false, "Attempting to release a chunk with unprocessed generic work items");
            }
            m_genericWorkItemsLeftToProcess = 0;

            UnregisterFromNeighbors();
            
            // Reset chunk events
            for (int i = 0; i<m_eventCnt.Length; i++)
				m_eventCnt[i] = 0;

			RequestedRemoval = false;
			m_taskRunning = false;
            m_firstFinalization = true;

            m_lod = 0;

            m_notifyState = m_notifyState.Reset();            
			m_pendingTasks = m_pendingTasks.Reset();            
            m_completedTasks = m_completedTasks.Reset();
            m_refreshTasks = m_refreshTasks.Reset();
            
            m_setBlockQueue.Clear();

            // Reset blocks
            Blocks.Reset();
            
            PossiblyVisible = false;

            MinRenderX = EngineSettings.ChunkConfig.Mask;
            MaxRenderX = 0;
            MinRenderY = EngineSettings.ChunkConfig.Mask;
            MaxRenderY = 0;
            MinRenderZ = EngineSettings.ChunkConfig.Mask;
            MaxRenderZ = 0;
            m_lowestEmptyBlock = EngineSettings.ChunkConfig.Mask;

            // Reset sections
            NonEmptyBlocks = 0;
            IsBuilt = false;

            m_drawCallBatcher.Clear();

            ResetGeometryBoundingMesh();

            ResetEvent();
        }

		/// <summary>
		///     Changes chunk's visibility
		/// </summary>
        public void SetVisible(bool show)
        {
            Visible = show;
        }

        public void SetPossiblyVisible(bool show)
        {
            PossiblyVisible = show;
        }

		/// <summary>
		///     Changes a given block to a block of a different type
		/// </summary>
		public void ModifyBlock(int x, int y, int z, BlockData blockData)
		{
			BlockData thisBlock = this[x, y, z];

			// Do nothing if there's no change
			if (blockData.CompareTo(thisBlock)==0)
				return;

			// Meta data must be different as well
			// TODO: Not good. When trying to change an undamaged block into a different
			// undamaged block the following condition would be hit.
			//if (blockData.GetMeta()==thisBlock.GetMeta())
				//return;

			QueueSetBlock(this, x, y, z, blockData);
		}

        /// <summary>
        ///     Chunks whether the chunk resides within camera frustum
        /// </summary>
        public bool CheckFrustum(Plane[] frustum)
        {
            return GeometryUtility.TestPlanesAABB(frustum, WorldBounds);
        }

        public void ResetGeometryBoundingMesh()
        {
            GeometryBounds = new Bounds();
            BBoxVertices.Clear();
            BBoxVerticesTransformed.Clear();
        }
       
        public void Build()
        {
            if (IsBuilt)
                return;

            // Make sure the data is not regenerated all the time
            IsBuilt = true;

            // Prepare chunk for rendering
            m_drawCallBatcher.Commit();
        }

        public void BuildGeometryBoundingMesh(ref Bounds bounds)
        {
            GeometryBounds = bounds;

            // Build a bounding box for the mini chunk
            CubeBuilderSimple.Build(BBoxVertices, ref bounds, Pools);

            // Make a copy of the bounding box
            BBoxVerticesTransformed.AddRange(BBoxVertices);
        }

        private void AdjustMinMaxRenderBounds(int x, int y, int z, BlockData data)
        {
            bool isEmpty = data.IsEmpty();
            if (!isEmpty)
            {
                ++NonEmptyBlocks;

                if (x < MinRenderX)
                    MinRenderX = x;
                if (z < MinRenderZ)
                    MinRenderZ = z;
                if (y < MinRenderY)
                    MinRenderY = y;

                if (x > MaxRenderX)
                    MaxRenderX = x;
                if (z > MaxRenderZ)
                    MaxRenderZ = z;
                if (y > MaxRenderY)
                    MaxRenderY = y;
            }
            else if (y < m_lowestEmptyBlock)
                m_lowestEmptyBlock = y;
        }

        public void GenerateBlock(int x, int y, int z, BlockData data)
        {
            Blocks[x, y, z] = data;
            AdjustMinMaxRenderBounds(x, y, z, data);
        }

        // Calculate lowest empty and highest solid block position
		/* TODO: Lowest/highest block can be computed while the terrain is generated. This
		 * would speed things up for initial chunk generation.
		*/
        public void CalculateProperties()
        {
            if (m_firstFinalization)
            {
                m_firstFinalization = false;
            }
            else
            {
                NonEmptyBlocks = 0;
                MinRenderX = EngineSettings.ChunkConfig.Mask;
                MaxRenderX = 0;
                MinRenderY = EngineSettings.ChunkConfig.Mask;
                MaxRenderY = 0;
                MinRenderZ = EngineSettings.ChunkConfig.Mask;
                MaxRenderZ = 0;
                m_lowestEmptyBlock = EngineSettings.ChunkConfig.Mask;

                for (int y = EngineSettings.ChunkConfig.Mask; y>=0; y--)
                {
                    for (int z = 0; z<EngineSettings.ChunkConfig.Size; z++)
                    {
                        for (int x = 0; x<EngineSettings.ChunkConfig.Size; x++)
                        {
                            AdjustMinMaxRenderBounds(x,y,z,Blocks[x,y,z]);
                        }
                    }
                }
            }

            // This is an optimization - if this chunk is flat than there's no need to consider it as a whole.
            // Its' top part is sufficient enough. However, we never want this value be smaller than chunk's
            // lowest solid part.
            // E.g. a sphere floating above the group would be considered from its topmost solid block to
            // the ground without this. With this check, the lowest part above ground will be taken as minimum
            // render value.
            MinRenderY = Mathf.Max(m_lowestEmptyBlock-1, MinRenderY);
            MinRenderY = Mathf.Max(MinRenderY, 0);

            if (NonEmptyBlocks > 0)
            {
                int posInWorldX = (Pos.X<<EngineSettings.ChunkConfig.LogSize)<<Map.VoxelLogScaleX;
                int posInWorldY = (Pos.Y<<EngineSettings.ChunkConfig.LogSize)<<Map.VoxelLogScaleY;
                int posInWorldZ = (Pos.Z<<EngineSettings.ChunkConfig.LogSize)<<Map.VoxelLogScaleZ;

                // Build bounding mesh for each section
                float width = (MaxRenderX - MinRenderX + 1) << Map.VoxelLogScaleX;
                float height = (MaxRenderY - MinRenderY + 1) << Map.VoxelLogScaleY;
                float depth = (MaxRenderZ - MinRenderZ + 1) << Map.VoxelLogScaleZ;
                
                Bounds geomBounds = new Bounds(
                    new Vector3(
                        posInWorldX + MinRenderX + width *0.5f,
                        posInWorldY + MinRenderY + height *0.5f,
                        posInWorldZ + MinRenderZ + depth *0.5f
                        ),
                    new Vector3(width, height, depth)
                    );
                BuildGeometryBoundingMesh(ref geomBounds);
            }
            else
            {
                ResetGeometryBoundingMesh();
            }
        }

        private void Restore()
        {
			if (!RequestedRemoval)
				return;

			RequestedRemoval = false;

			if (EngineSettings.WorldConfig.Streaming)
				m_pendingTasks = m_pendingTasks.Reset(ChunkState.Serialize);
			m_pendingTasks = m_pendingTasks.Reset(ChunkState.Remove);
        }

        public void Finish()
        {
            if (RequestedRemoval)
                return;

            RequestedRemoval = true;

            if (EngineSettings.WorldConfig.Streaming)
                OnNotified(ChunkState.Serialize);
            OnNotified(ChunkState.Remove);            
        }

#region ChunkEvent implementation

		public override void OnRegistered(bool registerListener)
		{
			// Nothing to do when unsubscribing
			if (!registerListener)
				return;

			OnNotified(ChunkState.Generate);
		}

		public override void OnNotified(ChunkState state)
		{
		    int eventIndex = -1;
		    switch (state)
		    {
#if ENABLE_BLUEPRINTS
                case ChunkState.GenerateBlueprints:
                    eventIndex = 0;
                break;
#endif
                case ChunkState.BuildVertices:
		            eventIndex = 1;
                break;
		    }

		    if (eventIndex>=0)
		    {
                // Check completition
                int cnt = ++m_eventCnt[eventIndex];
                if (cnt < Subscribers.Length)
                    return;

                // Reset counter and process/queue event
                m_eventCnt[eventIndex] = 0;
            }

            // Queue operation
            m_pendingTasks = m_pendingTasks.Set(state);
		}

#endregion ChunkEvent implementation

#region Chunk generation
        
        public void MarkAsLoaded()
		{
			m_completedTasks = m_completedTasks.Set(
                ChunkState.Generate|
#if ENABLE_BLUEPRINTS
                ChunkState.GenerateBlueprints|
#endif
                ChunkState.FinalizeData
                );
		}

		private void RegisterNeighbor(Chunk neighbor)
        {
            if (neighbor == null)
                return;
            
            bool neighborWasRegistered = neighbor.IsRegistered();

            // Registration needs to be done both ways
            neighbor.Register(this, true);
            Register(neighbor, true);

            // Neighbor just got registered
            if (neighbor.IsRegistered() && !neighborWasRegistered)
            {
                ChunkState currState;
                lock (m_lock)
                {
                    currState = m_completedTasks;
                }
                // We have to take a special here for there are events which are distributed to all neighbors once a specific task completes.
                // For instance, once Generate is finished, GenerateBlueprints is sent to all neighbors. If a chunk gets unregistered
                // and registered back again, it would no longer receive the event. We therefore need to notify it again
#if ENABLE_BLUEPRINTS
                if (currState.Check(ChunkState.Generate))
                    neighbor.OnNotified(ChunkState.GenerateBlueprints);
#endif
                if (currState.Check(ChunkState.FinalizeData))
                    neighbor.OnNotified(ChunkState.BuildVertices);
            }
        }

		private void UnregisterFromNeighbors()
		{
			// Remove this section from its subscribers
			foreach (var info in Subscribers)
			{
				ChunkEvent subscriber = info;
				if (subscriber==null)
					continue;

				// Unregistration needs to be done both ways
				subscriber.Register(this, false);
				Register(subscriber, false);
			}
		}

        private bool IsFinished_Internal()
        {
            return m_completedTasks.Check(ChunkState.Remove);
        }

		public bool IsFinished()
		{
		    lock (m_lock)
		    {
                return IsFinished_Internal();
		    }
		}

        public bool IsFinalized()
        {
            lock (m_lock)
            {
                return m_completedTasks.Check(ChunkState.FinalizeData);
            }
        }

        private bool IsExecutingTask_Internal()
        {
            return m_taskRunning;
        }

        public bool IsExecutingTask()
        {
            lock (m_lock)
            {
                return IsExecutingTask_Internal();
            }
        }

        private void ProcessNotifyState()
        {
            if (m_notifyState==ChunkState.Idle)
                return;

            // Notify neighbors about our state.
            // States after GenerateBlueprints are handled differently because they are related only
            // to the chunk itself rather than chunk's neighbors
            switch (m_notifyState)
            {
#if ENABLE_BLUEPRINTS
                case ChunkState.GenerateBlueprints:
#endif
                case ChunkState.BuildVertices:
                    NotifyAll(m_notifyState);
                    break;
                default:
                    OnNotified(m_notifyState);
                    break;
            }

            m_notifyState = ChunkState.Idle;
        }
        
        public void ProcessPendingTasks(bool possiblyVisible)
        {
            lock (m_lock)
            {
                // We are not allowed to check anything as long as there is a task still running
                if (IsExecutingTask_Internal())
                    return;

                // Once this chunk is marked as finished we stop caring about everything else
                if (IsFinished_Internal())
                    return;
            }

            // Go from the least important bit to most important one. If a given bit it set
            // we execute the task tied with it
            
            ProcessNotifyState();
            if (m_pendingTasks.Check(ChunkState.GenericWork) && PerformGenericWork())
                return;

#if ENABLE_BLUEPRINTS
            ProcessNotifyState();
            if (m_pendingTasks.Check(ChunkState.GenerateBlueprints) && GenerateBlueprints())
                return;
#endif

            ProcessNotifyState();
            if (m_pendingTasks.Check(ChunkState.FinalizeData) && FinalizeData())
                return;

            // TODO: Consider making it possible to serialize section and generate vertices at the same time
            ProcessNotifyState();
            if (m_pendingTasks.Check(ChunkState.Serialize) && SerializeChunk())
                return;

            ProcessNotifyState();
            if (m_pendingTasks.Check(ChunkState.Remove) && RemoveChunk())
                return;

            ProcessNotifyState();
            if (m_pendingTasks.Check(ChunkState.Generate) && GenerateData(possiblyVisible))
                return;

            // Building vertices has the lowest priority for us. It's just the data we see.
            if (possiblyVisible)
			{
                ProcessNotifyState();
                if (m_pendingTasks.Check(ChunkState.BuildVertices))
                    GenerateVertices();
			}
		}

        #region Generic work

        private struct SGenericWorkItem
        {
            public readonly Chunk Chunk;
            public readonly Action Action;

            public SGenericWorkItem(Chunk chunk, Action action)
            {
                Chunk = chunk;
                Action = action;
            }
        }

        private static readonly ChunkState CurrStateGenericWork = ChunkState.GenericWork;
        private static readonly ChunkState NextStateGenericWork = ChunkState.Idle;

        private static void OnGenericWork(ref SGenericWorkItem item)
        {
            Chunk chunk = item.Chunk;

            // Perform the action
            item.Action();

            int cnt = Interlocked.Decrement(ref chunk.m_genericWorkItemsLeftToProcess);
            if (cnt<=0)
            {
                // Something is very wrong if we go below zero
                Assert.IsTrue(cnt == 0);

                // All generic work is done
                lock (chunk.m_lock)
                {
                    OnGenericWorkDone(chunk);
                }
            }
        }

        private static void OnGenericWorkDone(Chunk chunk)
        {
            chunk.m_completedTasks = chunk.m_completedTasks.Set(CurrStateGenericWork);
            chunk.m_notifyState = NextStateGenericWork;
            chunk.m_taskRunning = false;
        }

        private bool PerformGenericWork()
        {
            // When we get here we expect all generic tasks to be processed
            Assert.IsTrue(Interlocked.CompareExchange(ref m_genericWorkItemsLeftToProcess, 0, 0) == 0);

            m_pendingTasks = m_pendingTasks.Reset(CurrStateGenericWork);
            
            // Nothing here for us to do if the chunk was not changed
            if (m_completedTasks.Check(CurrStateGenericWork) && !m_refreshTasks.Check(CurrStateGenericWork))
            {
                m_genericWorkItemsLeftToProcess = 0;
                OnGenericWorkDone(this);
                return false;
            }

            m_refreshTasks = m_refreshTasks.Reset(CurrStateGenericWork);
            m_completedTasks = m_completedTasks.Reset(CurrStateGenericWork);
            
            // If there's nothing to do we can skip this state
            if (m_genericWorkItems.Count<=0)
            {
                m_genericWorkItemsLeftToProcess = 0;
                OnGenericWorkDone(this);
                return false;
            }

            m_taskRunning = true;
            m_genericWorkItemsLeftToProcess = m_genericWorkItems.Count;

            for (int i = 0; i<m_genericWorkItems.Count; i++)
            {
                SGenericWorkItem workItem = new SGenericWorkItem(this, m_genericWorkItems[i]);

                WorkPoolManager.Add(
                    new ThreadItem(
                        m_threadID,
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
            Assert.IsTrue(action!=null);
            m_genericWorkItems.Add(action);
            RefreshState(ChunkState.GenericWork);
        }

#endregion

        #region Generate chunk data

        private static readonly ChunkState CurrStateGenerateData = ChunkState.Generate;
#if ENABLE_BLUEPRINTS
		private static readonly ChunkState NextStateGenerateData = ChunkState.GenerateBlueprints;
#else
        private static readonly ChunkState NextStateGenerateData = ChunkState.FinalizeData;
#endif

        private static void OnGenerateData(Chunk chunk)
		{
			chunk.Map.ChunkGenerator.Generate(chunk);

		    lock (chunk.m_lock)
		    {
		        OnGenerateDataDone(chunk);
		    }
		}

		private static void OnGenerateDataDone(Chunk chunk)
		{
			chunk.m_completedTasks = chunk.m_completedTasks.Set(CurrStateGenerateData);
			chunk.m_notifyState = NextStateGenerateData;
			chunk.m_taskRunning = false;
        }

		private bool GenerateData(bool possiblyVisible)
        {
			if (m_completedTasks.Check(CurrStateGenerateData))
			{
                m_pendingTasks = m_pendingTasks.Reset(CurrStateGenerateData);

                OnGenerateDataDone(this);
                return false;
            }

            // In order to save performance only generate data on-demand
            if (!possiblyVisible)
                return true;

            m_pendingTasks = m_pendingTasks.Reset(CurrStateGenerateData);
            m_completedTasks = m_completedTasks.Reset(CurrStateGenerateData);

            m_taskRunning = true;
			WorkPoolManager.Add(new ThreadItem(
                m_threadID,
				arg =>
				{
					Chunk chunk = (Chunk)arg;
					OnGenerateData(chunk);
				},
				this)
			);

            return true;
		}

        #endregion Generate chunk data

#if ENABLE_BLUEPRINTS
        #region Generate blueprints

		private static readonly ChunkState CurrStateGenerateBlueprints = ChunkState.GenerateBlueprints;
		private static readonly ChunkState NextStateGenerateBlueprints = ChunkState.FinalizeData;

		private static void OnGenerateBlueprints(Chunk chunk)
		{
		    lock (chunk.m_lock)
		    {
		        OnGenerateBlueprintsDone(chunk);
		    }
		}

		private static void OnGenerateBlueprintsDone(Chunk chunk)
		{
			chunk.m_completedTasks = chunk.m_completedTasks.Set(CurrStateGenerateBlueprints);
			chunk.m_notifyState = NextStateGenerateBlueprints;
			chunk.m_taskRunning = false;
		}

		private bool GenerateBlueprints()
		{
			Assert.IsTrue(m_completedTasks.Check(ChunkState.Generate),
				string.Format("[{0},{1},{2}] - GenerateBlueprints set sooner than Generate completed. Pending:{3}, Completed:{4}", Pos.X, Pos.Y, Pos.Z, m_pendingTasks, m_completedTasks)
            );
			if (!m_completedTasks.Check(ChunkState.Generate))
				return true;

			m_pendingTasks = m_pendingTasks.Reset(CurrStateGenerateBlueprints);

			if (m_completedTasks.Check(CurrStateGenerateBlueprints))
			{
				OnGenerateBlueprintsDone(this);
				return false;
			}

			m_completedTasks = m_completedTasks.Reset(CurrStateGenerateBlueprints);

            m_taskRunning = true;
			WorkPoolManager.Add(new ThreadItem(
                m_threadID,
				arg =>
				{
					Chunk chunk = (Chunk)arg;
					OnGenerateBlueprints(chunk);
				},
				this)
			);

            return true;
		}

        #endregion Generate blueprints
#endif

        #region Finalize chunk data

        private static readonly ChunkState CurrStateFinalizeData = ChunkState.FinalizeData;
		private static readonly ChunkState NextStateFinalizeData = ChunkState.BuildVertices;

		private static void OnFinalizeData(Chunk chunk)
		{
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

            lock (chunk.m_lock)
		    {
		        OnFinalizeDataDone(chunk);
		    }
		}

		private static void OnFinalizeDataDone(Chunk chunk)
		{
			chunk.m_completedTasks = chunk.m_completedTasks.Set(CurrStateFinalizeData);
			chunk.m_notifyState = NextStateFinalizeData;
			chunk.m_taskRunning = false;
		}

		private bool FinalizeData()
		{
#if ENABLE_BLUEPRINTS
            // All sections must have blueprints generated first
		    Assert.IsTrue(
				m_completedTasks.Check(ChunkState.GenerateBlueprints),
				string.Format("[{0},{1},{2}] - FinalizeData set sooner than GenerateBlueprints completed. Pending:{3}, Completed:{4}", Pos.X, Pos.Y, Pos.Z, m_pendingTasks, m_completedTasks)
            );
		    if (!m_completedTasks.Check(ChunkState.GenerateBlueprints))
                return true;
#else
            Assert.IsTrue(
                m_completedTasks.Check(ChunkState.Generate),
                string.Format("[{0},{1},{2}] - FinalizeData set sooner than Generate completed. Pending:{3}, Completed:{4}", Pos.X, Pos.Y, Pos.Z, m_pendingTasks, m_completedTasks)
            );
            if (!m_completedTasks.Check(ChunkState.Generate))
                return true;
#endif

			m_pendingTasks = m_pendingTasks.Reset(CurrStateFinalizeData);

            // Nothing here for us to do if the chunk was not changed
            if (m_completedTasks.Check(CurrStateFinalizeData) && !m_refreshTasks.Check(CurrStateFinalizeData))
            {
                OnFinalizeDataDone(this);
                return false;
            }

            m_refreshTasks = m_refreshTasks.Reset(CurrStateFinalizeData);
            m_completedTasks = m_completedTasks.Reset(CurrStateFinalizeData);

            m_taskRunning = true;
			WorkPoolManager.Add(new ThreadItem(
                m_threadID,
				arg =>
				{
					Chunk chunk = (Chunk)arg;
					OnFinalizeData(chunk);
				},
				this)
			);

            return true;
		}

        #endregion Finalize chunk data

        #region Serialize chunk

		private struct SSerializeWorkItem
		{
			public readonly Chunk Chunk;
			public readonly string FilePath;

			public SSerializeWorkItem(Chunk chunk, string filePath)
			{
				Chunk = chunk;
				FilePath = filePath;
			}
		}

		private static readonly ChunkState CurrStateSerializeChunk = ChunkState.Serialize;

		private static void OnSerializeChunk(Chunk chunk, string filePath)
		{
			// !TODO: Handle failure
			ChunkProvider.StoreChunkToDisk(
				chunk,
				filePath
			);

		    lock (chunk.m_lock)
		    {
		        OnSerialzeChunkDone(chunk);
		    }
		}

		private static void OnSerialzeChunkDone(Chunk chunk)
		{
			chunk.m_completedTasks = chunk.m_completedTasks.Set(CurrStateSerializeChunk);
			chunk.m_taskRunning = false;
        }

		private bool SerializeChunk()
		{
			// This state should only be set it streaming is enabled
			Assert.IsTrue(EngineSettings.WorldConfig.Streaming);

			// If chunk was generated...
			if (m_completedTasks.Check(ChunkState.Generate))
			{
				// ...  we need to wait until blueprints are generated and chunk is finalized
				if (!m_completedTasks.Check(
#if ENABLE_BLUEPRINTS
                    ChunkState.GenerateBlueprints |
#endif
                    ChunkState.FinalizeData
                    ))
					return true;
			}

			m_pendingTasks = m_pendingTasks.Reset(CurrStateSerializeChunk);

            // Nothing here for us to do if the chunk was not changed since the last serialization
            if (m_completedTasks.Check(CurrStateSerializeChunk) && !m_refreshTasks.Check(CurrStateSerializeChunk))
            {
                OnSerialzeChunkDone(this);
                return false;
            }

            m_refreshTasks = m_refreshTasks.Reset(CurrStateSerializeChunk);
			m_completedTasks = m_completedTasks.Reset(CurrStateSerializeChunk);

		    ChunkProvider provider = (ChunkProvider)Map.ChunkProvider;
			SSerializeWorkItem workItem = new SSerializeWorkItem(
				this,
                provider.GetFilePathFromIndex(Pos.X, Pos.Y, Pos.Z)
			);

            m_taskRunning = true;
			IOPoolManager.Add(new ThreadItem(
				arg =>
				{
					SSerializeWorkItem item = (SSerializeWorkItem)arg;
					OnSerializeChunk(item.Chunk, item.FilePath);
				},
				workItem)
			);

            return true;
		}

        #endregion Serialize chunk

        #region Generate vertices

		private struct SGenerateVerticesWorkItem
		{
			public readonly Chunk Chunk;
		    public readonly int MinX;
		    public readonly int MaxX;
			public readonly int MinY;
			public readonly int MaxY;
            public readonly int MinZ;
            public readonly int MaxZ;
            public readonly int LOD;

			public SGenerateVerticesWorkItem(Chunk chunk, int minX, int maxX, int minY, int maxY, int minZ, int maxZ, int lod)
			{
				Chunk = chunk;
			    MinX = minX;
			    MaxX = maxX;
				MinY = minY;
				MaxY = maxY;
			    MinZ = minZ;
			    MaxZ = maxZ;
			    LOD = lod;
			}
		}

		private static readonly ChunkState CurrStateGenerateVertices = ChunkState.BuildVertices;
		private static readonly ChunkState NextStateGenerateVertices = ChunkState.Idle;
        
		private static void OnGenerateVerices(Chunk chunk, int minX, int maxX, int minY, int maxY, int minZ, int maxZ, int lod)
		{
            Map map = chunk.Map;

            int offsetX = (chunk.Pos.X << EngineSettings.ChunkConfig.LogSize) << map.VoxelLogScaleX;
            int offsetY = (chunk.Pos.Y << EngineSettings.ChunkConfig.LogSize) << map.VoxelLogScaleY;
            int offsetZ = (chunk.Pos.Z << EngineSettings.ChunkConfig.LogSize) << map.VoxelLogScaleZ;

            map.MeshBuilder.BuildMesh(map, chunk.m_drawCallBatcher, offsetX, offsetY, offsetZ, minX, maxX, minY, maxY, minZ, maxZ, lod, chunk.Pools);

		    lock (chunk.m_lock)
		    {
		        OnGenerateVerticesDone(chunk);
		    }
		}

		private static void OnGenerateVerticesDone(Chunk chunk)
		{
			chunk.m_completedTasks = chunk.m_completedTasks.Set(CurrStateGenerateVertices);
			chunk.m_notifyState = NextStateGenerateVertices;
			chunk.m_taskRunning = false;
        }

		/// <summary>
		///     Build this minichunk's render buffers
		/// </summary>
		private void GenerateVertices()
		{
			/*Assert.IsTrue(
				m_completedTasks.Check(ChunkState.FinalizeData),
				string.Format("[{0},{1},{2}] - GenerateVertices set sooner than FinalizeData completed. Pending:{3}, Completed:{4}", Pos.X, Pos.Y, Pos.Z, m_pendingTasks, m_completedTasks)
            );*/
            if (!m_completedTasks.Check(ChunkState.FinalizeData))
                return;

			m_pendingTasks = m_pendingTasks.Reset(CurrStateGenerateVertices);

            // Nothing here for us to do if the chunk was not changed since the last time geometry was built
            if (m_completedTasks.Check(CurrStateGenerateVertices) && !m_refreshTasks.Check(CurrStateGenerateVertices))
            {
                OnGenerateVerticesDone(this);
                return;
            }

            m_refreshTasks = m_refreshTasks.Reset(CurrStateGenerateVertices);
            m_completedTasks = m_completedTasks.Reset(CurrStateGenerateVertices);
            
			if (NonEmptyBlocks>0)
			{
			    IsBuilt = false;
                
				var workItem = new SGenerateVerticesWorkItem(
                    this, MinRenderX, MaxRenderX, MinRenderY, MaxRenderY, MinRenderZ, MaxRenderZ, LOD
                    );
                
                m_taskRunning = true;
				WorkPoolManager.Add(new ThreadItem(
                    m_threadID,
					arg =>
					{
						SGenerateVerticesWorkItem item = (SGenerateVerticesWorkItem)arg;
                        OnGenerateVerices(item.Chunk, item.MinX, item.MaxX, item.MinY, item.MaxY, item.MinZ, item.MaxZ, item.LOD);
                    },
					workItem)
				);
			}
			else
			{
				OnGenerateVerticesDone(this);
			}
		}

        #endregion Generate vertices

        #region Remove chunk

		private static readonly ChunkState CurrStateRemoveChunk = ChunkState.Remove;

		private bool RemoveChunk()
		{
            // Wait until all generic tasks are processed
		    if (Interlocked.CompareExchange(ref m_genericWorkItemsLeftToProcess, 0, 0)!=0)
		    {
                Assert.IsTrue(false);
		        return false;
		    }

		    // If chunk was generated we need to wait for other states with higher priority to finish first
            if (m_completedTasks.Check(ChunkState.Generate))
            {
                // Blueprints and FinalizeData need to finish first
                if (!m_completedTasks.Check(
#if ENABLE_BLUEPRINTS
                    ChunkState.GenerateBlueprints|
#endif
                    ChunkState.FinalizeData
                    ))
                    return false;

                // With streaming enabled we have to wait for serialization to finish as well
                if (EngineSettings.WorldConfig.Streaming && !m_completedTasks.Check(ChunkState.Serialize))
                    return false;
            }
            else
            // No work on chunk started yet. Reset its' state completely
            {
                m_pendingTasks = m_pendingTasks.Reset();
                m_completedTasks = m_completedTasks.Reset();
            }
                        
            m_completedTasks = m_completedTasks.Set(CurrStateRemoveChunk);
		    return true;
		}

        #endregion Remove chunk

#endregion Chunk generation

#region Chunk modification

        private void RefreshState(ChunkState state)
        {
            m_refreshTasks = m_refreshTasks.Set(state);
            m_pendingTasks = m_pendingTasks.Set(state);
        }

		private void QueueSetBlock(Chunk chunk, int bx, int by, int bz, BlockData block)
		{
            // Ignore attempts to change the block into the same one
            if (block.BlockType==Blocks[bx, by, bz].BlockType)
                return;

            int cx = chunk.Pos.X;
		    int cy = chunk.Pos.Y;
			int cz = chunk.Pos.Z;

            int subscribersMask = 0;

			// Iterate over neighbors and decide which ones should be notified to rebuild
			for (int i = 0; i < Subscribers.Length; i++)
			{
				ChunkEvent subscriber = Subscribers[i];
				if (subscriber == null)
					continue;

				Chunk subscriberChunk = (Chunk)subscriber;

                if (subscriberChunk.Pos.X == cx && (
                    // Section to the left
                    ((bx == 0) && (subscriberChunk.Pos.X + 1 == cx)) ||
                    // Section to the right
                    ((bx == EngineSettings.ChunkConfig.Mask) && (subscriberChunk.Pos.X - 1 == cx))
                ))
                    subscribersMask = subscribersMask | (1 << i);

                if (subscriberChunk.Pos.Y == cy && (
                    // Section to the bottom
                    ((by == 0) && (subscriberChunk.Pos.Y + 1 == cy)) ||
                    // Section to the top
                    ((by == EngineSettings.ChunkConfig.Mask) && (subscriberChunk.Pos.Y - 1 == cy))
                ))
                    subscribersMask = subscribersMask | (1 << i);

                if (subscriberChunk.Pos.Z == cz && (
                    // Section to the back
                    ((bz == 0) && (subscriberChunk.Pos.Z + 1 == cz)) ||
                    // Section to the front
                    ((bz == EngineSettings.ChunkConfig.Mask) && (subscriberChunk.Pos.Z - 1 == cz))
                ))
                    subscribersMask = subscribersMask | (1 << i);
            }

            // Request update for the block
            m_setBlockQueue.Add(
                new SetBlockContext(chunk, bx, by, bz, block, subscribersMask)
                );
        }

		public void ProcessSetBlockQueue()
		{
            // Modify blocks
            for (int i = 0; i < m_setBlockQueue.Count; i++)
            {
                SetBlockContext context = m_setBlockQueue[i];
                
                Blocks[context.BX, context.BY, context.BZ] = context.Block;

                // Chunk needs to be finialized again
                /*
                    TODO: This can be optimized. There's no need to recompute min/max chunk index
                    everytime we update a block. An update is only required when we delete/add a block.
                    The best would be having two 2D [chunkWidth,chunkHeight] arrays storing min and
                    max height indexes respectively. This would be helpful for other things as well.
                */
                RefreshState(ChunkState.FinalizeData);

                // Let us know that there was a change in data since the last time the chunk
                // was loaded so we can enqueue serialization only when it is really necessary.
                m_refreshTasks = m_refreshTasks.Set(ChunkState.Serialize);

                // Ask for rebuild of geometry
                RefreshState(ChunkState.BuildVertices);

                // Notify subscribers
                if (context.SubscribersMask > 0)
                {
                    for (int j = 0; j < Subscribers.Length; j++)
                    {
                        Chunk subscriber = (Chunk)Subscribers[j];
                        if (subscriber!=null && ((context.SubscribersMask >> j)&1)!=0)
                            subscriber.RefreshState(ChunkState.BuildVertices);
                    }
                }
            }

            m_setBlockQueue.Clear();
		}

        #endregion Chunk modification

        #endregion Public Methods

        #region IOcclusionEntity

        //! Boundaries of the mini chunk
        public Bounds GeometryBounds { get; set; }

        //! Make the occluder visible/invisible
        public bool Visible
        {
            get { return m_drawCallBatcher.IsVisible(); }
            set { m_drawCallBatcher.SetVisible(value); }
        }

        public bool IsOccluder()
        {
            // For now let's consider all chunks with bounding box as occluders
            // TODO! Make this inteligent - don't include half empty or transparent chunks
            return BBoxVertices.Count > 0;
        }

        #endregion

        #region IRasterizationEntity

        public List<Vector3> BBoxVertices { get; set; }

        public List<Vector3> BBoxVerticesTransformed { get; set; }

        #endregion
    }
}