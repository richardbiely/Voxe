using System;
using Assets.Engine.Scripts.Common.DataTypes;
using Assets.Engine.Scripts.Common.IO.RLE;
using Assets.Engine.Scripts.Core.Blocks;
using Assets.Engine.Scripts.Physics;
using Assets.Engine.Scripts.Rendering;
using Assets.Engine.Scripts.Utils;
using Mono.Simd;
using UnityEngine;
using Assert = UnityEngine.Assertions.Assert;

namespace Assets.Engine.Scripts.Core.Chunks
{
    /// <summary>
    ///     Represents a chunk consisting of several even sized mini chunks
    /// </summary>
    public class Chunk
    {
        #region Public Vars
        
        public readonly RLE<BlockData> RLE;

        public readonly IBlockStorage Blocks;

        public readonly MiniChunk[] Sections;

        // Bounding box of this chunk
        public Bounds ChunkBounds;

        // Chunk coordinates
        public Vector2Int Pos;

        // Helpers offsets
        public int HighestSolidBlockOffset;

        public int LowestEmptyBlockOffset;

        public bool Visible;

        public bool WasModifiedSinceLoaded { get; set; }

        #endregion Public Vars

        #region Private Vars

        // the draw call batcher for this chunk
        private readonly DrawCallBatcher m_drawCallBatcher = new DrawCallBatcher();

        public int SectionsGenerated;
        public int SectionsBuilt;
        public int SectionsStored;

        #endregion Private Vars

        #region Constructors
        
        public Chunk()
        {
            RLE = new RLE<BlockData>();
            Blocks = new BlockStorage();

            Sections = new MiniChunk[EngineSettings.ChunkConfig.StackSize];
            for (int i = 0; i<Sections.Length; i++)
                Sections[i] = new MiniChunk(this, i);

            ChunkBounds = new Bounds();
            ChunkBounds.SetMinMax(Vector3.zero, new Vector3(EngineSettings.ChunkConfig.SizeX, EngineSettings.ChunkConfig.SizeYTotal, EngineSettings.ChunkConfig.SizeZ));

            Reset(false);
        }

        #endregion Constructors

        #region Accessors

        /// <summary>
        ///     Get or set the block at the local position
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

		private static void RegisterNeighbor(MiniChunk section, int offsetY, Chunk neighbor)
		{
			if (section == null || neighbor == null)
				return;

			foreach (MiniChunk s in neighbor.Sections)
			{
				// Find a section with the same height offset in a neighbor chunk
				if (s.OffsetY == offsetY)
				{
					section.RegisterSubscriber(s);
					return;
				}
			}
		}

		public void RegisterNeighbors()
		{
			Chunk left = Map.Current.GetChunk(Pos.X - 1, Pos.Z);
			Chunk right = Map.Current.GetChunk(Pos.X + 1, Pos.Z);
			Chunk front = Map.Current.GetChunk(Pos.X, Pos.Z - 1);
			Chunk behind = Map.Current.GetChunk(Pos.X, Pos.Z + 1);

			foreach (MiniChunk section in Sections)
			{
				// Left
				RegisterNeighbor(section, section.OffsetY, left);
				// Right
				RegisterNeighbor(section, section.OffsetY, right);
				// Front
				RegisterNeighbor(section, section.OffsetY, front);
				// Behind
				RegisterNeighbor(section, section.OffsetY, behind);
				// Above
				int max = section.OffsetY + EngineSettings.ChunkConfig.SizeY;
				if (max < EngineSettings.ChunkConfig.SizeYTotal)
					RegisterNeighbor(section, max, this);
				// Bellow
				int min = section.OffsetY - EngineSettings.ChunkConfig.SizeY;
				if (min >= 0)
					RegisterNeighbor(section, min, this);
			}
		}

