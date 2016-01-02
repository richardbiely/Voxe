using System.Collections.Generic;
using Assets.Engine.Scripts.Provider;
using UnityEngine;

namespace Assets.Engine.Scripts.Rendering
{
    public class DrawCallBatcher
    {
        private const string GOPChunk = "Chunk";

        private readonly RenderBuffer m_renderBuffer;
        private readonly List<GameObject> m_drawCalls;

        public Vector3 Pos;

        public DrawCallBatcher()
        {
            m_renderBuffer = new RenderBuffer();
            m_drawCalls = new List<GameObject>();
        }

        /// <summary>
        ///     Clear all draw calls
        /// </summary>
        public void Clear()
        {
            for (int i = 0; i < m_drawCalls.Count; i++)
            {
                var go = m_drawCalls[i];
                if (go==null)
                    continue;

                var filter = go.GetComponent<MeshFilter>();
                filter.mesh.Clear(false);
                ObjectPoolProvider.Meshes.Push(filter.mesh);
                filter.mesh = null;

                GameObjectProvider.PushObject(GOPChunk, go);
            }

            m_drawCalls.Clear();
            m_renderBuffer.Clear();
        }

        /// <summary>
        ///     Finalize the draw calls
        /// </summary>
        public void FinalizeDrawCalls()
        {
            Flush();
        }

        /// <summary>
        ///     Batch the given render buffer
        /// </summary>
        public void Batch(RenderBuffer renderBuffer)
        {
            // Skip empty inputs
            if (renderBuffer.IsEmpty())
                return;

            // Lets create a separate batch if the number of vertices is too great
            if (m_renderBuffer.Positions.Count + renderBuffer.Positions.Count >= 65000)
            {
                Debug.LogWarning("Too many vertices :O");
                Flush();
            }

            // Add data to main buffer
            int vOffset = m_renderBuffer.Positions.Count;

            // Further calls to batch need to offset each triangle value by the number of triangles previously present
            if (vOffset != 0)
            {
                for (int j = 0; j < renderBuffer.Triangles.Count; j++)
                    renderBuffer.Triangles[j] += vOffset;
            }

            m_renderBuffer.Positions.AddRange(renderBuffer.Positions);
            m_renderBuffer.Normals.AddRange(renderBuffer.Normals);
            m_renderBuffer.UVs.AddRange(renderBuffer.UVs);
            m_renderBuffer.UV2.AddRange(renderBuffer.UV2);
            m_renderBuffer.Colors.AddRange(renderBuffer.Colors);
            m_renderBuffer.Triangles.AddRange(renderBuffer.Triangles);
        }

        private void Flush()
        {
            if (m_renderBuffer.Positions.Count <= 0)
            {
                Debug.Log("Empty flush");
                return;
            }

            var go = GameObjectProvider.PopObject(GOPChunk);
            if (go != null)
            {
#if DEBUG
                go.name = string.Format("[{0};{1}]", (int)Pos.x>>EngineSettings.ChunkConfig.LogSizeX, (int)Pos.z>>EngineSettings.ChunkConfig.LogSizeZ);
#endif

                Mesh mesh = ObjectPoolProvider.Meshes.Pop();
                m_renderBuffer.CopyToMesh(mesh, false);

                MeshFilter filter = go.GetComponent<MeshFilter>();
                filter.sharedMesh = null;
                filter.sharedMesh = mesh;
                filter.transform.position = Pos;

                m_drawCalls.Add(go);
            }

            m_renderBuffer.Clear();
        }

        public void SetVisible(bool show)
        {
            for(int i=0; i<m_drawCalls.Count; i++)
                m_drawCalls[i].SetActive(show);
        }
    }
}