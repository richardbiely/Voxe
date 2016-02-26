﻿using System.Collections.Generic;
using Assets.Engine.Scripts.Common;
using Assets.Engine.Scripts.Common.DataTypes;
using Assets.Engine.Scripts.Core.Blocks;
using Assets.Engine.Scripts.Physics;
using UnityEngine;
using UnityEngine.Assertions;
using System.Linq;
using Assets.Engine.Scripts.Builders.Geometry;
using Assets.Engine.Scripts.Common.Math;
using Assets.Engine.Scripts.Generators;
using Assets.Engine.Scripts.Rendering;

namespace Assets.Engine.Scripts.Core.Chunks
{
    public class Map: ChunkManager
    {
        #region Private vars

        //! Clipmap with precomputed static data useful for map processing
        private ChunkClipmap m_clipmap;

        //! Chunks to be removed
        private List<Chunk> m_chunksToRemove;
        
        private Plane[] m_cameraPlanes = new Plane[6];

        private Vector2Int[] m_chunksToLoadByPos;

        #endregion Private vars

        #region ChunkManager overrides

        protected override void OnAwake()
        {
            // Camera - set from the editor
            // ChunkGenerator - set from the editor

            m_clipmap = new ChunkClipmap();
            m_chunksToRemove = new List<Chunk>();
        }

        protected override void OnStart()
        {
            UpdateRangeRects();
            InitCache();
            UpdateCache();
        }

        #endregion Constructor

        #region Public Fields

        public Camera Camera;
        public AChunkGenerator ChunkGenerator;
        public AVoxelGeometryBuilder MeshBuilder;
        
        //! Position of viewer in chunk coordinates
        public Vector2Int ViewerChunkPos { get; private set; }

        public OcclusionCuller Occlusion;

        public float LODCoef = 1f;
        public int ForceLOD = -1;

        [Header("Debugging")]
        public bool ShowGeomBounds;
        public bool ShowMapBounds;

        #endregion Public Fields

        #region Public Methods

        /// <summary>
        ///     Returns a block at a given world position
        /// </summary>
        public BlockData GetBlock(int wx, int wy, int wz)
        {
            if (wy<0 || wy>=EngineSettings.ChunkConfig.SizeYTotal)
                return BlockData.Air;

            int cx = wx>>EngineSettings.ChunkConfig.LogSize;
            int cz = wz>>EngineSettings.ChunkConfig.LogSize;

            Chunk chunk = GetChunk(cx, cz);
            if (chunk==null)
                return BlockData.Air;

            int lx = wx&EngineSettings.ChunkConfig.Mask;
            int lz = wz&EngineSettings.ChunkConfig.Mask;

            return chunk[lx, wy, lz];
        }

        /// <summary>
        ///     Damage the block at the world position
        /// </summary>
        public void DamageBlock(int wx, int wy, int wz, int damage)
        {
            if (wy<0 || wy>=EngineSettings.ChunkConfig.SizeYTotal)
                return;

            int cx = wx>>EngineSettings.ChunkConfig.LogSize;
            int cz = wz>>EngineSettings.ChunkConfig.LogSize;

            Chunk chunk = GetChunk(cx, cz);
            if (chunk==null)
                return;

            int lx = wx&EngineSettings.ChunkConfig.Mask;
            int lz = wz&EngineSettings.ChunkConfig.Mask;

            chunk.DamageBlock(lx, wy, lz, damage);
        }

        /// <summary>
        ///     Set the block at the world position
        /// </summary>
        public void SetBlock(BlockData block, int wx, int wy, int wz)
        {
            if (wy<0 || wy>=EngineSettings.ChunkConfig.SizeYTotal)
                return;

            int cx = wx>>EngineSettings.ChunkConfig.LogSize;
            int cz = wz>>EngineSettings.ChunkConfig.LogSize;

            Chunk chunk = GetChunk(cx, cz);
            if (chunk==null)
                return;

            int lx = wx&EngineSettings.ChunkConfig.Mask;
            int lz = wz&EngineSettings.ChunkConfig.Mask;

			chunk.ModifyBlock(lx, wy, lz, block);
        }

        // We'll only allow a certain amount of chunks to be created per update
        // Currently, this number is derived from the visible world size divided by the number of updates per second
        // !TODO: Let this value large for now. Change it back / adjust it later
        //private static readonly int MaxChunksPerUpdate = 10000;//(EngineSettings.WorldConfig.VisibleRange * EngineSettings.WorldConfig.VisibleRange / 20);

