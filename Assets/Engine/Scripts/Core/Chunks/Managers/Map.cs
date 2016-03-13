using System.Collections.Generic;
using Assets.Engine.Scripts.Common;
using Assets.Engine.Scripts.Common.DataTypes;
using Assets.Engine.Scripts.Core.Blocks;
using Assets.Engine.Scripts.Physics;
using UnityEngine;
using UnityEngine.Assertions;
using System.Linq;
using Assets.Engine.Scripts.Builders.Geometry;
using Assets.Engine.Scripts.Common.Math;
using Assets.Engine.Scripts.Core.Threading;
using Assets.Engine.Scripts.Generators;
using Assets.Engine.Scripts.Rendering;

namespace Assets.Engine.Scripts.Core.Chunks
{
    public class Map: ChunkManager
    {
        #region Private vars

        //! Clipmap with precomputed static data useful for map processing
        private ChunkClipmap m_clipmap;
        
        private Plane[] m_cameraPlanes = new Plane[6];

        private Vector3Int[] m_chunksToLoadByPos;

        #endregion Private vars

        #region Public Fields

        public Camera Camera;
        public AChunkGenerator ChunkGenerator;
        public AVoxelGeometryBuilder MeshBuilder;
        
        //! Position of viewer in chunk coordinates
        public Vector3Int ViewerChunkPos { get; private set; }
        
        public OcclusionCuller Occlusion;
        
        // TODO: Make some custom drawer for these
        [HideInInspector]
        public int VisibleRange;
        [Header("Rendering distance")]
        public int CachedRange = 6;
        public int MinMapY = -6;
        public int MaxMapY = 6;
        //! If enabled, the world will be generated with camera acting as its center. Otherwise [0,0] will be the center
        public bool FollowCamera = false;

        [Header("Level of detail")]
        public float LODCoef = 1f;
        public int ForceLOD = -1;
        public bool FrustumCulling = true;

        [Header("Voxel dimensions")]
        //! Scale of voxel on X axis
        public int VoxelLogScaleX = 0;
        //! Scale of voxel on Y axis
        public int VoxelLogScaleY = 0;
        //! Scale of voxel on Z axis
        public int VoxelLogScaleZ = 0;

        [Header("Debugging")]
        public bool ShowGeomBounds;
        public bool ShowMapBounds;

        #endregion Public Fields

        #region ChunkManager overrides

        protected override void OnAwake()
        {
            VisibleRange = CachedRange-1;

            // Camera - set from the editor
            // ChunkGenerator - set from the editor

            m_clipmap = new ChunkClipmap(this, MinMapY, MaxMapY);
        }

        protected override void OnStart()
        {
            UpdateRangeRects();
            InitCache();
            UpdateCache();
        }

        #endregion Constructor

        #region Public Methods

        /// <summary>
        ///     Returns a block at a given world position
        /// </summary>
        public BlockData GetBlock(int wx, int wy, int wz)
        {
            int wxScaled = wx>>VoxelLogScaleX;
            int wyScaled = wy>>VoxelLogScaleY;
            int wzScaled = wz>>VoxelLogScaleZ;

            int cx = wxScaled>>EngineSettings.ChunkConfig.LogSize;
            int cy = wyScaled>>EngineSettings.ChunkConfig.LogSize;
            int cz = wzScaled>>EngineSettings.ChunkConfig.LogSize;

            Chunk chunk = GetChunk(cx, cy, cz);
            if (chunk==null)
                return BlockData.Air;

            int lx = wxScaled&EngineSettings.ChunkConfig.Mask;
            int ly = wyScaled&EngineSettings.ChunkConfig.Mask;
            int lz = wzScaled&EngineSettings.ChunkConfig.Mask;

            return chunk[lx, ly, lz];
        }

