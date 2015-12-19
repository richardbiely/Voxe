using System.Collections.Generic;
using System.IO;
using System.Threading;
using Assets.Engine.Scripts.Builders;
using Assets.Engine.Scripts.Common.DataTypes;
using Assets.Engine.Scripts.Common.Extensions;
using Assets.Engine.Scripts.Common.Threading;
using Assets.Engine.Scripts.Core.Blocks;
using Assets.Engine.Scripts.Core.Threading;
using Assets.Engine.Scripts.Provider;
using Assets.Engine.Scripts.Utils;
using UnityEngine;
using Assert = UnityEngine.Assertions.Assert;
using RenderBuffer = Assets.Engine.Scripts.Rendering.RenderBuffer;

namespace Assets.Engine.Scripts.Core.Chunks
{
    /// <summary>
    ///     Represents one vertical part of a chunk
    /// </summary>
    public class MiniChunk: ChunkEvent
    {
        #region Public Properties

        /// <summary>
        ///     The render buffer used for solid blocks
        /// </summary>
        public RenderBuffer SolidRenderBuffer { get; private set; }

        #endregion Public Properties

        #region Private variabls

        //! Queue of setBlock operations to execute
	    private readonly List<MiniChunk> m_setBlockSections;
	    private readonly List<SetBlockContext> m_setBlockQueue;

        private readonly int [] m_eventCnt;

        private readonly Chunk m_parentChunk;
        
        //! Next state after currently finished state
        private ChunkState m_notifyState;
        //! The state currently being executed - only for debugging purposes
        private ChunkState m_currentState;

        //! Tasks waiting to be executed
        private ChunkState m_pendingTasks;
        //! Tasks already executed
        private ChunkState m_completedTasks;

        private bool m_taskRunning;
        public bool TaskRunning
        {
            get
            {
                lock (m_lock)
                {
                    return m_taskRunning;
                }
            }
            set
            {
                lock (m_lock)
                {
                    m_taskRunning = value;
                }
            }
        }
        private readonly object m_lock = new object();

        public bool RequestedRemoval;

        #endregion Private variabls

        #region Public Fields

        //! Current LOD level of the chunk. NOTE: Leftover. This needs to be done completely differently
        public int LOD { get; set; }

        //! Number of blocks which not air (non-empty blocks)
        public int NonEmptyBlocks;

        public int OffsetY { get; private set; }

        public Vector2Int Pos
        {
            get { return m_parentChunk.Pos; }
        }

        public Vector2Int PosAbs
        {
            get { return m_parentChunk.Pos; }
        }

        #endregion Public Fields

        #region Constructor

        public MiniChunk(Chunk parentChunk, int positionY) :
            base(
                // With stacksize=1, only 4 neighbors are possible
                (EngineSettings.ChunkConfig.StackSize == 1) ? 4 :
                (
                    // Stacksize=2 allow for 5 neighbors only
                    (EngineSettings.ChunkConfig.StackSize == 2) ? 5 :
                    // With greater stacksizes 5 or 6 cubical neighbors are possible depending on Y offset
                    ((positionY == 0 || positionY == EngineSettings.ChunkConfig.StackSize - 1) ? 5 : 6)
                )
                )
        {
            m_parentChunk = parentChunk;
            OffsetY = positionY * EngineSettings.ChunkConfig.SizeY;

            m_setBlockQueue = new List<SetBlockContext>();
            m_setBlockSections = new List<MiniChunk>();
            SolidRenderBuffer = new RenderBuffer();

            m_eventCnt = new[] {0, 0};

            Reset();
        }

        #endregion Constructor

        #region Accessor

        /// <summary>
        ///     Get the block at the local position
        /// </summary>
        public BlockData this[int x, int y, int z]
        {
            get { return m_parentChunk[x, y + OffsetY, z]; }
            set { m_parentChunk[x, y + OffsetY, z] = value; }
        }

        #endregion Accessor

        #region Public Methods