        private bool IsChunkInViewFrustum(Chunk chunk)
        {
            // Check if the chunk lies within camera planes
            return chunk.CheckFrustum(m_cameraPlanes);
        }

        private void UpdateRangeRects()
        {
            // Update camera position
            int posX = Mathf.FloorToInt(Camera.transform.position.x) >> EngineSettings.ChunkConfig.LogSize;
            int posZ = Mathf.FloorToInt(Camera.transform.position.z) >> EngineSettings.ChunkConfig.LogSize;
            ViewerChunkPos = new Vector2Int(posX, posZ);

            // Update clipmap offset
            m_clipmap.SetOffset(ViewerChunkPos.X, ViewerChunkPos.Z);

            // Recalculate camera frustum planes
            Geometry.CalculateFrustumPlanes(Camera, ref m_cameraPlanes);
        }

        protected override void OnPreProcessChunks()
        {
            // Update world bounds
            if (EngineSettings.WorldConfig.Infinite)
            {
                UpdateRangeRects();
            }
        }

        protected override void OnProcessChunk(Chunk chunk)
        {
            bool removeChunk = false;

            // Chunk is within view frustum
            if (IsChunkInViewFrustum(chunk))
            {
                ChunkClipmapItem item = m_clipmap[chunk.Pos.X, chunk.Pos.Z];

                // Chunk is too far away. Remove it
                if (!m_clipmap.IsInsideBounds(chunk.Pos.X, chunk.Pos.Z))
                {
                    removeChunk = true;
                }
                // Chunk is within visibilty range. Full update with geometry generation is possible
                else if (item.IsWithinVisibleRange)
                {
                    chunk.LOD = item.LOD;
                    chunk.SetPossiblyVisible(true);
                    chunk.Restore();

                    // If occlusion culling is enabled we need to register it
                    if (EngineSettings.WorldConfig.OcclusionCulling && Occlusion!=null)
                    {
                        foreach (MiniChunk section in chunk.Sections)
                        {
                            section.Visible = false;
                            if (!chunk.IsFinalized())
                                continue;

                            Occlusion.RegisterEntity(section);
                        }
                    }
                    else
                        chunk.SetVisible(true);
                        
                    chunk.UpdateChunk();
                }
                // Chunk is within cached range. Full update except for geometry generation
                else// if (item.IsWithinCachedRange)
                {
                    chunk.LOD = item.LOD;
                    chunk.SetPossiblyVisible(false);
                    chunk.SetVisible(false);
                    chunk.Restore();
                    chunk.UpdateChunk();
                }
            }
            else
            {
                ChunkClipmapItem item = m_clipmap[chunk.Pos.X, chunk.Pos.Z];

                // Chunk is not visible and too far away. Remote it
                if (!m_clipmap.IsInsideBounds(chunk.Pos.X, chunk.Pos.Z))
                {
                    removeChunk = true;
                }
                // Chunk is not in viewfrustum but still within cached range
                else if (item.IsWithinCachedRange)
                {
                    chunk.LOD = item.LOD;
                    chunk.SetPossiblyVisible(false);
                    chunk.SetVisible(false);
                    chunk.Restore();
                    chunk.UpdateChunk();
                }
            }

            if (removeChunk)
                UnregisterChunk(chunk.Pos);
        }

        protected override void OnPostProcessChunks()
        {
            // Perform occlussion culling
            if (EngineSettings.WorldConfig.OcclusionCulling && Occlusion!=null)
            {
                Occlusion.PerformOcclusion();
            }

            // Update chunk cache
            if (EngineSettings.WorldConfig.Infinite)
            {
                UpdateCache();
            }
        }

        private void InitCache()
        {
            m_clipmap.Init(ForceLOD, LODCoef);

            // Build a list of coordinates
            List<Vector2Int> chunksToLoad = new List<Vector2Int>();
            for (int z = -EngineSettings.WorldConfig.CachedRange; z <= EngineSettings.WorldConfig.CachedRange; ++z)
                for (int x = -EngineSettings.WorldConfig.CachedRange; x <= EngineSettings.WorldConfig.CachedRange; ++x)
                    chunksToLoad.Add(new Vector2Int(x, z));

            // Take the coordinates and sort them according to their distance from the center
            m_chunksToLoadByPos = chunksToLoad
                .Where(pos => Mathf.Abs(pos.X) + Mathf.Abs(pos.Z) < EngineSettings.WorldConfig.CachedRange*1.41f)
                .OrderBy(pos => Mathf.Abs(pos.X) + Mathf.Abs(pos.Z)) // Vectors with smallest magnitude first
                .ThenBy(pos => Mathf.Abs(pos.X)) // Beware cases like (-4,0) vs. (2,2). The second one is closer to the center
                .ThenBy(pos => Mathf.Abs(pos.Z))
                .ToArray();
        }

