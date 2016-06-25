using System.Collections.Generic;
using Engine.Scripts.Builders;
using Engine.Scripts.Common;
using Engine.Scripts.Common.DataTypes;
using Engine.Scripts.Core.Blocks;
using Engine.Scripts.Core.Chunks.Managers;
using Engine.Scripts.Core.Chunks.States;
using Engine.Scripts.Core.Pooling;
using Engine.Scripts.Rendering;
using Engine.Scripts.Rendering.Batchers;
using UnityEngine;

namespace Engine.Scripts.Core.Chunks
{
    /// <summary>
    ///     Represents a chunk consisting of several even sized mini chunks
    /// </summary>
	public class Chunk: IOcclusionEntity
    {
        private static int s_id;

        #region Public variables
        
        public Map Map { get; private set; }
        
        public readonly BlockStorage Blocks;
        
        //! Bounding box in world coordinates
        public Bounds WorldBounds { get; private set; }

        // Chunk coordinates
        public Vector3Int Pos { get; private set; }

        public ChunkStateManagerClient StateManager { get; private set; }

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
                    StateManager.OnNotified(StateManager, ChunkState.BuildVertices);
                }
            }
        }
        
        //! Number of blocks which not air (non-empty blocks)
        public int NonEmptyBlocks { get; set; }

        //! Says whether or not building of geometry can be triggered
        public bool PossiblyVisible { get; set; }        
        
        //! Manager taking care of render calls
        public RenderGeometryBatcher RenderGeometryBatcher { get; private set; }

        #endregion Public variables

        #region Private variables

        //! ThreadID associated with this chunk. Used when working with object pools in MT environment. Resources
        //! need to be release where they were allocated. Thanks to this, associated containers could be made lock-free
        public int ThreadID { get; private set; }

        //! Object pools used by this chunk
        public LocalPools Pools { get; private set; }
        
        //! Queue of setBlock operations to execute
        private readonly List<SetBlockContext> m_setBlockQueue = new List<SetBlockContext>();
        
        //! If true, removal of chunk has been requested and no further requests are going to be accepted
        private bool m_removalRequested;

        //! First finalization differs from subsequent ones
        private bool m_firstFinalization;

        //! Chunk's current level of detail
        private int m_lod;
        
        #endregion Private variables

        #region Constructors

        public static Chunk CreateChunk(Map map, int cx, int cy, int cz)
        {
            Chunk chunk = Globals.MemPools.ChunkPool.Pop();
            chunk.Init(map, cx, cy, cz, new ChunkStateManagerClient(chunk));
            return chunk;
        }

        public Chunk()
        {
            Blocks = new BlockStorage();

            // Associate Chunk with a certain thread and make use of its memory pool
            // This is necessary in order to have lock-free caches
            ThreadID = Globals.WorkPool.GetThreadIDFromIndex(s_id++);
            Pools = Globals.WorkPool.GetPool(ThreadID);

            RenderGeometryBatcher = new RenderGeometryBatcher(Globals.CubeMeshGeometryBuilder, this);
            BBoxVertices = new List<Vector3>();
            BBoxVerticesTransformed = new List<Vector3>();

            StateManager = new ChunkStateManagerClient(this);
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
        
        public void Init(Map map, int cx, int cy, int cz, IChunkStateManager stateManager)
        {
            Map = map;
            Pos = new Vector3Int(cx, cy, cz);
            m_lod = 0;
            StateManager = (ChunkStateManagerClient)stateManager;

            int sizeX = EngineSettings.ChunkConfig.Size<<map.VoxelLogScaleX;
            int sizeY = EngineSettings.ChunkConfig.Size<<map.VoxelLogScaleY;
            int sizeZ = EngineSettings.ChunkConfig.Size<<map.VoxelLogScaleZ;
            WorldBounds = new Bounds(
                new Vector3(sizeX*(cx+0.5f), sizeY*(cy+0.5f), sizeZ*(cz+0.5f)),
                new Vector3(sizeX, sizeY, sizeZ)
                );

            Reset();

            StateManager.Init();
        }
       
        #region Public Methods

        public void UpdateChunk()
        {
            if (!StateManager.CanUpdate())
                return;

            // Do not update blocks until the chunk has all its data prepared
            if (StateManager.IsStateCompleted(ChunkState.FinalizeData))
            {
                ProcessSetBlockQueue();
            }

            if (StateManager.IsStateCompleted(ChunkState.BuildVertices))
            {
                StateManager.SetMeshBuilt();
                Build();
            }

            StateManager.Update();
        }
        
        public void Reset()
        {
            // Reset chunk events
			m_removalRequested = false;
            m_firstFinalization = true;

            m_lod = 0;
            
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

            RenderGeometryBatcher.Clear();

            ResetGeometryBoundingMesh();

            StateManager.Reset();
        }

        /// <summary>
        ///     Changes a given block to a block of a different type
        /// </summary>
        public void ModifyBlock(int x, int y, int z, BlockData blockData)
		{
            int index = Helpers.GetIndex1DFrom3D(x, y, z);

            // Nothing for us to do if block did not change
            BlockData thisBlock = this[x,y,z];
            if (blockData.Equals(thisBlock))
				return;
            
            m_setBlockQueue.Add(new SetBlockContext(index, blockData, true));
        }

        private void ProcessSetBlockQueue()
        {
            if (m_setBlockQueue.Count <= 0)
                return;
            
            StateManager.RequestState(ChunkState.FinalizeData | ChunkState.BuildVerticesNow);

            int rebuildMask = 0;

            // Modify blocks
            for (int j = 0; j < m_setBlockQueue.Count; j++)
            {
                SetBlockContext context = m_setBlockQueue[j];
                
                int x, y, z;
                Helpers.GetIndex3DFrom1D(context.Index, out x, out y, out z);
                this[x,y,z] = context.Block;
                
                if (
                    // Only check neighbors if it is still needed
                    rebuildMask == 0x3f ||
                    // Only check neighbors when it is a change of a block on a chunk's edge
                    (((x + 1) & EngineSettings.ChunkConfig.Mask) > 1 &&
                     ((y + 1) & EngineSettings.ChunkConfig.Mask) > 1 &&
                     ((z + 1) & EngineSettings.ChunkConfig.Mask) > 1)
                    )
                    continue;

                int cx = Pos.X;
                int cy = Pos.Y;
                int cz = Pos.Z;

                // If it is an edge position, notify neighbor as well
                // Iterate over neighbors and decide which ones should be notified to rebuild                
                for (int i = 0; i < StateManager.Listeners.Length; i++)
                {
                    ChunkEvent listener = StateManager.Listeners[i];
                    if (listener == null)
                        continue;

                    // No further checks needed once we know all neighbors need to be notified
                    if (rebuildMask == 0x3f)
                        break;

                    ChunkStateManagerClient listenerChunk = (ChunkStateManagerClient)listener;

                    int lx = listenerChunk.chunk.Pos.X;
                    int ly = listenerChunk.chunk.Pos.Y;
                    int lz = listenerChunk.chunk.Pos.Z;

                    if ((ly == cy || lz == cz) &&
                        (
                            // Section to the left
                            ((x == 0) && (lx + EngineSettings.ChunkConfig.Mask == cx)) ||
                            // Section to the right
                            ((x == EngineSettings.ChunkConfig.Mask) && (lx - EngineSettings.ChunkConfig.Mask == cx))
                        ))
                        rebuildMask = rebuildMask | (1 << i);

                    if ((lx == cx || lz == cz) &&
                        (
                            // Section to the bottom
                            ((y == 0) && (ly + EngineSettings.ChunkConfig.Mask == cy)) ||
                            // Section to the top
                            ((y == EngineSettings.ChunkConfig.Mask) && (ly - EngineSettings.ChunkConfig.Mask == cy))
                        ))
                        rebuildMask = rebuildMask | (1 << i);

                    if ((ly == cy || lx == cx) &&
                        (
                            // Section to the back
                            ((z == 0) && (lz + EngineSettings.ChunkConfig.Mask == cz)) ||
                            // Section to the front
                            ((z == EngineSettings.ChunkConfig.Mask) && (lz - EngineSettings.ChunkConfig.Mask == cz))
                        ))
                        rebuildMask = rebuildMask | (1 << i);
                }
            }

            m_setBlockQueue.Clear();

            // Notify neighbors that they need to rebuilt their geometry
            if (rebuildMask > 0)
            {
                for (int j = 0; j < StateManager.Listeners.Length; j++)
                {
                    ChunkStateManagerClient listener = (ChunkStateManagerClient)StateManager.Listeners[j];
                    if (listener != null && ((rebuildMask >> j) & 1) != 0)
                        listener.RequestState(ChunkState.FinalizeData | ChunkState.BuildVerticesNow);
                }
            }
        }

        public void ResetGeometryBoundingMesh()
        {
            GeometryBounds = new Bounds();
            BBoxVertices.Clear();
            BBoxVerticesTransformed.Clear();
        }
       
        public void Build()
        {
            // Prepare chunk for rendering
            RenderGeometryBatcher.Commit();
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
        
        public void Finish()
        {
            if (m_removalRequested)
                return;

            m_removalRequested = true;

            if (EngineSettings.WorldConfig.Streaming)
                StateManager.OnNotified(StateManager, ChunkState.SaveData);
            StateManager.OnNotified(StateManager, ChunkState.Remove);
        }
        
#endregion Public Methods

#region IOcclusionEntity

        //! Boundaries of the mini chunk
        public Bounds GeometryBounds { get; set; }

        //! Make the occluder visible/invisible
        public bool Visible
        {
            get { return RenderGeometryBatcher.IsEnabled(); }
            set { RenderGeometryBatcher.Enable(value); }
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