        public new void Reset()
        {
            LOD = 0;
            NonEmptyBlocks = 0;

            m_setBlockSections.Clear();
            m_setBlockQueue.Clear();

            for (int i = 0; i<m_eventCnt.Length; i++)
                m_eventCnt[i] = 0;

            RequestedRemoval = false;
            m_taskRunning = false;

            m_notifyState = m_notifyState.Reset();
            m_currentState = m_currentState.Reset();
            m_pendingTasks = m_pendingTasks.Reset();
            m_completedTasks = m_completedTasks.Reset();

            base.Reset();
        }
        public void MarkAsLoaded()
        {
            m_completedTasks =
                m_completedTasks.Set(ChunkState.Generate|ChunkState.GenerateBlueprints|ChunkState.FinalizeData);
        }

        public void Restore()
        {
            if (!RequestedRemoval)
                return;

            RequestedRemoval = false;

            if (EngineSettings.WorldConfig.Streaming)
                m_pendingTasks = m_pendingTasks.Reset(ChunkState.Remove);
            m_pendingTasks = m_pendingTasks.Reset(ChunkState.Remove);

            //UnregisterFromSubscribers();
        }

        public bool Finish()
        {
            if (!RequestedRemoval)
            {
                RequestedRemoval = true;

                if(EngineSettings.WorldConfig.Streaming)
                    OnNotified(ChunkState.Serialize);
                OnNotified(ChunkState.Remove);
            }

            // Wait until all work is finished on a given chunk
            bool isWorking = IsExecutingTask() || !IsFinished();
            return isWorking;
        }

        public void RegisterSubscriber(MiniChunk subscriber)
        {
            // Registration needs to be done both ways
            subscriber.Register(this, true);
            Register(subscriber, true);
        }

        public void UnregisterFromSubscribers()
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
            if(!m_completedTasks.Check(ChunkState.Remove))
                return false;

            if (m_pendingTasks != 0)
            {
                //Debug.LogFormat("[{0},{1}]: Pending:{0}", Pos.X, Pos.Z, m_pendingTasks);
            }

            return true;
        }

        public bool IsExecutingTask()
        {
            return TaskRunning;
        }

        private void QueueSetBlock(SetBlockContext context)
        {
	        int index = 0;

	        MiniChunk[] queued = new MiniChunk[6];

	        // Iterate over neighbors and decide which ones should be notified to rebuild
	        for (int i = 0; i < Subscribers.Length; i++)
	        {
		        ChunkEvent subscriber = Subscribers[i];
		        if (subscriber == null)
			        continue;

	            MiniChunk n = (MiniChunk)subscriber;

		        int ncy = n.OffsetY >> EngineSettings.ChunkConfig.LogSizeY;

		        if (n.Pos.Z == context.CZ && (
			        // Section to the left
			        ((context.BX == 0) && (n.Pos.X + 1 == context.CX)) ||
			        // Section to the right
			        ((context.BX == EngineSettings.ChunkConfig.MaskX) && (n.Pos.X - 1 == context.CX))
			        ))
			        queued[index++] = n;

		        if (n.Pos.X == context.CX && (
			        // Section to the front
			        ((context.BZ == EngineSettings.ChunkConfig.MaskZ) && (n.Pos.Z - 1 == context.CZ)) ||
			        // Section to the back
			        ((context.BZ == 0) && (n.Pos.Z + 1 == context.CZ))
			        ))
			        queued[index++] = n;

		        if (n.Pos.X == context.CX && n.Pos.Z == context.CZ && (
			        // Section to the top
			        ((context.BY == 0) && (ncy - 1 == context.CY)) ||
			        // Section to the bottom
			        ((context.BY == EngineSettings.ChunkConfig.MaskY) && (ncy + 1 == context.CY))
			        ))
			        queued[index++] = n;
	        }

	        // Merge requests with already existing ones
	        for (int i = 0; i < index; i++)
	        {
		        bool skip = false;
		        for (int j = 0; j < m_setBlockSections.Count; j++)
		        {
		            if (queued[i]!=m_setBlockSections[j])
                    continue;

		            skip = true;
		            break;
		        }

		        if (skip)
			        continue;

		        m_setBlockSections.Add(queued[i]);
	        }

	        // Queue this block itself as well
	        m_setBlockQueue.Add(context);
        }

