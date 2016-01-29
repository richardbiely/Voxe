using System.Collections.Generic;
using UnityEngine;

namespace Assets.Engine.Scripts.Rendering
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
            float xx = (1f / Screen.width) * Rasterizer.Width;
            float yy = (1f / Screen.height) * Rasterizer.Height;

            Rasterizer.PerformRaterization();

            Profiler.BeginSample("OcclusionCulling");

            for (int i = 0; i<m_currEntiesCnt; i++)
            {
                IOcclusionEntity entity = m_entites[i];

                // No bounding box means entity is insignificant in terms of occlusion
                List<Vector3> vertices = entity.BBoxVerticesTransformed;
                if (vertices.Count<=0)
                {
                    entity.Visible = true;
                    continue;
                }

                float sectionDepth = float.MaxValue;
				float sectionDepthMax = float.MinValue;
                int pos = -1;

                // Iterate over vertices as quads
                for (int j = 0; j < vertices.Count; j += 4)
                {
                    for (int v = j; v < j+4; v++)
                    {
						// Find the vertex closest to the camera
                        if (vertices[v].z < sectionDepth)
                        {
                            sectionDepth = vertices[v].z;
                            pos = v;
                        }

						// Find the vertex furthest from the camera
						if (vertices[v].z > sectionDepthMax)
							sectionDepthMax = vertices[v].z;
                    }
                }

				// If the furthest point of an entity lies behind camera the entity is invisible
				if (sectionDepthMax < 0.0f)
				{
					entity.Visible = false;
					continue;
				}

                // Vertex converted to screen space
                Vector3 closestVert = vertices[pos];

                // Vertex converted to depth buffer space
                int x = (int)((closestVert.x - 0.5f) * xx);
                int y = (int)((closestVert.y - 0.5f) * yy);

                x = Mathf.Max(0, Mathf.Min(x, Rasterizer.Width - 1));
                y = Mathf.Max(0, Mathf.Min(y, Rasterizer.Height - 1));

                // An entity covered by raster is considered hidden
                float rasterDepth = Rasterizer.GetDepth(x, y);
                entity.Visible = !(rasterDepth < sectionDepth);
            }

            // In order to not grow into infinity we clear m_entites if the difference
            // with m_currEntitesCnt is too big
            if (m_entites.Count>1000 && m_entites.Count>2*m_currEntiesCnt)
                m_entites.Clear();
            m_currEntiesCnt = 0;

            Profiler.EndSample();
        }
    }
}
