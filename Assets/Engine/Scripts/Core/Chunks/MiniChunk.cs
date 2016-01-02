using System.Collections.Generic;
using Assets.Engine.Scripts.Common.DataTypes;
using Assets.Engine.Scripts.Core.Blocks;
using RenderBuffer = Assets.Engine.Scripts.Rendering.RenderBuffer;

namespace Assets.Engine.Scripts.Core.Chunks
{
    /// <summary>
    ///     Represents one vertical part of a chunk
    /// </summary>
    public class MiniChunk
    {
        #region Public Properties

        /// <summary>
        ///     The render buffer used for solid blocks
        /// </summary>
        public RenderBuffer SolidRenderBuffer { get; private set; }

        #endregion Public Properties

        #region Private variabls

        

        private readonly Chunk m_parentChunk;
        
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

        public MiniChunk(Chunk parentChunk, int positionY)
        {
            m_parentChunk = parentChunk;
            OffsetY = positionY * EngineSettings.ChunkConfig.SizeY;
            
            SolidRenderBuffer = new RenderBuffer();

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

        public void Reset()
        {
            LOD = 0;
            NonEmptyBlocks = 0;
        }

        #endregion Public Methods
    }
}