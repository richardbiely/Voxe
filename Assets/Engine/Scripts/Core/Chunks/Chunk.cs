using System;
using Assets.Engine.Scripts.Builders;
using Assets.Engine.Scripts.Common.DataTypes;
using Assets.Engine.Scripts.Common.Extensions;
using Assets.Engine.Scripts.Common.IO.RLE;
using Assets.Engine.Scripts.Common.Threading;
using Assets.Engine.Scripts.Core.Blocks;
using Assets.Engine.Scripts.Core.Threading;
using Assets.Engine.Scripts.Physics;
using Assets.Engine.Scripts.Provider;
using Assets.Engine.Scripts.Utils;
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
        
        public readonly RLE<BlockData> RLE;

        public readonly IBlockStorage Blocks;

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

        public bool PossiblyVisible { get; private set; }

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

		//! Specifies whether there's a task running
		private bool m_taskRunning;
		private readonly object m_lock = new object();

		public bool RequestedRemoval;

        #endregion Private variables

        #region Constructors
        
        public Chunk():
			base(4)
        {
            RLE = new RLE<BlockData>();
            Blocks = new BlockStorage();

            Sections = new MiniChunk[EngineSettings.ChunkConfig.StackSize];
            for (int i = 0; i<Sections.Length; i++)
                Sections[i] = new MiniChunk(this, i);
            
            Reset(false);
        }

        #endregion Constructors

        #region Accessors

		/// <summary>
		///     Access a block using a 1D coordinate
		/// </summary>
		public BlockData this[int index]
		{
			get
			{
				return Blocks[index];
			}
			set
			{
				Blocks[index] = value;
			}
		}

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


        public void Init(int cx, int cz)
        {
            Pos = new Vector2Int(cx, cz);

            WorldBounds = new Bounds(
                new Vector3(EngineSettings.ChunkConfig.SizeX*(cx+0.5f), EngineSettings.ChunkConfig.SizeYTotal*0.5f, EngineSettings.ChunkConfig.SizeZ*(cz+0.5f)),
                new Vector3(EngineSettings.ChunkConfig.SizeX, EngineSettings.ChunkConfig.SizeYTotal, EngineSettings.ChunkConfig.SizeZ)
                );

            foreach (MiniChunk section in Sections)
            {
                section.WorldBounds = new Bounds(
                    new Vector3(EngineSettings.ChunkConfig.SizeX*(cx+0.5f), section.OffsetY + EngineSettings.ChunkConfig.SizeY*0.5f, EngineSettings.ChunkConfig.SizeZ*(cz+0.5f)),
                    new Vector3(EngineSettings.ChunkConfig.SizeX, EngineSettings.ChunkConfig.SizeY, EngineSettings.ChunkConfig.SizeZ)
                    );
            }
        }

		public void RegisterNeighbors()
		{
			Chunk left = Map.Current.GetChunk(Pos.X - 1, Pos.Z);
			Chunk right = Map.Current.GetChunk(Pos.X + 1, Pos.Z);
			Chunk front = Map.Current.GetChunk(Pos.X, Pos.Z - 1);
			Chunk behind = Map.Current.GetChunk(Pos.X, Pos.Z + 1);

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

        public void Reset(bool canBeLoaded)
        {
            UnregisterFromNeighbors();
            
            // Reset chunk events
            for (int i = 0; i<m_eventCnt.Length; i++)
				m_eventCnt[i] = 0;

			RequestedRemoval = false;
			m_taskRunning = false;
            
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
            
            // Reset sections
            foreach (MiniChunk section in Sections)
                section.Reset();

			base.Reset();
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

        /// <summary>
        ///     Tells whether any block in a chunk intersects a given AABB
        /// </summary>
        public bool Intersects(Bounds aabb)
        {
            return WorldBounds.Intersects(aabb) && CheckBlocksAABB(aabb);
        }

        // Compares chunk's blocks against a given AABB
        public bool CheckBlocksAABB(Bounds bounds)
        {
            Vector3Int pom = new Vector3Int(Pos.X*EngineSettings.ChunkConfig.SizeX, 0, Pos.Z*EngineSettings.ChunkConfig.SizeZ);

            int minX = Mathf.Clamp(Mathf.FloorToInt(bounds.min.x) - pom.X - 5, 0, EngineSettings.ChunkConfig.MaskX);
            int minY = Mathf.Clamp(Mathf.FloorToInt(bounds.min.y) - 5, 0, EngineSettings.ChunkConfig.SizeYTotal - 1);
            int minZ = Mathf.Clamp(Mathf.FloorToInt(bounds.min.z) - pom.Z - 5, 0, EngineSettings.ChunkConfig.MaskZ);
            Vector3Int bMin = new Vector3Int(minX, minY, minZ);

            int maxX = Mathf.Clamp((int)(bounds.max.x) - pom.X + 5, 0, EngineSettings.ChunkConfig.MaskX);
            int maxY = Mathf.Clamp((int)(bounds.max.y) + 5, 0, EngineSettings.ChunkConfig.SizeYTotal - 1);
            int maxZ = Mathf.Clamp((int)(bounds.max.z) - pom.Z + 5, 0, EngineSettings.ChunkConfig.MaskZ);
            Vector3Int bMax = new Vector3Int(maxX, maxY, maxZ);

            for (int y = bMin.Y; y<=bMax.Y; y++)
            {
                for (int z = bMin.Z; z<=bMax.Z; z++)
                {
                    for (int x = bMin.X; x<=bMax.X; x++)
                    {
                        if (CheckBlockAABB(new Vector3Int(x, y, z), bounds))
                            return true;
                    }
                }
            }

            return false;
        }

        // test a specific block against the given AABB
        public bool CheckBlockAABB(Vector3Int blockPos, Bounds bounds)
        {
            BlockInfo info = BlockDatabase.GetBlockInfo(this[blockPos.X, blockPos.Y, blockPos.Z].BlockType);
            if (info.Physics!=PhysicsType.Solid && info.Physics!=PhysicsType.Fence)
                return false;

            Bounds blockBounds = new Bounds(Vector3.zero, Vector3.one);
            blockBounds.center = new Vector3(
                blockBounds.center.x+blockPos.X+(Pos.X*EngineSettings.ChunkConfig.SizeX),
                blockBounds.center.y+blockPos.Y,
                blockBounds.center.z+blockPos.Z+(Pos.Z*EngineSettings.ChunkConfig.SizeZ)
                );

            // fence block - bounds are twice as high
            if (info.Physics==PhysicsType.Fence)
                blockBounds.max = new Vector3(blockBounds.max.x, blockBounds.max.y+Vector3.up.y, blockBounds.max.z);

            return blockBounds.Intersects(bounds);
        }

        // Calculate lowest empty and highest solid block position
		/* TODO: Lowest/highest block can be computed while the terrain is generated. This
		 * would speed things up for initial chunk generation.
		*/
        public void CalculateProperties()
        {
            int nonEmptyBlocks = 0;

            MinRenderY = EngineSettings.ChunkConfig.MaskYTotal;
			MaxRenderY = 0;
            MinRenderX = EngineSettings.ChunkConfig.MaskX;
            MaxRenderX = 0;
            MinRenderZ = EngineSettings.ChunkConfig.MaskZ;
            MaxRenderX = 0;

            int minY = EngineSettings.ChunkConfig.MaskYTotal, maxY = 0;

            for (int y = EngineSettings.ChunkConfig.MaskYTotal; y>=0; y--)
            {
                int sectionIndex = y>>EngineSettings.ChunkConfig.LogSizeY;
				MiniChunk section = Sections[sectionIndex];

                for (int z = 0; z<EngineSettings.ChunkConfig.SizeZ; z++)
                {
                    for (int x = 0; x<EngineSettings.ChunkConfig.SizeX; x++)
                    {
                        bool isEmpty = Blocks[x,y,z].IsEmpty();
                        if (!isEmpty)
                        {
                            ++nonEmptyBlocks;
							++section.NonEmptyBlocks;

                            if (x<MinRenderX) MinRenderX = x;
                            if (y<minY) minY = y;
                            if (z< MinRenderZ) MinRenderZ = z;

                            if (x> MaxRenderX) MaxRenderX = x;
                            if (y>maxY) maxY = y;
                            if (z> MaxRenderZ) MaxRenderZ = z;

                            if (y > MaxRenderY)
                                MaxRenderY = y;
                        }
                        else if (MinRenderY > y)
                            MinRenderY = y;
                    }
                }
            }

            MinRenderY = Math.Max(MinRenderY-1, 0);
            MaxRenderY = Math.Min(MaxRenderY+1, EngineSettings.ChunkConfig.MaskYTotal);

            if (nonEmptyBlocks > 0)
            {
                int posInWorldX = Pos.X<<EngineSettings.ChunkConfig.LogSizeX;
                int posInWorldZ = Pos.Z<<EngineSettings.ChunkConfig.LogSizeZ;

                // Build bounding mesh for each section
                float width = (MaxRenderX - MinRenderX) + 1;
                float depth = (MaxRenderX - MinRenderZ) + 1;
                int startY = minY;
                for (int i = 0; i < Sections.Length; i++)
                {
                    MiniChunk section = Sections[i];
                    section.ResetBoundingMesh();

                    if (startY>=maxY)
                        continue;

                    int sectionMaxY = section.OffsetY + EngineSettings.ChunkConfig.MaskY;
                    if (startY >= sectionMaxY)
                        continue;

                    int heightMax = startY+EngineSettings.ChunkConfig.SizeY;
                    heightMax = Mathf.Min(heightMax, sectionMaxY);
                    heightMax = Mathf.Min(heightMax, maxY);

                    float height = heightMax - startY;

                    Bounds bounds = new Bounds(
                        new Vector3(posInWorldX + width*0.5f, startY + height*0.5f + 1, posInWorldZ + depth*0.5f),
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
			Map.Current.ChunkProvider.GetGenerator().Generate(chunk);

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
			// Only do this when streaming is enabled for now
			if (EngineSettings.WorldConfig.Streaming)
			{
				chunk.RLE.Reset();
				chunk.RLE.Compress(chunk.Blocks.ToArray());
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

            // NOTE: No need to check whether FinalizeData has already been called here.
            // In fact, we expect that this might happen e.g. when modifying blocks in chunk.
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
			LocalChunkProvider.StoreChunkToDisk(
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

			// NOTE: No need to check whether SerializeChunk has already been called here.
			// In fact, we expect that this might happen e.g. when modifying blocks in chunk.
			m_completedTasks = m_completedTasks.Reset(CurrStateSerializeChunk);

			SSerializeWorkItem workItem = new SSerializeWorkItem(
				this,
				LocalChunkProvider.GetFilePathFromIndex(Pos.X, Pos.Z)
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
			public readonly int MinY;
			public readonly int MaxY;

			public SGenerateVerticesWorkItem(Chunk chunk, int setBlockSections, int minY, int maxY)
			{
				Chunk = chunk;
				SetBlockSections = setBlockSections;
				MinY = minY;
				MaxY = maxY;
			}
		}

		private static readonly ChunkState CurrStateGenerateVertices = ChunkState.BuildVertices;
		private static readonly ChunkState NextStateGenerateVertices = ChunkState.Idle;

		private static void OnGenerateVerices(Chunk chunk, int setBlockSections, int minY, int maxY)
		{
			int minSection = minY >> EngineSettings.ChunkConfig.LogSizeY;
			int maxSection = maxY >> EngineSettings.ChunkConfig.LogSizeY;

			for (int sectionIndex=minSection; sectionIndex <= maxSection; sectionIndex++)
			{
				// Only rebuild sections which requested it
				// E.g. 00100110b means that sections 1,2 and 5 need to be rebuild
				int sectionIndexBit = 1<<sectionIndex;
				if((setBlockSections & sectionIndexBit)==0)
					continue;
				setBlockSections = setBlockSections & (~sectionIndexBit);

				MiniChunk section = chunk.Sections[sectionIndex];
				section.SolidRenderBuffer.Clear();

				int offsetX = chunk.Pos.X*EngineSettings.ChunkConfig.SizeX + chunk.MinRenderX;
				int offsetZ = chunk.Pos.Z*EngineSettings.ChunkConfig.SizeZ + chunk.MinRenderZ;

				int sectionMinY = Mathf.Max(minY, sectionIndex*EngineSettings.ChunkConfig.SizeY);
				int sectionMaxY = Mathf.Min(maxY, sectionIndex*EngineSettings.ChunkConfig.SizeY + EngineSettings.ChunkConfig.MaskY);
                
				for (int wy=sectionMinY; wy<=sectionMaxY; wy++)
				{
					for (int z=chunk.MinRenderZ, wz=offsetZ; z<=chunk.MaxRenderZ; z++, wz++)
					{
						for (int x=chunk.MinRenderX, wx=offsetX; x<=chunk.MaxRenderX; x++, wx++)
						{
							BlockData block = chunk.Blocks[x, wy, z];
							if (block.IsEmpty())
								continue;

							IBlockBuilder builder = BlockDatabase.GetBlockBuilder(block.BlockType);
							if (builder==null)
							{
								Assert.IsTrue(false, string.Format("No builder exists for blockType={0}", block.BlockType));
								continue;
							}

							Vector3Int worldPos = new Vector3Int(wx, wy, wz);
                            Vector3Int localOffset = new Vector3Int(x, wy, z);
							builder.Build(Map.Current, section.SolidRenderBuffer, ref block, ref worldPos, ref localOffset);
						}
					}
				}
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
			Assert.IsTrue(
				m_completedTasks.Check(ChunkState.FinalizeData),
				string.Format("[{0},{1}] - GenerateVertices set sooner than FinalizeData completed. Pending:{2}, Completed:{3}", Pos.X, Pos.Z, m_pendingTasks, m_completedTasks)
            );
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

            // NOTE: No need to check whether GenerateVertices has already been called here.
            // In fact, we expect that this might happen e.g. when modifying blocks in chunk.
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
				var workItem = new SGenerateVerticesWorkItem(this, m_setBlockSections, lowest, highest);
                
				m_setBlockSections = 0;

                m_taskRunning = true;
				WorkPoolManager.Add(new ThreadItem(
					arg =>
					{
						SGenerateVerticesWorkItem item = (SGenerateVerticesWorkItem)arg;
						OnGenerateVerices(item.Chunk, item.SetBlockSections, item.MinY, item.MaxY);
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
            int blockIndex = Common.Helpers.GetIndex1DFrom3D(bx, by, bz);
            if (block.BlockType==Blocks[blockIndex].BlockType)
                return;

            int cx = chunk.Pos.X;
			int cz = chunk.Pos.Z;
			int sectionIndex = by >> EngineSettings.ChunkConfig.LogSizeY;

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
                    ((bx == EngineSettings.ChunkConfig.MaskX) && (subscriberChunk.Pos.X - 1 == cx))
                ))
                    subscribersMask = subscribersMask | (1 << i);

				if (subscriberChunk.Pos.X == cx && (
					// Section to the front
					((bz == EngineSettings.ChunkConfig.MaskZ) && (subscriberChunk.Pos.Z - 1 == cz)) ||
					// Section to the back
					((bz == 0) && (subscriberChunk.Pos.Z + 1 == cz))
				))
                    subscribersMask = subscribersMask | (1 << i);
            }
            
            int diff = by - sectionIndex * EngineSettings.ChunkConfig.SizeY;
            // Section to the bottom
            if (diff == 0)
            {
                int index = Math.Max(sectionIndex - 1, 0);
                sectionsMask = sectionsMask | (1 << index);
            }
            // Section to the top
            else if (diff == EngineSettings.ChunkConfig.MaskY)
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

                int index = Common.Helpers.GetIndex1DFrom3D(context.BX, context.BY, context.BZ);
                Blocks[index] = context.Block;
                                
                int section = context.BY >> EngineSettings.ChunkConfig.LogSizeY;

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