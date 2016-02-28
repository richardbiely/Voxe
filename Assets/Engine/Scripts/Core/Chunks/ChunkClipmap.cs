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
        private readonly AxisInfo[] m_axes;
        private readonly int m_diffCachedVisibleRange;
        private readonly int m_size;

        public ChunkClipmap(Map map, int rangeYMin, int rangeYMax)
        {
            m_diffCachedVisibleRange = map.CachedRange-map.VisibleRange;
            m_size = map.CachedRange;

            if (rangeYMin<-m_size)
                rangeYMin = -m_size;
            if (rangeYMax>m_size)
                rangeYMax = m_size;

            RangeYMin = rangeYMin;
            RangeYMax = rangeYMax;

            m_axes = new[]
            {
                new AxisInfo
                {
                    Map = new CircularArray1D<ChunkClipmapItem>(2*m_size+1),
                    RangeMin = -m_size,
                    RangeMax = m_size
                },
                new AxisInfo
                {
                    Map = new CircularArray1D<ChunkClipmapItem>(2*m_size+1),
                    RangeMin = rangeYMin,
                    RangeMax = rangeYMax
                },
                new AxisInfo
                {
                    Map = new CircularArray1D<ChunkClipmapItem>(2*m_size+1),
                    RangeMin = -m_size,
                    RangeMax = m_size
                }
            };
        }

        public int RangeYMin { get; private set; }
        public int RangeYMax { get; private set; }

        public ChunkClipmapItem this[int x, int y, int z]
        {
            get
            {
                int absX = Mathf.Abs(x+m_axes[0].Map.Offset);
                int absY = Mathf.Abs(y+m_axes[1].Map.Offset);
                int absZ = Mathf.Abs(z+m_axes[2].Map.Offset);

                if (absX>absZ)
                    return absX>absY ? m_axes[0].Map[x] : m_axes[1].Map[y];

                return absZ>absY ? m_axes[2].Map[z] : m_axes[1].Map[y];
            }
        }

        public void Init(int forceLOD, float coefLOD)
        {
            int halfWidth = m_axes[0].Map.Size/2;

            // Generate clipmap fields. It is enough to generate them for one dimension for clipmap is symetrical in all m_axes
            for (int axis = 0; axis<3; axis++)
            {
                for (int distance = -halfWidth; distance<=halfWidth; distance++)
                {
                    int lod = DetermineLOD(distance, forceLOD, coefLOD);
                    bool isInVisibilityRange = IsWithinVisibilityRange(axis, distance);
                    bool isInCacheRange = IsWithinCachedRange(axis, distance);

                    m_axes[axis].Map[distance] = new ChunkClipmapItem
                    {
                        LOD = lod,
                        IsWithinVisibleRange = isInVisibilityRange,
                        IsWithinCachedRange = isInCacheRange
                    };
                }
            }
        }

        public void SetOffset(int x, int y, int z)
        {
            m_axes[0].Map.Offset = -x;
            m_axes[1].Map.Offset = -y;
            m_axes[2].Map.Offset = -z;
        }

        public bool IsInsideBounds(int x, int y, int z)
        {
            int xx = x+m_axes[0].Map.Offset;
            int yy = y+m_axes[1].Map.Offset;
            int zz = z+m_axes[2].Map.Offset;
            return IsWithinCachedRange(0, xx) && IsWithinCachedRange(1, yy) && IsWithinCachedRange(2, zz);
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

        private bool IsWithinVisibilityRange(int axis, int distance)
        {
            int rangeMin = m_axes[axis].RangeMin+m_diffCachedVisibleRange;
            int rangeMax = m_axes[axis].RangeMax-m_diffCachedVisibleRange;
            return distance>=rangeMin && distance<=rangeMax;
        }

        private bool IsWithinCachedRange(int axis, int distance)
        {
            int rangeMin = m_axes[axis].RangeMin;
            int rangeMax = m_axes[axis].RangeMax;
            return distance>=rangeMin && distance<=rangeMax;
        }

        private class AxisInfo
        {
            public CircularArray1D<ChunkClipmapItem> Map; // -N ... 0 ... N
            public int RangeMax;
            public int RangeMin;
        }
    }
}