        private void UpdateCache()
        {
            foreach(var chunkPos in m_chunksToLoadByPos)
            {
                int xx = ViewerChunkPos.X + chunkPos.X;
                int zz = ViewerChunkPos.Z + chunkPos.Z;
                RegisterChunk(new Vector2Int(xx,zz));
            }
        }
        
        public void Shutdown()
        {
            if (EngineSettings.WorldConfig.Streaming)
            {
                UnregisterAll();

                // With streaming enabled, wait until all chunks are stored to disk before exiting
                while (!IsEmpty())
                {
                }
            }
        }

        /// <summary>
        ///     Perform a raycast against the map blocks
        /// </summary>
        public bool Raycast(Ray ray, float distance, out TileRaycastHit hit)
        {
            // break out direction vector
            float dx = ray.direction.x;
            float dy = ray.direction.y;
            float dz = ray.direction.z;

            // avoid infinite loop
            if (Mathf.Approximately(dx, 0f) &&
                Mathf.Approximately(dy, 0f) &&
                Mathf.Approximately(dz, 0f))
            {
                Assert.IsTrue(false, "Ray length must be greater than zero");
                hit = new TileRaycastHit();
                return false;
            }

            // block containing origin point
            int x = Helpers.FastFloor(ray.origin.x);
            int y = Helpers.FastFloor(ray.origin.y);
            int z = Helpers.FastFloor(ray.origin.z);

            // direction to increment x,y,z when stepping
            int stepX = Helpers.SigNum(dx);
            int stepY = Helpers.SigNum(dy);
            int stepZ = Helpers.SigNum(dz);

            float tMaxX = Helpers.IntBound(ray.origin.x, dx);
            float tMaxY = Helpers.IntBound(ray.origin.y, dy);
            float tMaxZ = Helpers.IntBound(ray.origin.z, dz);

            float tDeltaX = stepX/dx;
            float tDeltaY = stepY/dy;
            float tDeltaZ = stepZ/dz;

            Vector3 normal = new Vector3();

            distance /= Mathf.Sqrt(dx*dx+dy*dy+dz*dz);

            int worldMinX = ViewerChunkPos.X*EngineSettings.ChunkConfig.Size;
            int worldMaxX = worldMinX+(EngineSettings.WorldConfig.CachedRange*EngineSettings.ChunkConfig.Size);
            int worldMinZ = ViewerChunkPos.Z*EngineSettings.ChunkConfig.Size;
            int worldMaxZ = worldMinZ+(EngineSettings.WorldConfig.CachedRange*EngineSettings.ChunkConfig.Size);

            while ( // step is still inside world bounds
                (stepX>0 ? (x<worldMaxX) : x>=worldMinX) &&
                (stepY>0 ? (y<EngineSettings.ChunkConfig.SizeYTotal) : y>=0) &&
                (stepZ>0 ? (z<worldMaxZ) : z>=worldMinZ))
            {
                // tMaxX stores the t-value at which we cross a cube boundary along the
                // X axis, and similarly for Y and Z. Therefore, choosing the least tMax
                // chooses the closest cube boundary.
                if (tMaxX<tMaxY)
                {
                    if (tMaxX<tMaxZ)
                    {
                        if (tMaxX>distance)
                            break;

                        // Update which cube we are now in.
                        x += stepX;
                        // Adjust tMaxX to the next X-oriented boundary crossing.
                        tMaxX += tDeltaX;
                        // Record the normal vector of the cube face we entered.
                        normal[0] = -stepX;
                        normal[1] = 0;
                        normal[2] = 0;
                    }
                    else
                    {
                        if (tMaxZ>distance)
                            break;

                        z += stepZ;
                        tMaxZ += tDeltaZ;
                        normal[0] = 0;
                        normal[1] = 0;
                        normal[2] = -stepZ;
                    }
                }
                else
                {
                    if (tMaxY<tMaxZ)
                    {
                        if (tMaxY>distance)
                            break;

                        y += stepY;
                        tMaxY += tDeltaY;
                        normal[0] = 0;
                        normal[1] = -stepY;
                        normal[2] = 0;
                    }
                    else
                    {
                        // Identical to the second case, repeated for simplicity in the conditionals.
                        if (tMaxZ>distance)
                            break;

                        z += stepZ;
                        tMaxZ += tDeltaZ;
                        normal[0] = 0;
                        normal[1] = 0;
                        normal[2] = -stepZ;
                    }
                }

                if (GetBlock(x, y, z).IsEmpty())
                    continue;

                TileRaycastHit rayhit = new TileRaycastHit
				{
					HitBlock = new Vector3Int(x, y, z),
					HitFace = normal
				};

                hit = rayhit;
                return true;
            }

            hit = new TileRaycastHit();
            return false;
        }
        
