using System.Collections.Generic;
using Assets.Engine.Scripts.Common;
using Assets.Engine.Scripts.Common.DataTypes;
using Assets.Engine.Scripts.Core.Blocks;
using Assets.Engine.Scripts.Core.Chunks;
using Assets.Engine.Scripts.Physics;
using Assets.Engine.Scripts.Provider;
using UnityEngine;
using UnityEngine.Assertions;
using System.Linq;
using Assets.Engine.Scripts.Common.Math;
using Assets.Engine.Scripts.Core.Threading;
using Assets.Engine.Scripts.Rendering;

namespace Assets.Engine.Scripts.Core
{
    public class Map: MonoBehaviour
    {
        #region Static Fields

        //! The local instance of the Map.
        public static Map Current;        

        #endregion Static Fields

        #region Private vars

        //private BlockStorage m_blocks;
        private ChunkStorage m_chunks;

        //! Chunks to be removed
        private List<Chunk> m_chunksToRemove;
        
        private Rect m_viewRange;
        private Rect m_cachedRange;

        private Camera m_camera;
        private Plane[] m_cameraPlanes = new Plane[6];

        private Vector2Int[] m_chunksToLoadByPos;

        #endregion Private vars

        #region Constructor

        public void Awake()
        {
            //m_blocks = new BlockStorage();
            m_chunks = new ChunkStorage();
            m_chunksToRemove = new List<Chunk>();
        }

        public void Start()
        {
            GameObject cameraGo = GameObject.FindGameObjectWithTag("MainCamera");
            m_camera = cameraGo.GetComponent<Camera>();
            
            UpdateRangeRects();
            InitCache();
            UpdateCache();
        }

        #endregion Constructor

        #region Public Fields

        public IChunkProvider ChunkProvider;
        
        // Position of viewer in chunk coordinates
        public Vector2Int ViewerChunkPos;

        public OcclusionCuller Occlusion;

        public float LODCoef = 1f;
        public int ForceLOD = -1;

        [Header("Debugging")]
        public bool ShowGeomBounds;
        public bool ShowMapBounds;

        #endregion Public Fields

        #region Public Methods

        /// <summary>
        ///     Gets the chunk at the given chunk coordinates.
        /// </summary>
        public Chunk GetChunk(int cx, int cz)
        {
            Chunk chunk = m_chunks.Check(cx, cz) ? m_chunks[cx, cz] : null;
            return chunk;
        }