        /// <summary>
        ///     Set the block at the world position
        /// </summary>
        public void SetBlock(BlockData block, int wx, int wy, int wz)
        {
            int wxScaled = wx>>VoxelLogScaleX;
            int wyScaled = wy>>VoxelLogScaleY;
            int wzScaled = wz>>VoxelLogScaleZ;

            int cx = wxScaled>>EngineSettings.ChunkConfig.LogSize;
            int cy = wyScaled>>EngineSettings.ChunkConfig.LogSize;
            int cz = wzScaled>>EngineSettings.ChunkConfig.LogSize;

            Chunk chunk = GetChunk(cx, cy, cz);
            if (chunk==null)
                return;

            int lx = wxScaled&EngineSettings.ChunkConfig.Mask;
            int ly = wyScaled&EngineSettings.ChunkConfig.Mask;
            int lz = wzScaled&EngineSettings.ChunkConfig.Mask;

			chunk.ModifyBlock(lx, ly, lz, block);
        }
        
        private bool IsChunkInViewFrustum(Chunk chunk)
        {
            // Check if the chunk lies within camera planes
#if DEBUG
            return !FrustumCulling || chunk.CheckFrustum(m_cameraPlanes);
#else
            return chunk.CheckFrustum(m_cameraPlanes);
#endif
        }

        private void UpdateRangeRects()
        {
            // Update camera position
            int posX = Mathf.FloorToInt(Camera.transform.position.x) >> EngineSettings.ChunkConfig.LogSize >> VoxelLogScaleX;
            int posY = Mathf.FloorToInt(Camera.transform.position.y) >> EngineSettings.ChunkConfig.LogSize >> VoxelLogScaleY;
            int posZ = Mathf.FloorToInt(Camera.transform.position.z) >> EngineSettings.ChunkConfig.LogSize >> VoxelLogScaleZ;
            ViewerChunkPos = new Vector3Int(posX, FollowCamera ? posY : 0, posZ);

            // Update clipmap offset
            m_clipmap.SetOffset(ViewerChunkPos.X, ViewerChunkPos.Y, ViewerChunkPos.Z);

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
                ChunkClipmapItem item = m_clipmap[chunk.Pos.X, chunk.Pos.Y, chunk.Pos.Z];

                // Chunk is too far away. Remove it
                if (!m_clipmap.IsInsideBounds(chunk.Pos.X, chunk.Pos.Y, chunk.Pos.Z))
                {
                    removeChunk = true;
                }
                // Chunk is within visibilty range. Full update with geometry generation is possible
                else if (item.IsWithinVisibleRange)
                {
                    chunk.LOD = item.LOD;
                    chunk.SetPossiblyVisible(true);

                    // If occlusion culling is enabled we need to register it
                    if (EngineSettings.CoreConfig.OcclusionCulling && Occlusion!=null)
                    {
                        chunk.Visible = false;
                        if (chunk.IsFinalized())
                            Occlusion.RegisterEntity(chunk);
                    }
                    else
                        chunk.SetVisible(true);
                }
                // Chunk is within cached range. Full update except for geometry generation
                else// if (item.IsWithinCachedRange)
                {
                    chunk.LOD = item.LOD;
                    chunk.SetPossiblyVisible(false);
                    chunk.SetVisible(false);
                }
            }
            else
            {
                ChunkClipmapItem item = m_clipmap[chunk.Pos.X, chunk.Pos.Y, chunk.Pos.Z];

                // Chunk is not visible and too far away. Remote it
                if (!m_clipmap.IsInsideBounds(chunk.Pos.X, chunk.Pos.Y, chunk.Pos.Z))
                {
                    removeChunk = true;
                }
                // Chunk is not in viewfrustum but still within cached range
                else if (item.IsWithinCachedRange)
                {
                    chunk.LOD = item.LOD;
                    chunk.SetPossiblyVisible(false);
                    chunk.SetVisible(false);
                }
            }

            if (removeChunk)
                chunk.Finish();
        }