        #region Public Methods

#if UNITY_STANDALONE_WIN
        public static readonly Vector4i Vec5 = new Vector4i(5, 5, 5, 0);
        public static readonly Vector4i VecSize = new Vector4i(EngineSettings.ChunkConfig.SizeX, EngineSettings.ChunkConfig.SizeY, EngineSettings.ChunkConfig.SizeZ, 0);
        public static readonly Vector4i VecSize1 = new Vector4i(EngineSettings.ChunkConfig.MaskX, EngineSettings.ChunkConfig.SizeYTotal-1, EngineSettings.ChunkConfig.MaskZ, 0);
#endif

        public void UpdateChunk()
        {
            foreach (MiniChunk section in Sections)
            {
                section.ProcessSetBlockQueue();
                section.ProcessPendingTasks(Visible);
            }
        }

        public void Reset(bool canBeLoaded)
        {
            // Reset sections
            foreach (MiniChunk section in Sections)
                section.UnregisterFromSubscribers();

            // Reset blocks
            Blocks.Reset();

            WasModifiedSinceLoaded = true;
            Visible = false;

            SectionsGenerated = 0;
            SectionsBuilt = 0;
            SectionsStored = 0;

            LowestEmptyBlockOffset = EngineSettings.ChunkConfig.SizeYTotal-1;
            HighestSolidBlockOffset = 0;

            Clean();
            foreach (MiniChunk section in Sections)
                section.Reset();
        }

        public void SetVisible(bool show)
        {
            Visible = show;
            m_drawCallBatcher.SetVisible(show);
        }

        /// <summary>
        ///     Damage the given block, destroying it if it takes too much damage
        /// </summary>
        public void DamageBlock(int x, int y, int z, int damage)
        {
            int i = y>>EngineSettings.ChunkConfig.LogSizeY;
            int ly = y&EngineSettings.ChunkConfig.MaskY;

            Sections[i].DamageBlock(x, ly, z, damage);
        }

        /// <summary>
        ///     Returns whether chunk is in camera view
        /// </summary>
        public bool CheckFrustum(Plane[] frustum)
        {
            Bounds bounds = ChunkBounds;
            bounds.center += new Vector3(Pos.X*EngineSettings.ChunkConfig.SizeX, 0f, Pos.Z*EngineSettings.ChunkConfig.SizeZ);

            return GeometryUtility.TestPlanesAABB(frustum, bounds);
        }

        /// <summary>
        ///     Returns whether any block in the chunk intersects the given AABB
        /// </summary>
        public bool Intersects(Bounds aabb)
        {
            Bounds bounds = ChunkBounds;
            bounds.center += new Vector3(Pos.X*EngineSettings.ChunkConfig.SizeX, 0f, Pos.Z*EngineSettings.ChunkConfig.SizeZ);

            return bounds.Intersects(aabb) && CheckBlocksAABB(aabb);
        }

