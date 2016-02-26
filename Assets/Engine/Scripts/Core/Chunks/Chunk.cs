using System;
using Assets.Engine.Scripts.Common.DataTypes;
using Assets.Engine.Scripts.Common.Extensions;
using Assets.Engine.Scripts.Common.Threading;
using Assets.Engine.Scripts.Core.Blocks;
using Assets.Engine.Scripts.Core.Threading;
using Assets.Engine.Scripts.Provider;
using UnityEngine;
using Assert = UnityEngine.Assertions.Assert;
using System.Collections.Generic;

namespace Assets.Engine.Scripts.Core.Chunks
{
    /// <summary>
    ///     Represents a chunk consisting of several even sized mini chunks
    /// </summary>
	public class Chunk: ChunkEvent
    {
        #region Public variables

        public Map Map { get; private set; }
        
        public readonly BlockStorage Blocks;

        public readonly MiniChunk[] Sections;

        //! Bounding box in world coordinates
        public Bounds WorldBounds { get; private set; }

        // Chunk coordinates
        public Vector2Int Pos { get; private set; }

        //! Chunk bound in terms of geometry
        public int MaxRenderY { get; set; }
        public int MinRenderY { get; set; }
        public int MaxRenderX { get; private set; }
        public int MinRenderX { get; private set; }
        public int MinRenderZ { get; private set; }
        public int MaxRenderZ { get; private set; }

        //! Range in which sections are to be iterated
        public int MinFilledSection { get; private set; }
        public int MaxFilledSection { get; private set; }

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

                    // Request update for each chunk section
                    for (int i = 0; i < EngineSettings.ChunkConfig.StackSize; i++)
                        m_setBlockSections = m_setBlockSections | (1 << i);
                    RefreshState(ChunkState.BuildVertices);
                }
            }
        }

        public bool PossiblyVisible { get; private set; }

        public bool RequestedRemoval;

#if     DEBUG
        public bool IsUsed = false;
#endif