        protected override void OnPostProcessChunks()
        {
            // Perform occlussion culling
            if (EngineSettings.CoreConfig.OcclusionCulling && Occlusion!=null)
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
            List<Vector3Int> chunksToLoad = new List<Vector3Int>();
            for(int y= m_clipmap.RangeYMin; y<= m_clipmap.RangeYMax; ++y)
                for (int z = -CachedRange; z <= CachedRange; ++z)
                    for (int x = -CachedRange; x <= CachedRange; ++x)
                        chunksToLoad.Add(new Vector3Int(x, y, z));

            // Center y around zero
            int yRange = Mathf.Abs(m_clipmap.RangeYMin)+Mathf.Abs(m_clipmap.RangeYMax) / 2;

            // Take the coordinates and sort them according to their distance from the center
            m_chunksToLoadByPos = chunksToLoad
                // Load as sphere
                .Where(
                    pos =>
                    Mathf.Abs(pos.Y)<1.41f*yRange &&
                    Mathf.Abs(pos.X)+Mathf.Abs(pos.Z)<1.41f*CachedRange
                )
                // Vectors with smallest magnitude first
                .OrderBy(pos => pos.X*pos.X+pos.Y*pos.Y+pos.Z*pos.Z)
                // Beware cases like (-4,0) vs. (2,2). The second one is closer to the center
                .ThenBy(pos => Mathf.Abs(pos.Y))
                .ThenBy(pos => Mathf.Abs(pos.X))
                .ThenBy(pos => Mathf.Abs(pos.Z))
                .ToArray();
        }

        private void UpdateCache()
        {
#if DEBUG
            // Make it possible to see results in real-time
            m_clipmap.Init(ForceLOD, LODCoef);
#endif

            // Register new chunks in chunk manager
            foreach (var chunkPos in m_chunksToLoadByPos)
            {
                int xx = ViewerChunkPos.X+chunkPos.X;
                int yy = ViewerChunkPos.Y+chunkPos.Y;
                int zz = ViewerChunkPos.Z+chunkPos.Z;
                    
                RegisterChunk(new Vector3Int(xx, yy, zz));
            }

            // Commit collected work items
            WorkPoolManager.Commit();
            IOPoolManager.Commit();
        }
        
        public void Shutdown()
        {
            if (EngineSettings.WorldConfig.Streaming)
            {
                UnregisterAll();

                // With streaming enabled, wait until all chunks are stored to disk before exiting
                while (!IsEmpty)
                {
                    ProcessChunks();
                }
            }
        }

