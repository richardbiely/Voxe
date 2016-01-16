using System.Collections.Generic;
using Assets.Engine.Scripts.Common.DataTypes;
using Assets.Engine.Scripts.Provider;
using UnityEngine;
using UnityEngine.Assertions;

namespace Assets.Engine.Scripts.Rendering
{
    public class DrawCallBatcher
    {
        private const string GOPChunk = "Chunk";

        private readonly RenderBuffer m_renderBuffer;
        private readonly List<GameObject> m_drawCalls;
        private readonly List<Renderer> m_drawCallRenderers;

        private bool m_visible;

#if DEBUG
        public Vector3Int Pos { get; set; }
#endif

        public DrawCallBatcher()
        {
            m_renderBuffer = new RenderBuffer();
            m_drawCalls = new List<GameObject>();
            m_drawCallRenderers = new List<Renderer>();

            m_visible = false;
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
                filter.sharedMesh.Clear(false);
                ObjectPoolProvider.Meshes.Push(filter.sharedMesh);
                filter.sharedMesh = null;

                GameObjectProvider.PushObject(GOPChunk, go);
            }

            m_drawCalls.Clear();
            m_renderBuffer.Clear();
            m_drawCallRenderers.Clear();

            m_visible = false;
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
                // [X, Z, Y]:<part> - name the gameobject so that it can be easily identified with a naked eye
                go.name = string.Format("[{0},{1},{2}]:{3}", Pos.X, Pos.Z, Pos.Y, m_drawCalls.Count);
#endif

                Mesh mesh = ObjectPoolProvider.Meshes.Pop();
                Assert.IsTrue(mesh.vertices.Length<=0);
                m_renderBuffer.CopyToMesh(mesh, false);

                MeshFilter filter = go.GetComponent<MeshFilter>();
                filter.sharedMesh = null;
                filter.sharedMesh = mesh;
                filter.transform.position = new Vector3(Pos.X << EngineSettings.ChunkConfig.LogSizeX, 0, Pos.Z << EngineSettings.ChunkConfig.LogSizeZ);

                m_drawCalls.Add(go);
                m_drawCallRenderers.Add(go.GetComponent<Renderer>());
            }

            m_renderBuffer.Clear();
        }

        public void SetVisible(bool show)
        {
            bool visible = false;
            for (int i = 0; i<m_drawCallRenderers.Count; i++)
            {
                Renderer renderer = m_drawCallRenderers[i];
                renderer.enabled = show;
                visible = visible|show;
            }

            m_visible = visible;
        }

        public bool IsVisible()
        {
            return m_drawCalls.Count>0 && m_visible;
        }
    }
}