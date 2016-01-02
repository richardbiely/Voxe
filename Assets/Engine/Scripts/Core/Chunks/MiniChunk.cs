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

        //! Queue of setBlock operations to execute
	    private readonly List<SetBlockContext> m_setBlockQueue;

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

            m_setBlockQueue = new List<SetBlockContext>();
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

            m_setBlockQueue.Clear();
        }

		public void EnqeueSetBlock(SetBlockContext context)
		{
			m_setBlockQueue.Add(context);
		}

		public bool ProcessSetBlockQueue()
		{
			if (m_setBlockQueue.Count<=0)
				return false;

			// Modify blocks
			for (int i = 0; i<m_setBlockQueue.Count; i++)
			{
				SetBlockContext context = m_setBlockQueue[i];

				int index = Common.Helpers.GetIndex1DFrom3D(context.BX, context.BY, context.BZ);
				BlockType prevType = m_parentChunk[index].BlockType;
				m_parentChunk[index] = context.Block;

				/*// Update information about highest solid and lowest empty block offset
				bool isEmpty = context.Block.IsEmpty();
				if ((context.BY>m_parentChunk.HighestSolidBlockOffset) && !isEmpty)
					m_parentChunk.HighestSolidBlockOffset = (short)context.BY;
				else if ((m_parentChunk.LowestEmptyBlockOffset>context.BY) && isEmpty)
					m_parentChunk.LowestEmptyBlockOffset = (short)context.BY;*/
				
				// If the block type changed we need to update the number of non-empty blocks
				/*if (prevType!=context.Block.BlockType)
				{
					if(context.Block.BlockType==BlockType.None)
						--NonEmptyBlocks;
					else
						++NonEmptyBlocks;
				}*/
			}
			m_setBlockQueue.Clear();

			return true;
		}

        #endregion Public Methods
    }
}