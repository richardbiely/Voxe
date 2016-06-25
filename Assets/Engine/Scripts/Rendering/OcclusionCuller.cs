using System;
using System.Collections.Generic;
using Engine.Scripts.Common;
using UnityEngine;

namespace Engine.Scripts.Rendering
{
    public class OcclusionCuller: MonoBehaviour
    {
        public Rasterizer Rasterizer;

        private List<IOcclusionEntity> m_entites;
        private int m_currEntiesCnt;

        void Awake()
        {
            m_entites = new List<IOcclusionEntity>();
            m_currEntiesCnt = 0;
        }
        
        public void RegisterEntity(IOcclusionEntity entity)
        {
            // Early rejection
            if (!entity.IsOccluder())
                return;

            // Entities are added too often and clearing them would result in significant
            // impact on performance due to GC. Therefore, instead of adding and clearing,
            // we add and rewrite.

            if (m_currEntiesCnt >= m_entites.Count)
                m_entites.Add(entity);
            else
                m_entites[m_currEntiesCnt] = entity;

            ++m_currEntiesCnt;

            m_entites.Add(entity);
            Rasterizer.Add(entity);
        }

        public void PerformOcclusion()
        {
            Rasterizer.PerformRaterization();

            Profiler.BeginSample("OcclusionCulling");

            for (int i = 0; i<m_currEntiesCnt; i++)
            {
                IOcclusionEntity entity = m_entites[i];
                entity.Visible = false;

                List<Vector3> vertices = entity.BBoxVerticesTransformed;
                for (int j = 0; j < vertices.Count; j += 4)
                {
                    Vector3[] verts =
                    {
                        vertices[j  ],
                        vertices[j+1],
                        vertices[j+2],
                        vertices[j+3]
                    };

                    // Rasterize triangles and compare theirs pixels against depth buffer
                    // Once a proper points is found, return.

                    if (ProcessTriangle(ref verts[2], ref verts[1], ref verts[0]))
                    {
                        entity.Visible = true;
                        break;
                    }

                    if (ProcessTriangle(ref verts[3], ref verts[2], ref verts[0]))
                    {
                        entity.Visible = true;
                        break;
                    }
                }
            }

            Profiler.EndSample();

            // In order to not grow into infinity we clear m_entites if the difference
            // with m_currEntitesCnt is too big
            if (m_entites.Count>1000 && m_entites.Count>2*m_currEntiesCnt)
                m_entites.Clear();
            m_currEntiesCnt = 0;
        }

        private bool ProcessTriangle(ref Vector3 p1, ref Vector3 p2, ref Vector3 p3)
        {
            // Sort the points in order to always have them ordered like following: p1, p2, p3
            if (p1.y > p2.y)
            {
                Vector3 temp = p2;
                p2 = p1;
                p1 = temp;
            }
            if (p2.y > p3.y)
            {
                Vector3 temp = p2;
                p2 = p3;
                p3 = temp;
            }
            if (p1.y > p2.y)
            {
                Vector3 temp = p2;
                p2 = p1;
                p1 = temp;
            }

            // Helper values
            float y21 = p2.y - p1.y;
            float y31 = p3.y - p1.y;
            float y32 = p3.y - p2.y;
            float x21 = p2.x - p1.x;
            float x31 = p3.x - p1.x;

            float y21Inv = Mathf.Approximately(y21, 0f) ? 1f : 1f / y21;
            float y31Inv = Mathf.Approximately(y31, 0f) ? 1f : 1f / y31;
            float y32Inv = Mathf.Approximately(y32, 0f) ? 1f : 1f / y32;

            // Compute slopes
            float dP1P2 = (y21 > 0f) ? x21 * y21Inv : 0f;
            float dP1P3 = (y31 > 0f) ? x31 * y31Inv : 0f;

            // Compute min/max for Y axis
            int minY = (int)p1.y;
            int maxY = (int)p3.y;
            int p2Y = (int)p2.y;

            minY = Math.Max(0, minY);
            maxY = Math.Min(maxY, Rasterizer.Height-1);
            p2Y = Math.Max(0, p2Y);
            p2Y = Math.Min(p2Y, Rasterizer.Height-1);

            int y;

            // P1
            // -
            // -- 
            // - -
            // -  -
            // -   - P2
            // -  -
            // - -
            // -
            // P3
            if (dP1P2 > dP1P3)
            {
                for (y = minY; y<p2Y; y++)
                {
                    if (ProcessScanLine(y, ref p1, ref p3, ref p1, ref p2, y31Inv, y21Inv))
                        return true;
                }
                for (y = p2Y; y<=maxY; y++)
                {
                    if (ProcessScanLine(y, ref p1, ref p3, ref p2, ref p3, y31Inv, y32Inv))
                        return true;
                }
            }

            //       P1
            //        -
            //       -- 
            //      - -
            //     -  -
            // P2 -   - 
            //     -  -
            //      - -
            //        -
            //       P3
            else
            {
                for (y = minY; y<p2Y; y++)
                {
                    if (ProcessScanLine(y, ref p1, ref p2, ref p1, ref p3, y21Inv, y31Inv))
                        return true;
                }
                for (y = p2Y; y<=maxY; y++)
                {
                    if (ProcessScanLine(y, ref p2, ref p3, ref p1, ref p3, y32Inv, y31Inv))
                        return true;
                }
            }

            return false;
        }

        // drawing line between 2 points from left to right
        // papb -> pcpd
        private bool ProcessScanLine(int y, ref Vector3 pa, ref Vector3 pb, ref Vector3 pc, ref Vector3 pd, float yBa, float yDc)
        {
            // Thanks to current y, we can compute the gradient to compute others values like
            // the starting x (sx) and ending x (ex) to draw between
            // if pa.y == pb.y or pc.y == pd.y, gradient is forced to 1
            float gradient1 = (pa.y != pb.y ? (y - pa.y) * yBa : 1f).Clamp(0f, 1f);
            float gradient2 = (pc.y != pd.y ? (y - pc.y) * yDc : 1f).Clamp(0f, 1f);

            // Starting X and ending X
            int sx = (int)Helpers.Interpolate(pa.x, pb.x, gradient1);
            int ex = (int)Helpers.Interpolate(pc.x, pd.x, gradient2);
            sx = Math.Max(0, sx);
            ex = Math.Min(ex, Rasterizer.Width-1);

            // Starting Z and ending Z
            float z1 = Helpers.Interpolate(pa.z, pb.z, gradient1);
            float z2 = Helpers.Interpolate(pc.z, pd.z, gradient2);

            // Draw a horizontal line
            float exsx = 1f / (ex - sx);
            for (int x = sx; x <= ex; x++)
            {
                float gradient = ((x - sx) * exsx).Clamp(0f, 1f);
                float z = Helpers.Interpolate(z1, z2, gradient);

                // Skip points behind camera
                if (z < 0.0f)
                    continue;

                // We are interested in points closer to camera than a value written in rasterizer's depth buffer
                // Once we find such a value we can early exit
                float depth = Rasterizer.GetDepth(x, y);
                if (z<depth)
                    return true;
            }

            return false;
        }
    }
}
