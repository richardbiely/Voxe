using System;
using System.Collections.Generic;
using Assets.Engine.Scripts.Common;
using UnityEngine;

namespace Assets.Engine.Scripts.Rendering
{
    public class Rasterizer: MonoBehaviour
    {
        public Camera RasterizerCamera;
        public int Width;
        public int Height;

#if DEBUG
        private Texture2D m_texture;
#endif

        private int m_widthMin1;
        private int m_heightMin1;
        private float[] m_depthBuffer;

        private float m_xx;
        private float m_yy;

        private List<IRasterizationEntity> m_entities;
        private int m_currEntiesCnt;

        private void Awake()
        {
            if (Width<=0)
                Width = Screen.width;
            if (Height<=0)
                Height = Screen.height;
            m_widthMin1 = Width-1;
            m_heightMin1 = Height-1;

            m_depthBuffer = Helpers.CreateArray1D<float>(Width * Height);

            m_entities = new List<IRasterizationEntity>();
            m_currEntiesCnt = 0;

#if DEBUG
            m_texture = new Texture2D(128, 128, TextureFormat.ARGB32, false);
#endif
        }
        
        public void Add(IRasterizationEntity entity)
        {
            // Buffers are added too often and clearing them would result in significant
            // impact on performance due to GC. Therefore, instead of adding and clearing,
            // we add and rewrite.
            // Given that it will be at most few tousands objects added (in extreme case)
            // this won't pose a problem (e.g. 10k*4B ~ 40 kiB of memory).

            if (m_currEntiesCnt>= m_entities.Count)
                m_entities.Add(entity);
            else
                m_entities[m_currEntiesCnt] = entity;

            ++m_currEntiesCnt;
        }

        public void PerformRaterization()
        {
            Profiler.BeginSample("Rasterization");

            m_xx = (1f/Screen.width)*Width;
            m_yy = (1f/Screen.height)*Height;

            // Clean up old data
            Array.Clear(m_depthBuffer, 0, m_depthBuffer.Length);

            // Fill buffer with new data
            for (int i = 0; i<m_currEntiesCnt; i++)
            {
                List<Vector3> vertices = m_entities[i].BBoxVertices;
                List<Vector3> verticesTransformed = m_entities[i].BBoxVerticesTransformed;

                for (int j = 0; j<vertices.Count; j += 4)
                {
                    Profiler.BeginSample("Sampling");
                    
                    for (int k=j; k<j+4; k++)
                    {
                        // Transform vertices to screen space
                        Vector3 screenPos = RasterizerCamera.WorldToScreenPoint(vertices[k]);
                        // Transform vertices to buffer space
                        verticesTransformed[k] = new Vector3(screenPos.x * m_xx, screenPos.y * m_yy, screenPos.z);
                    }

                    Vector3[] verts =
                    {
                        verticesTransformed[j  ],
                        verticesTransformed[j+1],
                        verticesTransformed[j+2],
                        verticesTransformed[j+3]
                    };

                    Profiler.EndSample();

                    // Rasterize triangles from transformed vertices
                    ProcessTriangle(ref verts[2], ref verts[1], ref verts[0]);
                    ProcessTriangle(ref verts[3], ref verts[2], ref verts[0]);
                }
            }

            m_currEntiesCnt = 0;

            Profiler.EndSample();

            #if DEBUG
            for (int j = 0; j<Height; j++)
            {
                for (int i = 0; i<Width; i++)
                {
                    float depth = GetDepth(i, j) / 256;
                    m_texture.SetPixel(i, j, new Color(depth, depth, depth));
                }
            }
            m_texture.Apply();
            #endif
        }

#if DEBUG
        void OnGUI()
        {
            if (m_texture!=null)
            {
                GUI.DrawTexture(new Rect(0, Screen.height-256, 256, 256), m_texture, ScaleMode.ScaleToFit, false, 0);
            }
        }
#endif

        public float GetDepth(int x, int y)
        {
            int index = Helpers.GetIndex1DFrom2D(x, y, Width);
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
            maxY = Math.Min(maxY, m_heightMin1);
            p2Y = Math.Max(0, p2Y);
            p2Y = Math.Min(p2Y, m_heightMin1);
            
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
            Profiler.BeginSample("ScanLine");

            // Thanks to current y, we can compute the gradient to compute others values like
            // the starting x (sx) and ending x (ex) to draw between
            // if pa.y == pb.y or pc.y == pd.y, gradient is forced to 1
            float gradient1 = (pa.y != pb.y ? ((float)y - pa.y) * yBa : 1f).Clamp(0f, 1f);
            float gradient2 = (pc.y != pd.y ? ((float)y - pc.y) * yDc : 1f).Clamp(0f, 1f);
            
            // Starting X and ending X
            int sx = (int)Helpers.Interpolate(pa.x, pb.x, gradient1);
            int ex = (int)Helpers.Interpolate(pc.x, pd.x, gradient2);
            sx = Math.Max(0, sx);
            ex = Math.Min(ex, m_widthMin1);

            // Starting Z and ending Z
            float z1 = Helpers.Interpolate(pa.z, pb.z, gradient1);
            float z2 = Helpers.Interpolate(pc.z, pd.z, gradient2);
            
            // Draw a horizontal line
            float exsx = 1f / (ex - sx);
            for (int x = sx; x < ex; x++)
            {
                float gradient = ((x-sx)*exsx).Clamp(0f, 1f);
                float z = Helpers.Interpolate(z1, z2, gradient);
                
                int index = Helpers.GetIndex1DFrom2D(x, y, Width);
                PutPixel(index, z);
            }

            Profiler.EndSample();
        }

        private void PutPixel(int index, float z)
        {
            if (m_depthBuffer[index] < z)
                m_depthBuffer[index] = z;
        }
    }
}