        /// <summary>
        ///     Perform a raycast against the map blocks
        /// </summary>
        public bool Raycast(Ray ray, float distance, out TileRaycastHit hit)
        {
            // Direction vector
            float dx = ray.direction.x;
            float dy = ray.direction.y;
            float dz = ray.direction.z;

            // Avoid infinite loop - early exit if ray size is too small
            if (Mathf.Approximately(dx, 0f) &&
                Mathf.Approximately(dy, 0f) &&
                Mathf.Approximately(dz, 0f))
            {
                Assert.IsTrue(false, "Ray length must be greater than zero");
                hit = new TileRaycastHit();
                return false;
            }
            
            // Block containing origin point. Round it according to VoxelLogScale
            int wx = (Helpers.FastFloor(ray.origin.x) >> VoxelLogScaleX) << VoxelLogScaleX;
            int wy = (Helpers.FastFloor(ray.origin.y) >> VoxelLogScaleY) << VoxelLogScaleY;
            int wz = (Helpers.FastFloor(ray.origin.z) >> VoxelLogScaleZ) << VoxelLogScaleZ;

            // Direction when stepping
            int nx = Helpers.SigNum(dx);
            int ny = Helpers.SigNum(dy);
            int nz = Helpers.SigNum(dz);

            //Amount by which to increment x,y,z when stepping
            int stepX = nx << VoxelLogScaleX;
            int stepY = ny << VoxelLogScaleY;
            int stepZ = nz << VoxelLogScaleZ;

            // Initial values depend on the fractional part of the origin
            float tMaxX = Helpers.IntBound(wx, dx);
            float tMaxY = Helpers.IntBound(wy, dy);
            float tMaxZ = Helpers.IntBound(wz, dz);

            // The change in t when taking a step
            float tDeltaX = stepX/dx;
            float tDeltaY = stepY/dy;
            float tDeltaZ = stepZ/dz;

            // Rescale from units of 1 cube-edge to units of direction so we can compare with t
            distance /= Mathf.Sqrt(dx*dx+dy*dy+dz*dz);

            int worldMinX = ViewerChunkPos.X<<EngineSettings.ChunkConfig.LogSize<<VoxelLogScaleX;
            int worldMaxX = worldMinX+(CachedRange<<EngineSettings.ChunkConfig.LogSize<<VoxelLogScaleX);
            int worldMinY = ViewerChunkPos.Y<<EngineSettings.ChunkConfig.LogSize<<VoxelLogScaleY;
            int worldMaxY = worldMinY+(CachedRange<<EngineSettings.ChunkConfig.LogSize<<VoxelLogScaleY);
            int worldMinZ = ViewerChunkPos.Z<<EngineSettings.ChunkConfig.LogSize<<VoxelLogScaleZ;
            int worldMaxZ = worldMinZ+(CachedRange<<EngineSettings.ChunkConfig.LogSize<<VoxelLogScaleZ);

            while ( // step is still inside world bounds
                (stepX>0 ? wx<worldMaxX : wx>=worldMinX) &&
                (stepY>0 ? wy<worldMaxY : wy>=worldMinY) &&
                (stepZ>0 ? wz<worldMaxZ : wz>=worldMinZ))
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
                        
                        wx += stepX;
                        tMaxX += tDeltaX;
                    }
                    else
                    {
                        if (tMaxZ>distance)
                            break;

                        wz += stepZ;
                        tMaxZ += tDeltaZ;
                    }
                }
                else
                {
                    if (tMaxY<tMaxZ)
                    {
                        if (tMaxY>distance)
                            break;

                        wy += stepY;
                        tMaxY += tDeltaY;
                    }
                    else
                    {
                        if (tMaxZ>distance)
                            break;

                        wz += stepZ;
                        tMaxZ += tDeltaZ;
                    }
                }

                if (GetBlock(wx, wy, wz).IsEmpty())
                    continue;

                TileRaycastHit rayhit = new TileRaycastHit
				{
					HitBlock = new Vector3Int(wx, wy, wz)
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
                foreach (Chunk chunk in m_chunks.Values)
                {
                    if (ShowGeomBounds && chunk.IsFinalized())
                    {
                        Gizmos.color = Color.white;

                        if (chunk.Visible && chunk.BBoxVertices.Count>0)
                            Gizmos.DrawWireCube(chunk.GeometryBounds.center, chunk.GeometryBounds.size);
                    }

                    if (ShowMapBounds)
                    {
                        ChunkClipmapItem item = m_clipmap[chunk.Pos.X, chunk.Pos.Y, chunk.Pos.Z];

                        int sizeX = EngineSettings.ChunkConfig.Size << VoxelLogScaleX;
                        int sizeY = EngineSettings.ChunkConfig.Size << VoxelLogScaleY;
                        int sizeZ = EngineSettings.ChunkConfig.Size << VoxelLogScaleZ;

                        if (!m_clipmap.IsInsideBounds(chunk.Pos.X, chunk.Pos.Y, chunk.Pos.Z))
                        {
                            Gizmos.color = Color.red;
                            Gizmos.DrawWireCube(
                                new Vector3(
                                    chunk.Pos.X*sizeX+sizeX/2,
                                    chunk.Pos.Y*sizeY+sizeY/2+0.05f,
                                    chunk.Pos.Z*sizeZ+sizeZ/2),
                                new Vector3(sizeX-0.05f, 0, sizeZ-0.05f)
                                );
                        }
                        else if (item.IsWithinVisibleRange)
                        {
                            Gizmos.color = Color.green;
                            Gizmos.DrawWireCube(
                                new Vector3(
                                    chunk.Pos.X*sizeX+sizeX/2,
                                    chunk.Pos.Y*sizeY+sizeY/2+0.05f,
                                    chunk.Pos.Z*sizeZ+sizeZ/2),
                                new Vector3(sizeX-0.05f, 0, sizeZ-0.05f)
                                );
                        }
                        else// if (item.IsWithinCachedRange)
                        {
                            Gizmos.color = Color.yellow;
                            Gizmos.DrawWireCube(
                                new Vector3(
                                    chunk.Pos.X*sizeX+sizeX/2,
                                    chunk.Pos.Y*sizeY+sizeY/2+0.05f,
                                    chunk.Pos.Z*sizeZ+sizeZ/2),
                                new Vector3(sizeX-0.05f, 0, sizeZ-0.05f)
                                );
                        }
                    }
                }
            }
        }
    }
}