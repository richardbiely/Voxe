using Assets.Engine.Scripts.Common.Memory;
using System.Collections.Generic;
using Assets.Engine.Scripts.Core.Blocks;
using Assets.Engine.Scripts.Core.Pooling;
using Assets.Engine.Scripts.Rendering;
using UnityEngine;

namespace Assets.Engine.Scripts.Core
{
    /// <summary>
    ///     Local object pools for often used heap objects.
    /// </summary>
    public class LocalPools: AObjectPool
    {
        private readonly Dictionary<int, IArrayPool<VertexData>> m_vertexDataArrayPools =
            new Dictionary<int, IArrayPool<VertexData>>(128);

        private readonly Dictionary<int, IArrayPool<BlockData>> m_blockDataArrayPools =
            new Dictionary<int, IArrayPool<BlockData>>(128);

        private readonly Dictionary<int, IArrayPool<Vector3>> m_vector3ArrayPools =
            new Dictionary<int, IArrayPool<Vector3>>(128);

        public void PopVertexDataArray(int size, out VertexData[] arr)
        {
            PopArray(size, m_vertexDataArrayPools, out arr);
        }

        public void PopBlockDataArray(int size, out BlockData[] arr)
        {
            PopArray(size, m_blockDataArrayPools, out arr);
        }

        public void PopVector3Array(int size, out Vector3[] arr)
        {
            PopArray(size, m_vector3ArrayPools, out arr);
        }

        public void PushVertexDataArray(ref VertexData[] arr)
        {
            PushArray(ref arr, m_vertexDataArrayPools);
        }

        public void PushBlockDataArray(ref BlockData[] arr)
        {
            PushArray(ref arr, m_blockDataArrayPools);
        }

        public void PushVector3Array(ref Vector3[] arr)
        {
            PushArray(ref arr, m_vector3ArrayPools);
        }
    }
}
