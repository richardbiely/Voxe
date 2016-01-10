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
        
        //! Chunks to be rendered
        private List<Chunk> m_chunksToRender;
        //! Chunks to be removed
        private List<Chunk> m_chunksToRemove;

        private struct Visibility
        {
            public Chunk Chunk;
            public MiniChunk Section;
            public float Distance;

            public Visibility(Chunk chunk, MiniChunk section, float distance)
            {
                Chunk = chunk;
                Section = section;
                Distance = distance;
            }
        }

        private const int RasterSize = 128;
        private Rasterizer m_rasterizer;

        private Rect m_viewRange;
        private Rect m_cachedRange;

        private Camera m_camera;
        private Plane[] m_cameraPlanes;

        private Vector2Int[] m_chunksToLoadByPos;

        #endregion Private vars

        #region Constructor

        public void Awake()
        {
            //m_blocks = new BlockStorage();
            m_chunks = new ChunkStorage();
            m_chunksToRender = new List<Chunk>();
            m_chunksToRemove = new List<Chunk>();
        }

        public void Start()
        {
            GameObject cameraGo = GameObject.FindGameObjectWithTag("MainCamera");
            m_camera = cameraGo.GetComponent<Camera>();

            m_rasterizer = new Rasterizer(m_camera, RasterSize);
            
            UpdateRangeRects();
            InitCache();
            UpdateCache();
        }

        #endregion Constructor

        #region Public Fields

        public IChunkProvider ChunkProvider;

        // Position of viewer in chunk coordinates
        public Vector2Int ViewerChunkPos;

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

            int cx = wx>>EngineSettings.ChunkConfig.LogSizeX;
            int cz = wz>>EngineSettings.ChunkConfig.LogSizeZ;

            Chunk chunk = m_chunks[cx, cz];
            if (chunk==null)
                return BlockData.Air;

            int lx = wx&EngineSettings.ChunkConfig.MaskX;
            int lz = wz&EngineSettings.ChunkConfig.MaskZ;

            return chunk[lx, wy, lz];
        }

        /// <summary>
        ///     Damage the block at the world position
        /// </summary>
        public void DamageBlock(int wx, int wy, int wz, int damage)
        {
            if (wy<0 || wy>=EngineSettings.ChunkConfig.SizeYTotal)
                return;

            int cx = wx>>EngineSettings.ChunkConfig.LogSizeX;
            int cz = wz>>EngineSettings.ChunkConfig.LogSizeZ;

            Chunk chunk = GetChunk(cx, cz);
            if (chunk==null)
                return;

            int lx = wx&EngineSettings.ChunkConfig.MaskX;
            int lz = wz&EngineSettings.ChunkConfig.MaskZ;

            chunk.DamageBlock(lx, wy, lz, damage);
        }

        /// <summary>
        ///     Set the block at the world position
        /// </summary>
        public void SetBlock(BlockData block, int wx, int wy, int wz)
        {
            if (wy<0 || wy>=EngineSettings.ChunkConfig.SizeYTotal)
                return;

            int cx = wx>>EngineSettings.ChunkConfig.LogSizeX;
            int cz = wz>>EngineSettings.ChunkConfig.LogSizeZ;

            Chunk chunk = GetChunk(cx, cz);
            if (chunk==null)
                return;

            int lx = wx&EngineSettings.ChunkConfig.MaskX;
            int lz = wz&EngineSettings.ChunkConfig.MaskZ;

			chunk.ModifyBlock(lx, wy, lz, block);
        }

        // We'll only allow a certain amount of chunks to be created per update
        // Currently, this number is derived from the visible world size divided by the number of updates per second
        // !TODO: Let this value large for now. Change it back / adjust it later
        //private static readonly int MaxChunksPerUpdate = 10000;//(EngineSettings.WorldConfig.VisibleRange * EngineSettings.WorldConfig.VisibleRange / 20);

        private bool IsWithinVisibilityRange(Chunk chunk)
        {
            // Chunk is close enough
            Vector2Int min = new Vector2Int(ViewerChunkPos.X - EngineSettings.WorldConfig.VisibleRange,
                                            ViewerChunkPos.Z - EngineSettings.WorldConfig.VisibleRange);
            Vector2Int max = new Vector2Int(ViewerChunkPos.X + EngineSettings.WorldConfig.VisibleRange,
                                            ViewerChunkPos.Z + EngineSettings.WorldConfig.VisibleRange);

            if (chunk.Pos.X>=min.X && chunk.Pos.Z>=min.Z && chunk.Pos.X<=max.X && chunk.Pos.Z<=max.Z)
            {
                // Check if the chunk lies within camera planes
                return chunk.CheckFrustum(m_cameraPlanes);
            }

            return false;
        }

        private bool IsWithinCachedRange(Chunk chunk)
        {
            // Chunk is close enough
            Vector2Int min = new Vector2Int(ViewerChunkPos.X - EngineSettings.WorldConfig.CachedRange,
                                            ViewerChunkPos.Z - EngineSettings.WorldConfig.CachedRange);
            Vector2Int max = new Vector2Int(ViewerChunkPos.X + EngineSettings.WorldConfig.CachedRange,
                                            ViewerChunkPos.Z + EngineSettings.WorldConfig.CachedRange);

            return (chunk.Pos.X>=min.X && chunk.Pos.Z>=min.Z && chunk.Pos.X<=max.X && chunk.Pos.Z<=max.Z);
        }

        private void UpdateRangeRects()
        {
            m_cameraPlanes = GeometryUtility.CalculateFrustumPlanes(m_camera);

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
                // Chunk within visibilty range. Full update with geometry generation is possible
                if (IsWithinVisibilityRange(chunk))
                {
                    chunk.SetPossiblyVisible(true);
                    chunk.SetVisible(!EngineSettings.WorldConfig.OcclusionCulling);
                    chunk.Restore();
                    chunk.UpdateChunk();

                    // If occlusion culling is enabled we need to pass bounding box data to rasterizer
                    if (EngineSettings.WorldConfig.OcclusionCulling && chunk.IsFinalized())
                    {
                        foreach (MiniChunk section in chunk.Sections)
                        {
                            if (section.BoundingMeshBuffer.Count<=0)
                                continue;

                            m_rasterizer.Add(section.BoundingMeshBuffer);
                            m_chunksToRender.Add(chunk);
                        }
                    }
                }
                // Chunk within cached range. Full update except for geometry generation
                else if (IsWithinCachedRange(chunk))
                {
                    chunk.SetPossiblyVisible(false);
                    chunk.SetVisible(false);
                    chunk.Restore();
                    chunk.UpdateChunk();               
                }
                // Make an attempt to unload the chunk
                else if (EngineSettings.WorldConfig.Infinite && chunk.Finish())
                {
                    m_chunksToRemove.Add(chunk);
                }
            }
            ChunkCnt = cnt;

            // Commit collected work items
            WorkPoolManager.Commit();
            IOPoolManager.Commit();

            #region Perform occlusion culling

            if (EngineSettings.WorldConfig.OcclusionCulling)
            {
                RasterizeChunks();
                OcclusionCulling();
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

        private void RasterizeChunks()
        {
            Assert.IsTrue(EngineSettings.WorldConfig.OcclusionCulling);

            if (m_chunksToRender.Count > 0)
                m_rasterizer.Rasterize();
        }

        private void OcclusionCulling()
        {
            Assert.IsTrue(EngineSettings.WorldConfig.OcclusionCulling);

            float xx = (1f/Screen.width)*RasterSize;
            float yy = (1f/Screen.height)*RasterSize;

            for (int i = 0; i < m_chunksToRender.Count; i++)
            {
                Chunk chunk = m_chunksToRender[i];
                foreach (MiniChunk section in chunk.Sections)
                {
                    List<Vector3> vertices = section.BoundingMeshBuffer;
                    if (vertices.Count <= 0)
                    {
                        section.SetVisible(false);
                        continue;
                    }

                    float sectionDepth = float.MaxValue;
                    int pos = -1;

                    for (int j = 0; j < vertices.Count; j += 4)
                    {
                        // Vertices converted to screen space
                        Vector3[] verticesOnScreen =
                        {
                            m_camera.WorldToScreenPoint(vertices[j]),
                            m_camera.WorldToScreenPoint(vertices[j+1]),
                            m_camera.WorldToScreenPoint(vertices[j+2]),
                            m_camera.WorldToScreenPoint(vertices[j+3])
                        };

                        // Find the vertex closest to the camera
                        for (int v = 0; v < verticesOnScreen.Length; v++)
                        {
                            if (verticesOnScreen[v].z < sectionDepth)
                            {
                                sectionDepth = verticesOnScreen[v].z;
                                pos = j + v;
                            }
                        }
                    }

                    // Vertex converted to screen space
                    Vector3 closestVert = m_camera.WorldToViewportPoint(vertices[pos]);

                    // Vertex converted to depth buffer space
                    int x = (int)((closestVert.x-0.5f)*xx);
                    int y = (int)((closestVert.y-0.5f)*yy);

                    x = Mathf.Max(0, Mathf.Min(x, RasterSize-1));
                    y = Mathf.Max(0, Mathf.Min(y, RasterSize-1));

                    // Section hidden behind raster is hidden
                    float rasterDepth = m_rasterizer.GetDepth(x, y);
                    section.SetVisible(!(rasterDepth<sectionDepth));
                }
            }

            m_chunksToRender.Clear();
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
                //.Where(pos => Mathf.Abs(pos.X) + Mathf.Abs(pos.Z) < EngineSettings.WorldConfig.CachedRange*1.41f)
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
            // block containing origin point
            int x = Mathf.FloorToInt(ray.origin.x);
            int y = Mathf.FloorToInt(ray.origin.y);
            int z = Mathf.FloorToInt(ray.origin.z);

            // break out direction vector
            float dx = ray.direction.x;
            float dy = ray.direction.y;
            float dz = ray.direction.z;

            // direction to increment x,y,z when stepping
            int stepX = Helpers.Signum(dx);
            int stepY = Helpers.Signum(dy);
            int stepZ = Helpers.Signum(dz);

            float tMaxX = Helpers.IntBound(ray.origin.x, dx);
            float tMaxY = Helpers.IntBound(ray.origin.y, dy);
            float tMaxZ = Helpers.IntBound(ray.origin.z, dz);

            float tDeltaX = stepX/dx;
            float tDeltaY = stepY/dy;
            float tDeltaZ = stepZ/dz;

            // avoid infinite loop
            if (Mathf.Approximately(dx, 0f) &&
                Mathf.Approximately(dy, 0f) &&
                Mathf.Approximately(dz, 0f))
            {
                Assert.IsTrue(false, "Ray length must be greater than zero");
                hit = new TileRaycastHit();
                return false;
            }

            Vector3 normal = new Vector3();

            distance /= Mathf.Sqrt(dx*dx+dy*dy+dz*dz);

            int worldMinX = ViewerChunkPos.X*EngineSettings.ChunkConfig.SizeX;
            int worldMaxX = worldMinX+(EngineSettings.WorldConfig.CachedRange*EngineSettings.ChunkConfig.SizeX);
            int worldMinZ = ViewerChunkPos.Z*EngineSettings.ChunkConfig.SizeZ;
            int worldMaxZ = worldMinZ+(EngineSettings.WorldConfig.CachedRange*EngineSettings.ChunkConfig.SizeZ);

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

        // test the blocks in the world against the given AABB
        public bool TestBlocksAABB(Bounds bounds, int radius)
        {
            if (radius<0 || radius>EngineSettings.WorldConfig.CachedRange)
                radius = EngineSettings.WorldConfig.CachedRange;

            int min = (EngineSettings.WorldConfig.CachedRange-radius)>>1;
            int max = min+radius;

            int offsetX = ViewerChunkPos.X;
            int offsetZ = ViewerChunkPos.Z;

            for (int z = min; z<max; z++)
            {
                int realZ = z+offsetZ;

                for (int x = min; x<max; x++)
                {
                    int realX = x+offsetX;

                    Chunk chunk = m_chunks[realX, realZ];
                    if (chunk==null)
                        continue;

                    if (chunk.Intersects(bounds))
                        return true;
                }
            }

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

                    if (!chunk.IsFinalized())
                        continue;

                    #if DEBUG
                    foreach (MiniChunk section in chunk.Sections)
                    {
                        if (section.BoundingMeshBuffer.Count<=0)
                            continue;

                        Gizmos.DrawWireCube(section.Bounds.center, section.Bounds.size);
                    }
                    #endif

                    if (IsWithinVisibilityRange(chunk))
                    {
                        Gizmos.color = Color.green;
                        Gizmos.DrawWireCube(
                            new Vector3(
                                chunk.Pos.X*EngineSettings.ChunkConfig.SizeX+EngineSettings.ChunkConfig.SizeX/2,
                                EngineSettings.ChunkConfig.SizeY+0.15f,
                                chunk.Pos.Z*EngineSettings.ChunkConfig.SizeZ+EngineSettings.ChunkConfig.SizeZ/2),
                            new Vector3(EngineSettings.ChunkConfig.SizeX-0.5f, 0,
                                        EngineSettings.ChunkConfig.SizeZ-0.5f)
                            );
                    }
                    else if(IsWithinCachedRange(chunk))
                    {
                        Gizmos.color = Color.yellow;
                        Gizmos.DrawWireCube(
                            new Vector3(
                                chunk.Pos.X*EngineSettings.ChunkConfig.SizeX+EngineSettings.ChunkConfig.SizeX/2,
                                EngineSettings.ChunkConfig.SizeY+0.15f,
                                chunk.Pos.Z*EngineSettings.ChunkConfig.SizeZ+EngineSettings.ChunkConfig.SizeZ/2),
                            new Vector3(EngineSettings.ChunkConfig.SizeX-0.5f, 0,
                                        EngineSettings.ChunkConfig.SizeZ-0.5f)
                            );
                    }
                    else
                    {
                        Gizmos.color = Color.red;
                        Gizmos.DrawWireCube(
                            new Vector3(chunk.Pos.X * EngineSettings.ChunkConfig.SizeX + EngineSettings.ChunkConfig.SizeX / 2,
                                        EngineSettings.ChunkConfig.SizeY + 0.1f,
                                        chunk.Pos.Z * EngineSettings.ChunkConfig.SizeZ + EngineSettings.ChunkConfig.SizeZ / 2),
                            new Vector3(EngineSettings.ChunkConfig.SizeX-0.5f, 0,
                                        EngineSettings.ChunkConfig.SizeZ-0.5f)
                            );
                    }
                }
            }

            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(
                new Vector3(ViewerChunkPos.X*EngineSettings.ChunkConfig.SizeX+EngineSettings.ChunkConfig.SizeX/2,
                            EngineSettings.ChunkConfig.SizeY+0.15f,
                            ViewerChunkPos.Z*EngineSettings.ChunkConfig.SizeZ+EngineSettings.ChunkConfig.SizeZ/2),
                new Vector3((EngineSettings.WorldConfig.VisibleRange*2+1)*EngineSettings.ChunkConfig.SizeX, 0,
                            (EngineSettings.WorldConfig.VisibleRange*2+1)*EngineSettings.ChunkConfig.SizeZ)
                );

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(
                new Vector3(ViewerChunkPos.X*EngineSettings.ChunkConfig.SizeX+EngineSettings.ChunkConfig.SizeX/2,
                            EngineSettings.ChunkConfig.SizeY+0.15f,
                            ViewerChunkPos.Z*EngineSettings.ChunkConfig.SizeZ+EngineSettings.ChunkConfig.SizeZ/2),
                new Vector3((EngineSettings.WorldConfig.CachedRange*2+1)*EngineSettings.ChunkConfig.SizeX, 0,
                            (EngineSettings.WorldConfig.CachedRange*2+1)*EngineSettings.ChunkConfig.SizeZ)
                );
        }
    }
}