        public void ProcessSetBlockQueue()
        {
            if (m_setBlockQueue.Count<=0)
		            return;
	
	          // Modify blocks
            for (int i = 0; i<m_setBlockQueue.Count; i++)
            {
                SetBlockContext context = m_setBlockQueue[i];

                BlockType prevType = this[context.BX, context.BY, context.BZ].BlockType;
                this[context.BX, context.BY, context.BZ] = context.Block;
                m_parentChunk.WasModifiedSinceLoaded = true;

                // If the block type changed we need to update the number of non-empty blocks
                if (prevType!=context.Block.BlockType)
                {
                    if(context.Block.BlockType==BlockType.None)
                        --NonEmptyBlocks;
                    else
                        ++NonEmptyBlocks;
                }
            }
            m_setBlockQueue.Clear();

	        // After modifing the blocks, let's send notifications
	        foreach (MiniChunk section in m_setBlockSections)
		        NotifyOne(section, ChunkState.Rebuild);
	        m_setBlockSections.Clear();

	        // Update this chunk if there was any request to do it
	        OnNotified(ChunkState.Rebuild);
        }

        public void ProcessPendingTasks(bool updateGeometry)
        {
            if (
                // Can't execute the task now, we'll try it later
                IsExecutingTask() ||
                // If we're done we're done
                IsFinished()
                )
                return;

            // Notify neighbor sections that this stage is complete
            // Note: Thanks to this, everything is perfectly thread-safe. We only modify m_doneState
            // and m_nextState on a different thread and if we know there's any task belonging to this
            // section in the thread pool we'll simply return from this method (aka one frame delay)
            if (m_notifyState!=ChunkState.Idle)
            {
                // Notify neighbors about our state.
                // FinalizeData is an exception because it is related to the chunk itself rather than
                // chunk's neighbors
                switch (m_notifyState)
                {
                    case ChunkState.GenerateBlueprints:
                        NotifyAll(m_notifyState);
                        break;
                    case ChunkState.FinalizeData:
                        NotifyAll(m_notifyState);
                        OnNotified(m_notifyState);
                        break;
                    default:
                        OnNotified(m_notifyState);
                        break;
                }

                m_notifyState = ChunkState.Idle;
            }

            // Go from the least important bit to most important one. If a given bit it set,
            // we execute the task tied with it

            if (m_pendingTasks.Check(ChunkState.Generate))
            {
                GenerateData();
            }
            else if (m_pendingTasks.Check(ChunkState.GenerateBlueprints))
            {
                GenerateBlueprints();
            }
            else if (m_pendingTasks.Check(ChunkState.FinalizeData))
            {
                FinalizeData();
            }
	        else if (m_pendingTasks.Check(ChunkState.Serialize))
	        {
		        //! TODO: Consider making it possible to serialize section and generate vertices at the same time
	            SerializeChunk();
	        }
            else if (m_pendingTasks.Check(ChunkState.Remove))
            {
                RemoveChunk();
            }
            else if (updateGeometry)
            {
                // Building vertices has the lowest priority for us. It's just the data we see.
                if (m_pendingTasks.Check(ChunkState.BuildVertices))
                {
                    GenerateVertices(false);
                }
                else if (m_pendingTasks.Check(ChunkState.Rebuild))
                {
                    GenerateVertices(true);
                }
            }
        }

        #region GenerateData

        private static readonly ChunkState CurrStateGenerateData = ChunkState.Generate;
        private static readonly ChunkState NextStateGenerateData = ChunkState.GenerateBlueprints;

        private static void OnGenerateData(MiniChunk section)
        {
            Map.Current.ChunkProvider.GetGenerator().Generate(section);

            OnGenerateDataDone(section);
        }

        private static void OnGenerateDataDone(MiniChunk section)
        {
            section.m_completedTasks = section.m_completedTasks.Set(CurrStateGenerateData);
            section.m_notifyState = NextStateGenerateData;
            section.TaskRunning = false;
        }

        private void GenerateData()
        {
            m_pendingTasks = m_pendingTasks.Reset(CurrStateGenerateData);

            if (m_completedTasks.Check(CurrStateGenerateData))
            {
                OnGenerateDataDone(this);
                return;
            }

            m_currentState = CurrStateGenerateData;
            m_completedTasks = m_completedTasks.Reset(CurrStateGenerateData);

            TaskRunning = true;
            WorkPoolManager.Add(new ThreadItem(
                                 arg =>
                                 {
                                     MiniChunk section = (MiniChunk)arg;
                                     OnGenerateData(section);
                                 },
                                 this)
                );
        }

