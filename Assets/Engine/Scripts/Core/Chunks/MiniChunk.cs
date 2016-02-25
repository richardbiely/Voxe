using System.Collections.Generic;
using Assets.Engine.Scripts.Builders;
using Assets.Engine.Scripts.Common.DataTypes;
using RenderBuffer = Assets.Engine.Scripts.Rendering.RenderBuffer;
using Assets.Engine.Scripts.Rendering;
using UnityEngine;

namespace Assets.Engine.Scripts.Core.Chunks
{
    /// <summary>
    ///     Represents one vertical part of a chunk
    /// </summary>
    public class MiniChunk: IOcclusionEntity
    {
        #region Public Properties

        //! The render buffer used for building MiniChunk geometry
        public RenderBuffer SolidRenderBuffer { get; private set; }        
        
        //! Current LOD level of the chunk. NOTE: Leftover. This needs to be done completely differently
        public int LOD { get; set; }

        //! True if MiniSection has already been built
        public bool IsBuilt { get; set; }

        public Vector2Int Pos
        {
            get { return m_parentChunk.Pos; }
        }

        //! Number of blocks which not air (non-empty blocks)
        public int NonEmptyBlocks { get; set; }

        public int OffsetY { get; private set; }

        //! Bounding box in world coordinates
        public Bounds WorldBounds { get; set; }

        #endregion Public Properties

        #region Private variabls

        //! Chunk owning this section
        private readonly Chunk m_parentChunk;
        //! Draw call batcher for this chunk
        private readonly DrawCallBatcher m_drawCallBatcher;

        #endregion Private variabls

        #region Constructor

        public MiniChunk(Chunk parentChunk, int positionY)
        {
            m_parentChunk = parentChunk;
            OffsetY = positionY * EngineSettings.ChunkConfig.Size;

            m_drawCallBatcher = new DrawCallBatcher(Globals.CubeMeshBuilder);
            SolidRenderBuffer = new RenderBuffer(Globals.CubeMeshBuilder);
            BBoxVertices = new List<Vector3>();
            BBoxVerticesTransformed = new List<Vector3>();

            Reset();
        }

        #endregion Constructor
        
        #region Public Methods

        public void Reset()
        {
            LOD = 0;
            NonEmptyBlocks = 0;            
            IsBuilt = false;

            m_drawCallBatcher.Clear();
            SolidRenderBuffer.Clear();

            ResetBoundingMesh();
        }

        public void ResetBoundingMesh()
        {
            BBoxVertices.Clear();
            BBoxVerticesTransformed.Clear();
        }

        public void Build()
        {
            if (IsBuilt || SolidRenderBuffer.Vertices.Count<=0)
                return;

            m_drawCallBatcher.Clear();
            m_drawCallBatcher.Pos = new Vector3Int(Pos.X, OffsetY >> EngineSettings.ChunkConfig.LogSize, Pos.Z);
            m_drawCallBatcher.Batch(SolidRenderBuffer);
            m_drawCallBatcher.FinalizeDrawCalls();

            // Make sure the data is not regenerated all the time
            IsBuilt = true;

            // Clear original buffer
            SolidRenderBuffer.Clear();
        }

        public void BuildBoundingMesh(ref Bounds bounds)
        {
            GeometryBounds = bounds;

            // Build a bounding box for the mini chunk
            CubeBuilderSimple.Build(BBoxVertices, ref bounds);

            // Make a copy of the bounding box
            BBoxVerticesTransformed.AddRange(BBoxVertices);
        }

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