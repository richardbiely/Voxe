using Assets.Engine.Scripts.Common.DataTypes;
using RenderBuffer = Assets.Engine.Scripts.Rendering.RenderBuffer;
using Assets.Engine.Scripts.Rendering;
using UnityEngine;

namespace Assets.Engine.Scripts.Core.Chunks
{
    /// <summary>
    ///     Represents one vertical part of a chunk
    /// </summary>
    public class MiniChunk
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

        #endregion Public Properties

        #region Private variabls

        // Chunk owning this section
        private readonly Chunk m_parentChunk;
        // Draw call batcher for this chunk
        private readonly DrawCallBatcher m_drawCallBatcher;

        #endregion Private variabls

        #region Constructor

        public MiniChunk(Chunk parentChunk, int positionY)
        {
            m_parentChunk = parentChunk;

            m_drawCallBatcher = new DrawCallBatcher();
            SolidRenderBuffer = new RenderBuffer();

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
        }

        public void Build()
        {
            if (IsBuilt || SolidRenderBuffer.Positions.Count<=0)
                return;

            m_drawCallBatcher.Clear();
            m_drawCallBatcher.Pos = new Vector3(Pos.X << EngineSettings.ChunkConfig.LogSizeX, 0, Pos.Z << EngineSettings.ChunkConfig.LogSizeZ);
            m_drawCallBatcher.Batch(SolidRenderBuffer);
            m_drawCallBatcher.FinalizeDrawCalls();

            // Make sure the data is not regenerated all the time
            IsBuilt = true;

            // Clear original buffer
            SolidRenderBuffer.Clear();
        }

        public void SetVisible(bool visible)
        {
            m_drawCallBatcher.SetVisible(visible);
        }

        #endregion Public Methods
    }
}