        #endregion

        #region GenerateBlueprints

        private static readonly ChunkState CurrStateGenerateBlueprints = ChunkState.GenerateBlueprints;
        private static readonly ChunkState NextStateGenerateBlueprints = ChunkState.FinalizeData;
        
        private static void OnGenerateBlueprints(MiniChunk section)
        {
            // !TODO: Generate blueprints here
            int sectionsGenerated = Interlocked.Increment(ref section.m_parentChunk.SectionsGenerated);
            Assert.IsTrue(
                sectionsGenerated <= section.m_parentChunk.Sections.Length,
                string.Format("OnGenerateBlueprints: Parent chunk sections generated {0}. Expected {1}", sectionsGenerated, section.m_parentChunk.Sections.Length)
                );

            OnGenerateBlueprintsDone(section);
        }

        private static void OnGenerateBlueprintsDone(MiniChunk section)
        {
            section.m_completedTasks = section.m_completedTasks.Set(CurrStateGenerateBlueprints);
            section.m_notifyState = NextStateGenerateBlueprints;
            section.TaskRunning = false;
        }

        private void GenerateBlueprints()
        {
            Assert.IsTrue(m_completedTasks.Check(ChunkState.Generate),
                string.Format("[{0},{1}] - GenerateBlueprints set sooner than Generate completed", Pos.X, Pos.Z)
                );
            if (!m_completedTasks.Check(ChunkState.Generate))
                return;

            m_pendingTasks = m_pendingTasks.Reset(CurrStateGenerateBlueprints);

            if (m_completedTasks.Check(ChunkState.GenerateBlueprints))
            {
                OnGenerateBlueprintsDone(this);
                return;
            }

            m_currentState = CurrStateGenerateBlueprints;
            m_completedTasks = m_completedTasks.Reset(CurrStateGenerateBlueprints);

            TaskRunning = true;
            WorkPoolManager.Add(new ThreadItem(
                                    arg =>
                                    {
                                        MiniChunk section = (MiniChunk)arg;
                                        OnGenerateBlueprints(section);
                                    },
                                    this)
                );
        }

        #endregion

        #region FinalizeData

        private static readonly ChunkState CurrStateFinalizeData = ChunkState.FinalizeData;
        private static readonly ChunkState NextStateFinalizeData = ChunkState.BuildVertices;

        private static void OnFinalizeData(Chunk chunk)
        {
            // Generate height limits
            chunk.CalculateHeightIndexes();

            // Compress chunk
            chunk.RLE.Reset();
            chunk.RLE.Compress(chunk.Blocks.ToArray());

            OnFinalizeDataDone(chunk, false);
        }

        private static void OnFinalizeDataDone(Chunk chunk, bool resetPending)
        {
            if (resetPending)
            {
                foreach (MiniChunk s in chunk.Sections)
                {
                    s.m_pendingTasks = s.m_pendingTasks.Reset(CurrStateFinalizeData);
                    s.m_completedTasks = s.m_completedTasks.Set(CurrStateFinalizeData);
                    s.m_notifyState = NextStateFinalizeData;
                    s.TaskRunning = false;
                }
            }
            else
            {
                foreach (MiniChunk s in chunk.Sections)
                {
                    s.m_completedTasks = s.m_completedTasks.Set(CurrStateFinalizeData);
                    s.m_notifyState = NextStateFinalizeData;
                    s.TaskRunning = false;
                }
            }
        }

