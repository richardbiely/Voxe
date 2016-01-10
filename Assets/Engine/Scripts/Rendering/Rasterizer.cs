using System;
using System.Collections.Generic;
using Assets.Engine.Scripts.Common;
using UnityEngine;

namespace Assets.Engine.Scripts.Rendering
{
    public class Rasterizer
    {
        private readonly Camera m_camera;
        private readonly int m_size;
        private readonly int m_sizeMin1;
        private readonly float[] m_depthBuffer;

        private float m_xx;
        private float m_yy;

        private readonly List<List<Vector3>> m_data;
        private int m_currBufferIndex;

        public Rasterizer(Camera camera, int size)
        {
            m_camera = camera;
            m_size = size;
            m_sizeMin1 = m_size-1;
            m_depthBuffer = Helpers.CreateArray1D<float>(size * size);

            m_data = new List<List<Vector3>>();
            m_currBufferIndex = 0;
        }

        public void Add(List<Vector3> buffer)
        {
            // Buffers are added too often and clearing them would result in significant
            // impact on performance due to GC. Therefore, instead of adding and clearing,
            // we add and rewrite.
            // Given that it will be at most few tousands objects added (in extreme case)
            // this won't pose a problem (e.g. 10k*4B ~ 40 kiB of memory).

            if (m_currBufferIndex>=m_data.Count)
                m_data.Add(buffer);
            else
                m_data[m_currBufferIndex] = buffer;

            ++m_currBufferIndex;
        }

        public void Rasterize()
        {
            m_xx = (1f/Screen.width)*m_size;
            m_yy = (1f/Screen.height)*m_size;

            for (int i = 0; i<m_currBufferIndex; i++)
            {
                List<Vector3> vertices = m_data[i];

                for (int j = 0; j<vertices.Count; j += 4)
                {
                    // Vertices converted to screen space
                    Vector3[] verts =
                    {
                        m_camera.WorldToScreenPoint(vertices[j]),
                        m_camera.WorldToScreenPoint(vertices[j+1]),
                        m_camera.WorldToScreenPoint(vertices[j+2]),
                        m_camera.WorldToScreenPoint(vertices[j+3])
                    };

                    ProcessTriangle(ref verts[2], ref verts[1], ref verts[0]);
                    ProcessTriangle(ref verts[3], ref verts[2], ref verts[0]);
                }
            }

            m_currBufferIndex = 0;
        }

        public float GetDepth(int x, int y)
        {
            int index = Helpers.GetIndex1DFrom2D(x, y, m_size);
            return m_depthBuffer[index];
        }
        
        private void ProcessTriangle(ref Vector3 p1, ref Vector3 p2, ref Vector3 p3)
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

            float y21Inv = Mathf.Approximately(y21, 0f) ? 1f : 1f/y21;
            float y31Inv = Mathf.Approximately(y31, 0f) ? 1f : 1f/y31;
            float y32Inv = Mathf.Approximately(y32, 0f) ? 1f : 1f/y32;

            // Compute slopes
            float dP1P2 = (y21 > 0f) ? x21 * y21Inv : 0f;
            float dP1P3 = (y31 > 0f) ? x31 * y31Inv : 0f;

            // Compute min/max for Y axis
            int minY = (int)p1.y;
            int maxY = (int)p3.y;
            int p2Y = (int)p2.y;

            minY = Math.Max(0, minY);
            maxY = Math.Min(maxY, m_sizeMin1);
            p2Y = Math.Max(0, p2Y);
            p2Y = Math.Min(p2Y, m_sizeMin1);
            
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
                for (y = minY; y < p2Y; y++)
                {
                    ProcessScanLine(y, ref p1, ref p3, ref p1, ref p2, y31Inv, y21Inv);
                }
                for (y = p2Y; y <= maxY; y++)
                {
                    ProcessScanLine(y, ref p1, ref p3, ref p2, ref p3, y31Inv, y32Inv);
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
                // ProcessScanLine(y, p1, p2, p1, p3)
                for (y = minY; y < p2Y; y++)
                    ProcessScanLine(y, ref p1, ref p2, ref p1, ref p3, y21Inv, y31Inv);
                // ProcessScanLine(y, p2, p3, p1, p3)
                for (y = p2Y; y <= maxY; y++)
                    ProcessScanLine(y, ref p2, ref p3, ref p1, ref p3, y32Inv, y31Inv);
            }
        }
        
        // drawing line between 2 points from left to right
        // papb -> pcpd
        private void ProcessScanLine(int y, ref Vector3 pa, ref Vector3 pb, ref Vector3 pc, ref Vector3 pd, float yBa, float yDc)
        {
            // Thanks to current y, we can compute the gradient to compute others values like
            // the starting x (sx) and ending x (ex) to draw between
            // if pa.y == pb.y or pc.y == pd.y, gradient is forced to 1
            float gradient1 = (pa.y != pb.y ? ((float)y - pa.y) * yBa : 1f).Clamp(0f, 1f);
            float gradient2 = (pc.y != pd.y ? ((float)y - pc.y) * yDc : 1f).Clamp(0f, 1f);
            
            // Starting X and ending X
            int sx = (int)Helpers.Interpolate(pa.x, pb.x, gradient1);
            int ex = (int)Helpers.Interpolate(pc.x, pd.x, gradient2);
            sx = Math.Max(0, sx);
            ex = Math.Min(ex, m_sizeMin1);

            // Starting Z and ending Z
            float z1 = Helpers.Interpolate(pa.z, pb.z, gradient1);
            float z2 = Helpers.Interpolate(pc.z, pd.z, gradient2);
            
            // Draw a horizontal line
            float exsx = 1f / (ex - sx);
            for (int x = sx; x < ex; x++)
            {
                float gradient = ((x-sx)*exsx).Clamp(0f, 1f);
                float z = Helpers.Interpolate(z1, z2, gradient);

                int bufferX = (int)(x * m_xx);
                int bufferY = (int)(y * m_yy);
                int index = Helpers.GetIndex1DFrom2D(bufferX, bufferY, m_size);
                PutPixel(index, z);
            }
        }

        private void PutPixel(int index, float z)
        {
            if (m_depthBuffer[index] < z)
                m_depthBuffer[index] = z;
        }
    }
}
