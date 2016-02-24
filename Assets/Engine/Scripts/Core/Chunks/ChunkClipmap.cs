using Assets.Engine.Scripts.Common.Collections;
using UnityEngine;

namespace Assets.Engine.Scripts.Core.Chunks
{
    public struct ChunkClipmapItem
    {
        public int LOD;
        public bool IsWithinVisibleRange;
        public bool IsWithinCachedRange;
    }

    public class ChunkClipmap
    {
        private readonly CircularArray2D<ChunkClipmapItem> m_map;

        public ChunkClipmap()
        {
            m_map = new CircularArray2D<ChunkClipmapItem>(2*EngineSettings.WorldConfig.CachedRange+1, 2*EngineSettings.WorldConfig.CachedRange+1);
        }

        public void Init(int forceLOD, float coefLOD)
        {
            int halfWidth = m_map.Width / 2;
            int halfHeight = m_map.Height / 2;

            for (int z = -halfHeight; z<=halfHeight; z++)
            {
                for (int x = -halfWidth; x<=halfWidth; x++)
                {
                    int lod = DetermineLOD(x, z, forceLOD, coefLOD);
                    bool isInVisibilityRange = IsWithinVisibilityRange(x, z);
                    bool isInCacheRange = IsWithinCachedRange(x, z);
                    m_map[x, z] = new ChunkClipmapItem
                    {
                        LOD = lod,
                        IsWithinVisibleRange = isInVisibilityRange,
                        IsWithinCachedRange = isInCacheRange
                    };
                }
            }
        }

        public ChunkClipmapItem this[int x, int z]
        {
            get
            {
                return m_map[x,z];
            }
        }

        public void SetOffset(int x, int z)
        {
            m_map.SetOffset(-x, -z);
        }

        public bool IsInsideBounds(int x, int z)
        {
            int xx = x+m_map.OffsetX;
            int zz = z+m_map.OffsetZ;
            return IsWithinCachedRange(xx, zz);
        }

        private static int DetermineLOD(int cx, int cz, int forceLOD, float coefLOD)
        {
            int lod = 0;

            if (forceLOD >= 0)
            {
                lod = forceLOD;
            }
            else
            {
                if (coefLOD <= 0)
                    return 0;

                int xDist = Mathf.Abs(cx);
                int zDist = Mathf.Abs(cz);

                // Pick the greater distance and choose a proper LOD
                int dist = Mathf.Max(xDist, zDist);
                lod = (int)(dist / (coefLOD * EngineSettings.ChunkConfig.LogSize));
            }

            // LOD can't be bigger than chunk size
            if (lod < 0)
                lod = 0;
            if (lod > EngineSettings.ChunkConfig.LogSize)
                lod = EngineSettings.ChunkConfig.LogSize;

            return lod;
        }

        private static bool IsWithinVisibilityRange(int cx, int cz)
        {
            return
                cx >= -EngineSettings.WorldConfig.VisibleRange &&
                cz >= -EngineSettings.WorldConfig.VisibleRange &&
                cx <= EngineSettings.WorldConfig.VisibleRange &&
                cz <= EngineSettings.WorldConfig.VisibleRange;
        }

        private static bool IsWithinCachedRange(int cx, int cz)
        {
            return
                cx >= -EngineSettings.WorldConfig.CachedRange &&
                cz >= -EngineSettings.WorldConfig.CachedRange &&
                cx <= EngineSettings.WorldConfig.CachedRange &&
                cz <= EngineSettings.WorldConfig.CachedRange;
        }
    }
}