        /// <summary>
        ///     Get the block at the world position
        /// </summary>
        public BlockData GetBlock(int wx, int wy, int wz)
        {
            if (wy<0 || wy>=EngineSettings.ChunkConfig.SizeYTotal)
                return BlockData.Air;

            int cx = wx>>EngineSettings.ChunkConfig.LogSize;
            int cz = wz>>EngineSettings.ChunkConfig.LogSize;

            Chunk chunk = m_chunks[cx, cz];
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

        public int DetermineLOD(int cx, int cz)
        {
            int lod = 0;

            if (ForceLOD>=0)
            {
                lod = ForceLOD;
            }
            else
            {
                if (LODCoef <= 0)
                    return 0;

                int xDist = Mathf.Abs(cx-ViewerChunkPos.X);
                int zDist = Mathf.Abs(cz-ViewerChunkPos.Z);

                // Pick the greater distance and choose a proper LOD
                int dist = Mathf.Max(xDist, zDist);
                lod = (int)(dist/(LODCoef*EngineSettings.ChunkConfig.LogSize));
            }

            // LOD can't be bigger than chunk size
            if (lod<0)
                lod = 0;
            if (lod>EngineSettings.ChunkConfig.LogSize)
                lod = EngineSettings.ChunkConfig.LogSize;

            return lod;
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

        private bool IsWithinVisibilityRange(Chunk chunk)
        {
            return chunk.Pos.X>=m_viewRange.xMin && chunk.Pos.Z>=m_viewRange.yMin && chunk.Pos.X<=m_viewRange.xMax && chunk.Pos.Z<=m_viewRange.yMax;
        }

        private bool IsWithinCachedRange(Chunk chunk)
        {
            return chunk.Pos.X>=m_cachedRange.xMin && chunk.Pos.Z>=m_cachedRange.yMin && chunk.Pos.X<=m_cachedRange.xMax && chunk.Pos.Z<=m_cachedRange.yMax;
        }

        private void UpdateRangeRects()
        {
            Geometry.CalculateFrustumPlanes(m_camera, ref m_cameraPlanes);

            m_viewRange = new Rect(
                ViewerChunkPos.X-EngineSettings.WorldConfig.VisibleRange,
                ViewerChunkPos.Z-EngineSettings.WorldConfig.VisibleRange,
                EngineSettings.WorldConfig.VisibleRange*2,
                EngineSettings.WorldConfig.VisibleRange*2
                );

            m_cachedRange = new Rect(
                ViewerChunkPos.X-EngineSettings.WorldConfig.CachedRange,
                ViewerChunkPos.Z-EngineSettings.WorldConfig.CachedRange,
                EngineSettings.WorldConfig.CachedRange*2,
                EngineSettings.WorldConfig.CachedRange*2
                );
        }

        public static int ChunkCnt = 0;
        
        private void UpdateChunks()
        {
            // Limit the amount of chunks per update. This is necessary due to the GC.
            // Creating too many chunks per frame would result in many allocations
            // Until I do not figure out how to handle mesh generation better, this is a necessity
            // !TODO Make this more intelligent. E.g., make this depend on visible world size so
            // !TODO it does not take too long before chunks are displayed on the screen
            //int chunksReadyToBatch = 0;
            
            // Process loaded chunks
            int cnt = 0;
            foreach (Chunk chunk in m_chunks.Values)
            {
                cnt++;

                bool removeChunk = false;

                // Chunk is within view frustum
                if (IsChunkInViewFrustum(chunk))
                {
                    // Chunk is within visibilty range. Full update with geometry generation is possible
                    if (IsWithinVisibilityRange(chunk))
                    {
                        chunk.LOD = DetermineLOD(chunk.Pos.X, chunk.Pos.Z);
                        chunk.SetPossiblyVisible(true);
                        chunk.Restore();
                        chunk.UpdateChunk();

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
                    else if (IsWithinCachedRange(chunk))
                    {
                        chunk.LOD = DetermineLOD(chunk.Pos.X, chunk.Pos.Z);
                        chunk.SetPossiblyVisible(false);
                        chunk.SetVisible(false);
                        chunk.Restore();
                        chunk.UpdateChunk();
                    }
                    else
                    // Chunk is too far away. Remove it
                    {
                        removeChunk = true;
                    }
                }
                else
                {
                    // Chunk is not in viewfrustum but still within cached range
                    if (IsWithinCachedRange(chunk))
                    {
                        chunk.LOD = DetermineLOD(chunk.Pos.X, chunk.Pos.Z);
                        chunk.SetPossiblyVisible(false);
                        chunk.SetVisible(false);
                        chunk.Restore();
                        chunk.UpdateChunk();
                    }
                    else
                    // Chunk is not visible and too far away. Remote it
                    {
                        removeChunk = true;
                    }
                }

                // Make an attempt to unload the chunk
                if (removeChunk && EngineSettings.WorldConfig.Infinite && chunk.Finish())
                {
                    m_chunksToRemove.Add(chunk);
                }
            }
            ChunkCnt = cnt;

            // Commit collected work items
            WorkPoolManager.Commit();
            IOPoolManager.Commit();

            #region Perform occlusion culling

            if (EngineSettings.WorldConfig.OcclusionCulling && Occlusion!=null)
            {
                Occlusion.PerformOcclusion();
            }

            #endregion
            
            #region Remove unused chunks

            for (int i = 0; i<m_chunksToRemove.Count; i++)
            {
                var chunk = m_chunksToRemove[i];

                // Now that all work is finished, release the chunk
                ReleaseChunk(chunk);
            }
            m_chunksToRemove.Clear();

            #endregion

        }

        private void InitCache()
        {
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
            int offsetX = ViewerChunkPos.X;
            int offsetZ = ViewerChunkPos.Z;

            foreach(var chunkPos in m_chunksToLoadByPos)
            {
                int xx = offsetX+chunkPos.X;
                int zz = offsetZ+chunkPos.Z;

                if (m_chunks.Check(xx, zz))
                    continue;

                Chunk chunk = ChunkProvider.RequestChunk(xx, zz);
                m_chunks[chunk.Pos.X, chunk.Pos.Z] = chunk;
            }

			// Commit collected work items
            WorkPoolManager.Commit();
            IOPoolManager.Commit();
        }
        
        public void UpdateMap()
        {
            if (EngineSettings.WorldConfig.Infinite)
            {
                UpdateRangeRects();
                UpdateChunks();
                UpdateCache();
            }
            else
            {
                UpdateChunks();
            }
        }

        private void ReleaseChunk(Chunk chunk)
        {
            int cx = chunk.Pos.X;
            int cz = chunk.Pos.Z;

            // Return out chunk back to object pool
            ChunkProvider.ReleaseChunk(chunk);

            // Invalidate the chunk
            m_chunks.Remove(cx, cz);
        }
        
        public void Shutdown()
        {
            if (EngineSettings.WorldConfig.Streaming)
            {
                // With streaming enabled, wait until all chunks are stored to disk before exit
                while (true)
                {
                    int cnt = 0;

                    // Process loaded chunks
                    foreach (Chunk chunk in m_chunks.Values)
                    {
                        ++cnt;
                        if (chunk.Finish())
                            m_chunksToRemove.Add(chunk);
                    }

                    // Wait for chunks
                    if (cnt==0)
                        break;

					// Commit collected work items
                    WorkPoolManager.Commit();
                    IOPoolManager.Commit();

                    // Release chunks which finished their work
                    for (int i = 0; i<m_chunksToRemove.Count; i++)
                    {
                        var chunk = m_chunksToRemove[i];
                        ReleaseChunk(chunk);
                    }
                    m_chunksToRemove.Clear();
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
                foreach (Chunk chunk in m_chunks.Values)
                {
                    if (chunk==null)
                        continue;
                    
                    if (ShowGeomBounds && chunk.IsFinalized())
                    {
                        Gizmos.color = Color.white;
                        foreach (MiniChunk section in chunk.Sections)
                        {
                            if (section.BBoxVertices.Count<=0)
                                continue;

                            Gizmos.DrawWireCube(section.GeometryBounds.center, section.GeometryBounds.size);
                        }
                    }

                    if (ShowMapBounds)
                    {
                        if (IsWithinVisibilityRange(chunk))
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
                        else if (IsWithinCachedRange(chunk))
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
                        else
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