#endregion Public variables

        #region Private variables

        //! A list of event requiring counter
        private readonly int [] m_eventCnt = {0, 0};

        //! Sections queued for building
        // TODO: StackSize can be at most 31 with this approach (32 bits). Implement a generic solution for arbitrary StackSize values
        private int m_setBlockSections;
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

        #endregion Private variables

        #region Constructors

        public Chunk():
			base(4)
        {
            Blocks = new BlockStorage();

            Sections = new MiniChunk[EngineSettings.ChunkConfig.StackSize];
            for (int i = 0; i<Sections.Length; i++)
                Sections[i] = new MiniChunk(this, i);
            
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
        
        public void Init(Map map, int cx, int cz, int lod)
        {
            Map = map;
            Pos = new Vector2Int(cx, cz);
            m_lod = lod;

            WorldBounds = new Bounds(
                new Vector3(EngineSettings.ChunkConfig.Size*(cx+0.5f), EngineSettings.ChunkConfig.SizeYTotal*0.5f, EngineSettings.ChunkConfig.Size*(cz+0.5f)),
                new Vector3(EngineSettings.ChunkConfig.Size, EngineSettings.ChunkConfig.SizeYTotal, EngineSettings.ChunkConfig.Size)
                );

            foreach (MiniChunk section in Sections)
            {
                section.WorldBounds = new Bounds(
                    new Vector3(EngineSettings.ChunkConfig.Size*(cx+0.5f), section.OffsetY + EngineSettings.ChunkConfig.Size*0.5f, EngineSettings.ChunkConfig.Size*(cz+0.5f)),
                    new Vector3(EngineSettings.ChunkConfig.Size, EngineSettings.ChunkConfig.Size, EngineSettings.ChunkConfig.Size)
                    );
            }
        }

		public void RegisterNeighbors()
		{
			Chunk left = Map.GetChunk(Pos.X - 1, Pos.Z);
			Chunk right = Map.GetChunk(Pos.X + 1, Pos.Z);
			Chunk front = Map.GetChunk(Pos.X, Pos.Z - 1);
			Chunk behind = Map.GetChunk(Pos.X, Pos.Z + 1);

			// Left
			RegisterNeighbor(left);
			// Right
			RegisterNeighbor(right);
			// Front
			RegisterNeighbor(front);
			// Behind
			RegisterNeighbor(behind);
		}

        #region Public Methods
        
        public void UpdateChunk()
        {
			ProcessSetBlockQueue();
			ProcessPendingTasks(PossiblyVisible);

            if (m_completedTasks.Check(ChunkState.BuildVertices))
            {
                foreach (MiniChunk section in Sections)
                    section.Build();
            }
        }

        public void Reset()
        {
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

			for(int i=0; i<EngineSettings.ChunkConfig.StackSize; i++)
				m_setBlockSections = m_setBlockSections | (1<<i);
            m_setBlockQueue.Clear();

            // Reset blocks
            Blocks.Reset();
            
            PossiblyVisible = false;

            MinRenderY = EngineSettings.ChunkConfig.MaskYTotal;
            MaxRenderY = 0;
            MinRenderX = EngineSettings.ChunkConfig.Mask;
            MaxRenderX = 0;
            MinRenderZ = EngineSettings.ChunkConfig.Mask;
            MaxRenderX = 0;

            MinFilledSection = 0;
            MaxFilledSection = EngineSettings.ChunkConfig.StackSize-1;
            
            // Reset sections
            foreach (MiniChunk section in Sections)
                section.Reset();

            ResetEvent();
        }

		/// <summary>
		///     Changes chunk's visibility
		/// </summary>
        public void SetVisible(bool show)
        {
            foreach (MiniChunk section in Sections)
                section.Visible = show;
        }

        public void SetPossiblyVisible(bool show)
        {
            PossiblyVisible = show;
        }

		/// <summary>
		///     Damages a given block. Destroys the block if the damage reaches a certain threshold
		/// </summary>
		/* TODO!
		* This needs to be done differently. Each block type will be configurable and
		* depending on the configration different kinds of interaction / damage will
		* be possible.
		*/
		public void DamageBlock(int x, int y, int z, int damage)
		{			
			if (damage == 0)
				return;

			var thisBlock = this[x, y, z];

			int blockDmgLevel = thisBlock.GetDamage();
			if (blockDmgLevel + damage >= 15)
			{
				QueueSetBlock(this, x, y, z, BlockData.Air);
				return;
			}

			thisBlock.SetDamage((byte)(blockDmgLevel+damage));
			QueueSetBlock(this, x, y, z, thisBlock);
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
            foreach (MiniChunk section in Sections)
            {
                if (GeometryUtility.TestPlanesAABB(frustum, section.WorldBounds))
                    return true;
            }
            
            return false;
        }
        
        public void GenerateBlock(int x, int y, int z, BlockData data)
        {
            Blocks[x, y, z] = data;
            
            bool isEmpty = data.IsEmpty();
            if (!isEmpty)
            {
                int sectionIndex = y >> EngineSettings.ChunkConfig.LogSize;
                MiniChunk section = Sections[sectionIndex];
                ++section.NonEmptyBlocks;

                if (x < MinRenderX)
                    MinRenderX = x;
                if (z < MinRenderZ)
                    MinRenderZ = z;

                if (x > MaxRenderX)
                    MaxRenderX = x;
                if (z > MaxRenderZ)
                    MaxRenderZ = z;

                if (y > MaxRenderY)
                    MaxRenderY = y;
            }
            else if (y < MinRenderY)
                MinRenderY = y;
        }

        // Calculate lowest empty and highest solid block position
		/* TODO: Lowest/highest block can be computed while the terrain is generated. This
		 * would speed things up for initial chunk generation.
		*/
        public void CalculateProperties()
        {
            int nonEmptyBlocks = 0;

            if (m_firstFinalization)
            {
                foreach (MiniChunk section in Sections)
                    nonEmptyBlocks += section.NonEmptyBlocks;

                m_firstFinalization = false;
            }
            else
            {
                MinRenderY = EngineSettings.ChunkConfig.MaskYTotal;
                MaxRenderY = 0;
                MinRenderX = EngineSettings.ChunkConfig.Mask;
                MaxRenderX = 0;
                MinRenderZ = EngineSettings.ChunkConfig.Mask;
                MaxRenderX = 0;

                for (int y = EngineSettings.ChunkConfig.MaskYTotal; y>=0; y--)
                {
                    int sectionIndex = y>>EngineSettings.ChunkConfig.LogSize;
                    MiniChunk section = Sections[sectionIndex];

                    for (int z = 0; z<EngineSettings.ChunkConfig.Size; z++)
                    {
                        for (int x = 0; x<EngineSettings.ChunkConfig.Size; x++)
                        {
                            bool isEmpty = Blocks[x, y, z].IsEmpty();
                            if (!isEmpty)
                            {
                                ++nonEmptyBlocks;
                                ++section.NonEmptyBlocks;

                                if (x<MinRenderX)
                                    MinRenderX = x;
                                if (z<MinRenderZ)
                                    MinRenderZ = z;

                                if (x>MaxRenderX)
                                    MaxRenderX = x;
                                if (z>MaxRenderZ)
                                    MaxRenderZ = z;

                                if (y>MaxRenderY)
                                    MaxRenderY = y;
                            }
                            else if (y<MinRenderY)
                                MinRenderY = y;
                        }
                    }
                }
            }

            MinRenderY = Math.Max(MinRenderY-1, 0);
            MaxRenderY = Math.Min(MaxRenderY+1, EngineSettings.ChunkConfig.MaskYTotal);

            MinFilledSection = MinRenderY>>EngineSettings.ChunkConfig.LogSize;
            MaxFilledSection = MaxRenderY>>EngineSettings.ChunkConfig.LogSize;

            if (nonEmptyBlocks > 0)
            {
                int posInWorldX = Pos.X<<EngineSettings.ChunkConfig.LogSize;
                int posInWorldZ = Pos.Z<<EngineSettings.ChunkConfig.LogSize;

                // Build bounding mesh for each section
                float width = (MaxRenderX - MinRenderX) + 1;
                float depth = (MaxRenderZ - MinRenderZ) + 1;
                int startY = MinRenderY;
                for (int i = 0; i < Sections.Length; i++)
                {
                    MiniChunk section = Sections[i];
                    section.ResetBoundingMesh();

                    if (startY>=MaxRenderY)
                        continue;

                    int sectionMaxY = section.OffsetY + EngineSettings.ChunkConfig.Mask;
                    if (startY>=sectionMaxY)
                        continue;

                    int heightMax = startY+EngineSettings.ChunkConfig.Size;
                    heightMax = Mathf.Min(heightMax, sectionMaxY);
                    heightMax = Mathf.Min(heightMax, MaxRenderY);

                    float height = heightMax - startY;

                    Bounds bounds = new Bounds(
                        new Vector3(posInWorldX + width*0.5f, startY + height*0.5f, posInWorldZ + depth*0.5f),
                        new Vector3(width, height, depth)
                        );
                    section.BuildBoundingMesh(ref bounds);

                    startY = sectionMaxY;
                }
            }
            else
            {
                for (int i = 0; i<Sections.Length; i++)
                {
                    MiniChunk section = Sections[i];
                    section.ResetBoundingMesh();
                }
            }
        }

        public void Restore()
        {
			if (!RequestedRemoval)
				return;

			RequestedRemoval = false;

			if (EngineSettings.WorldConfig.Streaming)
				m_pendingTasks = m_pendingTasks.Reset(ChunkState.Serialize);
			m_pendingTasks = m_pendingTasks.Reset(ChunkState.Remove);
        }

        public bool Finish()
        {
            // Try to process what's left in case there's still something to do
            ProcessSetBlockQueue();
			ProcessPendingTasks(false);

			if (!RequestedRemoval)
			{
				RequestedRemoval = true;

				if(EngineSettings.WorldConfig.Streaming)
					OnNotified(ChunkState.Serialize);
				OnNotified(ChunkState.Remove);
			}

			// Wait until all work is finished on a given chunk
			bool isWorking = IsExecutingTask() || !IsFinished();
			return !isWorking;
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
		    int eventIndex = 0;
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

		    if (eventIndex>0)
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

		public bool IsFinished()
		{
		    lock (m_lock)
		    {
		        return m_completedTasks.Check(ChunkState.Remove);
		    }
		}

        public bool IsFinalized()
        {
            lock (m_lock)
            {
                return m_completedTasks.Check(ChunkState.FinalizeData);
            }
        }

        public bool IsExecutingTask()
		{
		    lock (m_lock)
		    {
		        return m_taskRunning;
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
            // We are not allowed to check anything as long as there is a task still running
            if (IsExecutingTask())
                return;

            // Once this chunk is marked as finished we stop caring about everything else
            if (IsFinished())
                return;

            // Go from the least important bit to most important one. If a given bit it set
            // we execute the task tied with it
            ProcessNotifyState();
            if (m_pendingTasks.Check(ChunkState.Generate) && GenerateData(possiblyVisible))
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
            if (m_pendingTasks.Check(ChunkState.Remove))
			{
				RemoveChunk();
			}
			// Building vertices has the lowest priority for us. It's just the data we see.
			else if (possiblyVisible && m_pendingTasks.Check(ChunkState.BuildVertices))
			{				
				GenerateVertices();
			}
		}

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
				string.Format("[{0},{1}] - GenerateBlueprints set sooner than Generate completed. Pending:{2}, Completed:{3}", Pos.X, Pos.Z, m_pendingTasks, m_completedTasks)
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
				string.Format("[{0},{1}] - FinalizeData set sooner than GenerateBlueprints completed. Pending:{2}, Completed:{3}", Pos.X, Pos.Z, m_pendingTasks, m_completedTasks)
            );
		    if (!m_completedTasks.Check(ChunkState.GenerateBlueprints))
                return true;
#else
            Assert.IsTrue(
                m_completedTasks.Check(ChunkState.Generate),
                string.Format("[{0},{1}] - FinalizeData set sooner than Generate completed. Pending:{2}, Completed:{3}", Pos.X, Pos.Z, m_pendingTasks, m_completedTasks)
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

			SSerializeWorkItem workItem = new SSerializeWorkItem(
				this,
				ChunkProvider.GetFilePathFromIndex(Pos.X, Pos.Z)
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
			public readonly int SetBlockSections;
		    public readonly int MinX;
		    public readonly int MaxX;
			public readonly int MinY;
			public readonly int MaxY;
            public readonly int MinZ;
            public readonly int MaxZ;
            public readonly int LOD;

			public SGenerateVerticesWorkItem(Chunk chunk, int setBlockSections, int minX, int maxX, int minY, int maxY, int minZ, int maxZ, int lod)
			{
				Chunk = chunk;
				SetBlockSections = setBlockSections;
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
        
		private static void OnGenerateVerices(Chunk chunk, int setBlockSections, int minX, int maxX, int minY, int maxY, int minZ, int maxZ, int lod)
		{
			int minSection = minY >> EngineSettings.ChunkConfig.LogSize;
			int maxSection = maxY >> EngineSettings.ChunkConfig.LogSize;

		    for (int sectionIndex=minSection; sectionIndex<=maxSection; sectionIndex++)
		    {
		        // Only rebuild sections which requested it
		        // E.g. 00100110b means that sections 1,2 and 5 need to be rebuilt
		        int sectionIndexBit = 1<<sectionIndex;
		        if ((setBlockSections&sectionIndexBit)==0)
		            continue;
		        setBlockSections = setBlockSections&(~sectionIndexBit);

                Map map = chunk.Map;
                MiniChunk section = chunk.Sections[sectionIndex];

                int offsetX = chunk.Pos.X * EngineSettings.ChunkConfig.Size;
                int offsetZ = chunk.Pos.Z * EngineSettings.ChunkConfig.Size;

		        int minSectionY = Mathf.Max(minY, section.OffsetY) - section.OffsetY;
		        int maxSectionY = Mathf.Min(maxY, section.OffsetY+EngineSettings.ChunkConfig.Size) - section.OffsetY;

		        minSectionY = Mathf.Max(0, minSectionY);
		        maxSectionY = Mathf.Min(maxSectionY, EngineSettings.ChunkConfig.Mask);
                
                map.MeshBuilder.BuildMesh(map, section.SolidRenderBuffer, offsetX, section.OffsetY, offsetZ, minX, maxX, minSectionY, maxSectionY, minZ, maxZ, lod);
		    }

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
				string.Format("[{0},{1}] - GenerateVertices set sooner than FinalizeData completed. Pending:{2}, Completed:{3}", Pos.X, Pos.Z, m_pendingTasks, m_completedTasks)
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

			int nonEmptyBlocks = 0;
            foreach (MiniChunk section in Sections)
            {
                nonEmptyBlocks = nonEmptyBlocks + section.NonEmptyBlocks;
                if (section.NonEmptyBlocks > 0)
                    section.IsBuilt = false;
            }
			
			if (nonEmptyBlocks>0)
			{
				int lowest = Mathf.Max(MinRenderY, 0);
				int highest = Mathf.Min(MaxRenderY, EngineSettings.ChunkConfig.SizeYTotal-1);
				var workItem = new SGenerateVerticesWorkItem(
                    this, m_setBlockSections, MinRenderX, MaxRenderX, lowest, highest, MinRenderZ, MaxRenderZ, LOD
                    );
                
				m_setBlockSections = 0;

                m_taskRunning = true;
				WorkPoolManager.Add(new ThreadItem(
					arg =>
					{
						SGenerateVerticesWorkItem item = (SGenerateVerticesWorkItem)arg;
                        OnGenerateVerices(item.Chunk, item.SetBlockSections, item.MinX, item.MaxX, item.MinY, item.MaxY, item.MinZ, item.MaxZ, item.LOD);
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

		private void RemoveChunk()
		{
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
                    return;

                // With streaming enabled we have to wait for serialization to finish as well
                if (EngineSettings.WorldConfig.Streaming && !m_completedTasks.Check(ChunkState.Serialize))
                    return;
            }
            else
            // No work on chunk started yet. Reset its' state completely
            {
                m_pendingTasks = m_pendingTasks.Reset();
                m_completedTasks = m_completedTasks.Reset();
            }
                        
            m_completedTasks = m_completedTasks.Set(CurrStateRemoveChunk);
        }

#endregion Remove chunk

#endregion Chunk generation

#region Chunk modification

		private void QueueSection(int sectionIndex)
		{
            Assert.IsTrue(sectionIndex >= 0 && sectionIndex < EngineSettings.ChunkConfig.StackSize,
                string.Format("QueueSection called with wrong section index {0} on chunk [{1},{2}]", sectionIndex, Pos.X, Pos.Z)
                );
			
			m_setBlockSections = m_setBlockSections | (1 << sectionIndex);

            // We want this chunk rebuilt
            RefreshState(ChunkState.BuildVertices);
        }

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
			int cz = chunk.Pos.Z;
			int sectionIndex = by >> EngineSettings.ChunkConfig.LogSize;

            int subscribersMask = 0;
            int sectionsMask = 0;

			// Iterate over neighbors and decide which ones should be notified to rebuild
			for (int i = 0; i < Subscribers.Length; i++)
			{
				ChunkEvent subscriber = Subscribers[i];
				if (subscriber == null)
					continue;

				Chunk subscriberChunk = (Chunk)subscriber;

                if (subscriberChunk.Pos.Z == cz && (
                    // Section to the left
                    ((bx == 0) && (subscriberChunk.Pos.X + 1 == cx)) ||
                    // Section to the right
                    ((bx == EngineSettings.ChunkConfig.Mask) && (subscriberChunk.Pos.X - 1 == cx))
                ))
                    subscribersMask = subscribersMask | (1 << i);

				if (subscriberChunk.Pos.X == cx && (
					// Section to the front
					((bz == EngineSettings.ChunkConfig.Mask) && (subscriberChunk.Pos.Z - 1 == cz)) ||
					// Section to the back
					((bz == 0) && (subscriberChunk.Pos.Z + 1 == cz))
				))
                    subscribersMask = subscribersMask | (1 << i);
            }
            
            int diff = by - sectionIndex * EngineSettings.ChunkConfig.Size;
            // Section to the bottom
            if (diff == 0)
            {
                int index = Math.Max(sectionIndex - 1, 0);
                sectionsMask = sectionsMask | (1 << index);
            }
            // Section to the top
            else if (diff == EngineSettings.ChunkConfig.Mask)
            {
                int index = Math.Min(sectionIndex + 1, EngineSettings.ChunkConfig.StackSize - 1);
                sectionsMask = sectionsMask | (1 << index);
            }
            // This section
            sectionsMask = sectionsMask | (1 << sectionIndex);

            // Request update for the block
            m_setBlockQueue.Add(
                new SetBlockContext(chunk, bx, by, bz, block, subscribersMask, sectionsMask)
                );
        }

		public void ProcessSetBlockQueue()
		{
            // Modify blocks
            for (int i = 0; i < m_setBlockQueue.Count; i++)
            {
                SetBlockContext context = m_setBlockQueue[i];
                
                Blocks[context.BX, context.BY, context.BZ] = context.Block;
                                
                int section = context.BY >> EngineSettings.ChunkConfig.LogSize;

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
                for (int s = 0; s < Sections.Length; s++)
                {
                    if (((context.SectionsMask >> s) & 1) != 0)
                        QueueSection(s);
                }

                // Notify subscribers
                if (context.SubscribersMask > 0)
                {
                    for (int j = 0; j < Subscribers.Length; j++)
                    {
                        Chunk subscriber = (Chunk)Subscribers[j];
                        if (subscriber!=null && ((context.SubscribersMask >> j)&1)!=0)
                            subscriber.QueueSection(section);
                    }
                }
            }

            m_setBlockQueue.Clear();
		}

#endregion Chunk modification

#endregion Public Methods
    }
}