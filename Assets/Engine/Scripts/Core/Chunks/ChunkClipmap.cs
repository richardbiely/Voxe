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
        private readonly CircularArray1D<ChunkClipmapItem> m_map;
        private int m_offsetX;
        private int m_offsetZ;

        public ChunkClipmap()
        {
            m_map = new CircularArray1D<ChunkClipmapItem>(2*EngineSettings.WorldConfig.CachedRange+1); // -N ... 0 ... N
            m_offsetX = 0;
            m_offsetZ = 0;
        }

        public ChunkClipmapItem this[int x, int z]
        {
            get
            {
                int absX = Mathf.Abs(x+m_offsetX);
                int absZ = Mathf.Abs(z+m_offsetZ);
                if (absX>absZ)
                {
                    m_map.SetOffset(m_offsetX);
                    return m_map[x];
                }
                m_map.SetOffset(m_offsetZ);
                return m_map[z];
            }
        }

        public void Init(int forceLOD, float coefLOD)
        {
            int halfWidth = m_map.Size/2;

            // Generate clipmap fields. It is enough to generate them for one dimension for clipmap is symetrical in all axes
            for (int distance = -halfWidth; distance<=halfWidth; distance++)
            {
                int lod = DetermineLOD(distance, forceLOD, coefLOD);
                bool isInVisibilityRange = IsWithinVisibilityRange(distance);
                bool isInCacheRange = IsWithinCachedRange(distance);

                m_map[distance] = new ChunkClipmapItem
                {
                    LOD = lod,
                    IsWithinVisibleRange = isInVisibilityRange,
                    IsWithinCachedRange = isInCacheRange
                };
            }
        }

        public void SetOffset(int x, int z)
        {
            m_offsetX = -x;
            m_offsetZ = -z;
        }

        public bool IsInsideBounds(int x, int z)
        {
            int xx = x+m_offsetX;
            int zz = z+m_offsetZ;
            return IsWithinCachedRange(xx) && IsWithinCachedRange(zz);
        }

        private static int DetermineLOD(int distance, int forceLOD, float coefLOD)
        {
            int lod = 0;

            if (forceLOD>=0)
            {
                lod = forceLOD;
            }
            else
            {
                if (coefLOD<=0)
                    return 0;

                // Pick the greater distance and choose a proper LOD
                int dist = Mathf.Abs(distance);
                lod = (int)(dist/(coefLOD*EngineSettings.ChunkConfig.LogSize));
            }

            // LOD can't be bigger than chunk size
            if (lod<0)
                lod = 0;
            if (lod>EngineSettings.ChunkConfig.LogSize)
                lod = EngineSettings.ChunkConfig.LogSize;

            return lod;
        }

        private static bool IsWithinVisibilityRange(int distance)
        {
            return
                distance>=-EngineSettings.WorldConfig.VisibleRange &&
                distance<=EngineSettings.WorldConfig.VisibleRange;
        }

        private static bool IsWithinCachedRange(int distance)
        {
            return
                distance>=-EngineSettings.WorldConfig.CachedRange &&
                distance<=EngineSettings.WorldConfig.CachedRange;
        }
    }
}