        private void FinalizeData()
        {
            // All sections must be idle
            foreach (MiniChunk section in m_parentChunk.Sections)
            {
                if (section.IsExecutingTask())
                    return;

                // All sections must have blueprints generated first
                Assert.IsTrue(
                    section.m_completedTasks.Check(ChunkState.GenerateBlueprints),
                    string.Format("[{0},{1}] - FinalizeData set sooner than GenerateBlueprints completed", Pos.X, Pos.Z)
                    );
                if (!section.m_completedTasks.Check(ChunkState.GenerateBlueprints))
                    return;
            }

            // If there's any section with FinalizeData set, given the nature of this level of notification, we can
            // be sure the rest of them are set as well
            int finalized = 0;
            foreach (MiniChunk section in m_parentChunk.Sections)
            {
                if (section.m_completedTasks.Check(ChunkState.FinalizeData))
                    ++finalized;
            }
            if (finalized>0)
            {
                if (finalized==m_parentChunk.Sections.Length)
                {
                    OnFinalizeDataDone(m_parentChunk, true);
                    return;
                }
                else
                {
                    //Debug.LogFormat("[{0},{1}] - finalized {2}/{3}", Pos.X, Pos.Z, finalized, m_parentChunk.Sections.Length);  
                }
            }

            // Once all sections are generated the first section to get here handles finalization of the whole chunk.
            // At this points it's safe to access SectionGenerated in a non-interlocked way. We're only reading the value
            // and we only continue if it reaches Sections.Length which only happens when all previous tasks are finished.
            int sectionsGenerated = m_parentChunk.SectionsGenerated;
            bool correctNumberOfSectionsGenerated = sectionsGenerated <= m_parentChunk.Sections.Length;
            Assert.IsTrue(correctNumberOfSectionsGenerated,
                          string.Format("FinalizeData: Parent chunk sections generated {0}. Expected {1}", sectionsGenerated,
                                        m_parentChunk.Sections.Length));
            if (sectionsGenerated != m_parentChunk.Sections.Length)
                return;
            m_parentChunk.SectionsGenerated = 0;

            foreach (MiniChunk section in m_parentChunk.Sections)
            {
                section.m_pendingTasks = section.m_pendingTasks.Reset(CurrStateFinalizeData);
                section.m_completedTasks = section.m_completedTasks.Reset(CurrStateFinalizeData);
                section.TaskRunning = true;
            }

            WorkPoolManager.Add(new ThreadItem(
                                 arg =>
                                 {
                                     MiniChunk section = (MiniChunk)arg;
                                     OnFinalizeData(section.m_parentChunk);
                                 },
                                 this)
                );
        }

        #endregion

        #region SerializeChunk

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