        #endregion Public Methods

        private void OnDrawGizmosSelected()
        {
            if (m_chunks!=null)
            {
                foreach (ChunkController controller in m_chunks.Values)
                {
                    Chunk chunk = controller.Chunk;
                    if (chunk==null)
                        continue;

                    if (ShowGeomBounds && chunk.IsFinalized())
                    {
                        Gizmos.color = Color.white;
                        foreach (MiniChunk section in chunk.Sections)
                        {
                            if (!section.Visible || section.BBoxVertices.Count<=0)
                                continue;

                            Gizmos.DrawWireCube(section.GeometryBounds.center, section.GeometryBounds.size);
                        }
                    }

                    if (ShowMapBounds)
                    {
                        ChunkClipmapItem item = m_clipmap[chunk.Pos.X, chunk.Pos.Z];

                        if(!m_clipmap.IsInsideBounds(chunk.Pos.X, chunk.Pos.Z))
                        {
                            Gizmos.color = Color.red;
                            Gizmos.DrawWireCube(
                                new Vector3(
                                    chunk.Pos.X*EngineSettings.ChunkConfig.Size+EngineSettings.ChunkConfig.Size/2,
                                    EngineSettings.ChunkConfig.Size+0.1f,
                                    chunk.Pos.Z*EngineSettings.ChunkConfig.Size+EngineSettings.ChunkConfig.Size/2),
                                new Vector3(EngineSettings.ChunkConfig.Size-0.5f, 0,
                                            EngineSettings.ChunkConfig.Size-0.5f)
                                );
                        }
                        else if (item.IsWithinVisibleRange)
                        {
                            Gizmos.color = Color.green;
                            Gizmos.DrawWireCube(
                                new Vector3(
                                    chunk.Pos.X*EngineSettings.ChunkConfig.Size+EngineSettings.ChunkConfig.Size/2,
                                    EngineSettings.ChunkConfig.Size+0.15f,
                                    chunk.Pos.Z*EngineSettings.ChunkConfig.Size+EngineSettings.ChunkConfig.Size/2),
                                new Vector3(EngineSettings.ChunkConfig.Size-0.5f, 0,
                                            EngineSettings.ChunkConfig.Size-0.5f)
                                );
                        }
                        else// if (item.IsWithinCachedRange)
                        {
                            Gizmos.color = Color.yellow;
                            Gizmos.DrawWireCube(
                                new Vector3(
                                    chunk.Pos.X*EngineSettings.ChunkConfig.Size+EngineSettings.ChunkConfig.Size/2,
                                    EngineSettings.ChunkConfig.Size+0.15f,
                                    chunk.Pos.Z*EngineSettings.ChunkConfig.Size+EngineSettings.ChunkConfig.Size/2),
                                new Vector3(EngineSettings.ChunkConfig.Size-0.5f, 0,
                                            EngineSettings.ChunkConfig.Size-0.5f)
                                );
                        }
                    }
                }
            }

            if (ShowMapBounds)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(
                    new Vector3(ViewerChunkPos.X*EngineSettings.ChunkConfig.Size+EngineSettings.ChunkConfig.Size/2,
                                EngineSettings.ChunkConfig.Size+0.15f,
                                ViewerChunkPos.Z*EngineSettings.ChunkConfig.Size+EngineSettings.ChunkConfig.Size/2),
                    new Vector3((EngineSettings.WorldConfig.VisibleRange*2+1)*EngineSettings.ChunkConfig.Size, 0,
                                (EngineSettings.WorldConfig.VisibleRange*2+1)*EngineSettings.ChunkConfig.Size)
                    );

                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(
                    new Vector3(ViewerChunkPos.X*EngineSettings.ChunkConfig.Size+EngineSettings.ChunkConfig.Size/2,
                                EngineSettings.ChunkConfig.Size+0.15f,
                                ViewerChunkPos.Z*EngineSettings.ChunkConfig.Size+EngineSettings.ChunkConfig.Size/2),
                    new Vector3((EngineSettings.WorldConfig.CachedRange*2+1)*EngineSettings.ChunkConfig.Size, 0,
                                (EngineSettings.WorldConfig.CachedRange*2+1)*EngineSettings.ChunkConfig.Size)
                    );
            }
        }
    }
}