        // test the chunk's blocks against the given AABB
        public bool CheckBlocksAABB(Bounds bounds)
        {
            // Non-SIMD
            Vector3Int pom = new Vector3Int(Pos.X*EngineSettings.ChunkConfig.SizeX, 0, Pos.Z*EngineSettings.ChunkConfig.SizeZ);

            int minX = Mathf.Clamp(Mathf.FloorToInt(bounds.min.x) - pom.X - 5, 0, EngineSettings.ChunkConfig.MaskX);
            int minY = Mathf.Clamp(Mathf.FloorToInt(bounds.min.y) - 5, 0, EngineSettings.ChunkConfig.SizeYTotal - 1);
            int minZ = Mathf.Clamp(Mathf.FloorToInt(bounds.min.z) - pom.Z - 5, 0, EngineSettings.ChunkConfig.MaskZ);
            Vector3Int bMin = new Vector3Int(minX, minY, minZ);

            int maxX = Mathf.Clamp((int)(bounds.max.x) - pom.X + 5, 0, EngineSettings.ChunkConfig.MaskX);
            int maxY = Mathf.Clamp((int)(bounds.max.y) + 5, 0, EngineSettings.ChunkConfig.SizeYTotal - 1);
            int maxZ = Mathf.Clamp((int)(bounds.max.z) - pom.Z + 5, 0, EngineSettings.ChunkConfig.MaskZ);
            Vector3Int bMax = new Vector3Int(maxX, maxY, maxZ);

            /*
            // SIMD
            Vector4f bMinf = new Vector4f(bounds.min.x, bounds.min.y, bounds.min.z, 0f);
            Vector4i bMin = bMinf.ConvertToIntTruncated();
            Vector4f bMaxf = new Vector4f(bounds.max.x, bounds.max.y, bounds.max.z, 0f);
            Vector4i bMax = bMaxf.ConvertToInt();

            Vector4i pom = new Vector4i(Pos.X, 0, Pos.Z, 0) * VecSize;
            bMin -= pom;
            bMax -= pom;

            bMin -= Vec5;
            bMax += Vec5;

            bMin = bMin.Max(Vector4i.Zero).Min(VecSize1); // Clamp to 0..size (x,z) or 0..height (y) respectively
            bMax = bMax.Max(Vector4i.Zero).Min(VecSize1); // Clamp to 0..size (x,z) or 0..height (y) respectively
            */
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
        public void CalculateHeightIndexes()
        {
            foreach (MiniChunk section in Sections)
            {
                Debug.Assert(section.NonEmptyBlocks==0,
                             string.Format("Section expected to have zero nonEmptyBlocks but has {0} instead",
                                           section.NonEmptyBlocks));
                section.NonEmptyBlocks = 0;
            }

            int nonEmptyBlocks = 0;

            for (int y = EngineSettings.ChunkConfig.SizeYTotal-1; y>=0; y--)
            {
                int sectionIndex = y>>EngineSettings.ChunkConfig.LogSizeY;

                for (int z = 0; z<EngineSettings.ChunkConfig.SizeZ; z++)
                {
                    for (int x = 0; x<EngineSettings.ChunkConfig.SizeX; x++)
                    {
                        bool isEmpty = this[x, y, z].IsEmpty();
                        if (!isEmpty)
                        {
                            ++nonEmptyBlocks;
                            ++Sections[sectionIndex].NonEmptyBlocks;
                        }

                        if ((y>HighestSolidBlockOffset) && !isEmpty)
                            HighestSolidBlockOffset = y;
                        else if ((LowestEmptyBlockOffset>y) && isEmpty)
                            LowestEmptyBlockOffset = y;
                    }
                }
            }

            if (nonEmptyBlocks==0)
            {
                Debug.LogFormat("only empty blocks in chunk [{0},{1}]. Min={2}, Max={3}", Pos.X, Pos.Z, LowestEmptyBlockOffset, HighestSolidBlockOffset);
            }

            LowestEmptyBlockOffset = Math.Min(--LowestEmptyBlockOffset, 0);
        }

        public void Clean()
        {
            m_drawCallBatcher.Clear();
        }

        public void Restore()
        {
            /*if (Sections[0].RequestedRemoval)
            {
                foreach (MiniChunk section in Sections)
                    section.Restore();

                //RegisterNeighbors();
            }*/
        }

        public bool Finish()
        {
            bool allDone = true;

            foreach (MiniChunk section in Sections)
            {
                // Request finish
                if (section.Finish())
                    allDone = false;

                // Try to process what's left if there's still something to do
                section.ProcessSetBlockQueue();
                section.ProcessPendingTasks(false);
            }

            return allDone;
        }

        public void Prepare(MiniChunk section)
        {
            if (section.SolidRenderBuffer.Positions.Count<=0)
                return;

            m_drawCallBatcher.Pos = new Vector3(Pos.X*EngineSettings.ChunkConfig.SizeX, 0, Pos.Z*EngineSettings.ChunkConfig.SizeZ);

            // Batch draw calls
            m_drawCallBatcher.Batch(section.SolidRenderBuffer);

            // Clear section buffer
            section.SolidRenderBuffer.Clear();/*!!!!*/
        }

        public void Submit()
        {
            m_drawCallBatcher.FinalizeDrawCalls();
        }

        #endregion Public Methods
    }
}