            OnSerialzeChunkDone(chunk);
        }

        private static void OnSerialzeChunkDone(Chunk chunk)
        {
            foreach (MiniChunk section in chunk.Sections)
            {
                section.m_completedTasks = section.m_completedTasks.Set(CurrStateSerializeChunk);
                section.TaskRunning = false;
            }
        }

        private void SerializeChunk()
        {
            // This state should only be set it streaming is enabled
            Assert.IsTrue(EngineSettings.WorldConfig.Streaming);

            // If chunk was generated...
            if (m_completedTasks.Check(ChunkState.Generate))
            {
                // ...  we need to wait until blueprints are generated and chunk is finalized
                if (!m_completedTasks.Check(ChunkState.GenerateBlueprints | ChunkState.FinalizeData))
                    return;
            }

            m_pendingTasks = m_pendingTasks.Reset(CurrStateSerializeChunk);
            m_completedTasks = m_completedTasks.Reset(CurrStateSerializeChunk);

            ++m_parentChunk.SectionsStored;

            // Once all sections are marked as removed we can proceed with storing the chunk to disk
            if (
                m_parentChunk.SectionsStored == EngineSettings.ChunkConfig.StackSize &&
                m_parentChunk.WasModifiedSinceLoaded
                )
            {
                m_parentChunk.SectionsStored = 0;
                m_parentChunk.WasModifiedSinceLoaded = false;

                int nonEmptyBlocks = 0;
                foreach (MiniChunk section in m_parentChunk.Sections)
                {
                    section.TaskRunning = true;
                    nonEmptyBlocks += section.NonEmptyBlocks;
                }

                if (nonEmptyBlocks<=0)
                    OnSerialzeChunkDone(m_parentChunk);
                else
                {
                    SSerializeWorkItem workItem = new SSerializeWorkItem(
                        m_parentChunk,
                        LocalChunkProvider.GetFilePathFromIndex(Pos.X, Pos.Z)
                    );

                    IOPoolManager.Add(new ThreadItem(
                                          arg =>
                                          {
                                              SSerializeWorkItem item = (SSerializeWorkItem)arg;
                                              OnSerializeChunk(item.Chunk, item.FilePath);
                                          },
                                          workItem
                                          )
                        );
                }
            }
        }

        #endregion

        #region GenerateVertices

        private struct SGenerateVerticesWorkItem
        {
            public readonly int MinY;
            public readonly int MaxY;
            public readonly bool Rebuild;
            public readonly MiniChunk Section;

            public SGenerateVerticesWorkItem(int minY, int maxY, bool rebuild, MiniChunk section)
            {
                MinY = minY;
                MaxY = maxY;
                Section = section;
                Rebuild = rebuild;
            }
        }

        private static readonly ChunkState CurrStateGenerateVertices = ChunkState.BuildVertices;
        private static readonly ChunkState NextStateGenerateVertices = ChunkState.Idle;

        private static void OnGenerateVerices(MiniChunk section, int minY, int maxY, bool isRebuild)
        {
            section.SolidRenderBuffer.Clear();

            int offsetX = section.m_parentChunk.Pos.X*EngineSettings.ChunkConfig.SizeX;
            int offsetY = section.OffsetY;
            int offsetZ = section.m_parentChunk.Pos.Z*EngineSettings.ChunkConfig.SizeZ;

            const int lod = 1;
            const int lod2 = 1;

            for (int y = minY; y<=maxY; y += lod)
            {
                int wy = y+offsetY;

                for (int z = 0; z<EngineSettings.ChunkConfig.SizeZ; z += lod)
                {
                    int wz = z+offsetZ;

                    for (int x = 0; x<EngineSettings.ChunkConfig.SizeX; x += lod)
                    {
                        BlockData block = section[x, y, z];
                        if (block.IsEmpty())
                            continue;

                        IBlockBuilder builder = BlockDatabase.GetBlockBuilder(block.BlockType);
                        if (builder==null)
                        {
                            Assert.IsTrue(false, string.Format("No builder exists for blockType={0}", block.BlockType));
                            continue;
                        }

                        int wx = x+offsetX;

                        Vector3Int worldPos = new Vector3Int(wx, wy, wz);
                        Vector3Int localOffset = new Vector3Int(x, wy, z);
                        builder.Build(Map.Current, section.SolidRenderBuffer, ref block, ref worldPos, ref localOffset, lod, lod2);
                    }
                }
            }
            
            OnGenerateVerticesDone(section, isRebuild);
        }

        private static void OnGenerateVerticesDone(MiniChunk section, bool isRebuild)
        {
            if (isRebuild)
                Interlocked.Exchange(ref section.m_parentChunk.SectionsBuilt, EngineSettings.ChunkConfig.StackSize);
            else
                Interlocked.Increment(ref section.m_parentChunk.SectionsBuilt);

            section.m_completedTasks = section.m_completedTasks.Set(CurrStateGenerateVertices);
            section.m_notifyState = NextStateGenerateVertices;
            section.TaskRunning = false;
        }

        /// <summary>
        ///     Build this minichunk's render buffers
        /// </summary>
        private void GenerateVertices(bool isRebuild)
        {
            Assert.IsTrue(
                    m_completedTasks.Check(ChunkState.FinalizeData),
                    string.Format("[{0},{1}] - GenerateVertices set sooner than FinalizeData completed", Pos.X, Pos.Z)
                    );
            if (!m_completedTasks.Check(ChunkState.FinalizeData))
                return;

            m_completedTasks = m_completedTasks.Reset(CurrStateGenerateVertices);
            m_pendingTasks = m_pendingTasks.Reset(ChunkState.Rebuild | CurrStateGenerateVertices);

            int ySectionMin = OffsetY;
            int ySectionMax = ySectionMin+EngineSettings.ChunkConfig.MaskY;

            int lowest = Mathf.Max(m_parentChunk.LowestEmptyBlockOffset-LOD-1, 0);
            int highest = Mathf.Min(m_parentChunk.HighestSolidBlockOffset+LOD+1, EngineSettings.ChunkConfig.SizeYTotal-1);

            if (NonEmptyBlocks>0 && lowest<=ySectionMax && highest>=ySectionMin)
            {
                SGenerateVerticesWorkItem workItem = new SGenerateVerticesWorkItem(
                    Mathf.Max(lowest, ySectionMin)&EngineSettings.ChunkConfig.MaskX,
                    Mathf.Min(highest, ySectionMax)&EngineSettings.ChunkConfig.MaskZ,
                    isRebuild, this
                    );

                TaskRunning = true;
                WorkPoolManager.Add(new ThreadItem(
                                     arg =>
                                     {
                                         SGenerateVerticesWorkItem item = (SGenerateVerticesWorkItem)arg;
                                         OnGenerateVerices(item.Section, item.MinY, item.MaxY, item.Rebuild);
                                     },
                                     workItem)
                    );
            }
            else
            {
                OnGenerateVerticesDone(this, isRebuild);
            }
        }

        #endregion

        #region RemoveChunk

        private static readonly ChunkState CurrStateRemoveChunk = ChunkState.Remove;

        private void RemoveChunk()
        {
            // If chunk was generated we need to wait for other state with higher priority to finish first in order for the
            // events to be properly propagated to registered neighbor sections
            if (m_completedTasks.Check(ChunkState.Generate))
            {
                if (SubscribersCurr==Subscribers.Length)
                {
                    if (!m_completedTasks.Check(
                        ChunkState.GenerateBlueprints|
                        ChunkState.FinalizeData|
                        // With streaming enabled we have to wait for serialization to finish as well
                        (EngineSettings.WorldConfig.Streaming ? ChunkState.Serialize : ChunkState.Idle)))
                        return;
                }
                else
                {
                    
                }
            }

            m_pendingTasks = m_pendingTasks.Reset(CurrStateRemoveChunk);
            m_completedTasks = m_completedTasks.Set(CurrStateRemoveChunk);
        }

        #endregion

        /// <summary>
        ///     Damage the given block, destroying it if block damage hits max
        /// </summary>
        public void DamageBlock(int x, int y, int z, int damage)
        {
            if (damage == 0)
                return;

            var thisBlock = this[x, y, z];
            int ly = y + OffsetY;
            int cy = ly>>EngineSettings.ChunkConfig.LogSizeY;

            int blockDmgLevel = thisBlock.GetDamage();
            if (blockDmgLevel + damage >= 15)
            {
                QueueSetBlock(new SetBlockContext(BlockData.Air, x, y, z, m_parentChunk.Pos.X, cy, m_parentChunk.Pos.Z));
                return;
            }

            thisBlock.SetDamage((byte)(blockDmgLevel+damage));
            QueueSetBlock(new SetBlockContext(thisBlock, x, y, z, m_parentChunk.Pos.X, cy, m_parentChunk.Pos.Z));
        }

        public void ModifyBlock(int x, int y, int z, BlockData blockData)
        {
            BlockData thisBlock = this[x, y, z];

            // Do nothing if there's no change
            if (blockData.CompareTo(thisBlock)==0)
                return;

            // Meta data must be different as well
            if (blockData.GetMeta()==thisBlock.GetMeta())
                return;

            int ly = y + OffsetY;
            int cy = ly>>EngineSettings.ChunkConfig.LogSizeY;

            QueueSetBlock(new SetBlockContext(blockData, x, y, z, m_parentChunk.Pos.X, cy, m_parentChunk.Pos.Z));
        }

        #endregion Public Methods

        #region ChunkEvent

        public override void OnRegistered(bool registerListener)
        {
            // Nothing to do when unsubscribing
            if (!registerListener)
                return;

            OnNotified(ChunkState.Generate);
        }

        public override void OnNotified(ChunkState state)
        {
            if (state==ChunkState.GenerateBlueprints || state==ChunkState.FinalizeData)
            {
                int eventIndex = ((int)state-(int)ChunkState.GenerateBlueprints) >> 1;

                // Check completition
                int cnt = ++m_eventCnt[eventIndex];
                if (cnt<Subscribers.Length)
                    return;

                Assert.IsTrue(cnt==Subscribers.Length, string.Format("Cnt:{0}, Curr:{1}", cnt, Subscribers.Length));

                // Reset counter and process/queue event
                m_eventCnt[eventIndex] = 0;
            }

            // Queue operation
            m_pendingTasks = m_pendingTasks.Set(state);
        }

        #endregion ChunkEvent        

        #region Object overrides
        /*public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is Vector2Int && Equals((Vector2Int)obj);
        }

        protected bool Equals(MiniChunk other)
        {
            return OffsetY==other.OffsetY && Pos.X==other.Pos.X && Pos.Z==other.Pos.Z;
        }

        public override int GetHashCode()
        {
            return Pos.X*1024*1024 + Pos.Z*1024 + OffsetY;
        }*/

        #endregion
    }
}