using System.Collections.Generic;
using Assets.Engine.Scripts.Core;
using UnityEngine;

namespace Assets.Engine.Scripts.Builders
{
    public static class CubeBuilderSimple
    {
        public static void Build(List<Vector3> targetBuffer, ref Bounds bounds, LocalPools pools)
        {
            Vector3[] vertices;
            pools.PopVector3Array(24, out vertices);

            // Vertices as they are seen in faces
            //Front
            vertices[0] = new Vector3(bounds.max.x, bounds.min.y, bounds.min.z);
            vertices[1] = new Vector3(bounds.max.x, bounds.max.y, bounds.min.z);
            vertices[2] = new Vector3(bounds.min.x, bounds.max.y, bounds.min.z);
            vertices[3] = new Vector3(bounds.min.x, bounds.min.y, bounds.min.z);
            //Back
            vertices[4] = new Vector3(bounds.min.x, bounds.min.y, bounds.max.z);
            vertices[5] = new Vector3(bounds.min.x, bounds.max.y, bounds.max.z);
            vertices[6] = new Vector3(bounds.max.x, bounds.max.y, bounds.max.z);
            vertices[7] = new Vector3(bounds.max.x, bounds.min.y, bounds.max.z);
            //Right
            vertices[8] = new Vector3(bounds.max.x, bounds.min.y, bounds.max.z);
            vertices[9] = new Vector3(bounds.max.x, bounds.max.y, bounds.max.z);
            vertices[10] = new Vector3(bounds.max.x, bounds.max.y, bounds.min.z);
            vertices[11] = new Vector3(bounds.max.x, bounds.min.y, bounds.min.z);
            //Left
            vertices[12] = new Vector3(bounds.min.x, bounds.min.y, bounds.min.z);
            vertices[13] = new Vector3(bounds.min.x, bounds.max.y, bounds.min.z);
            vertices[14] = new Vector3(bounds.min.x, bounds.max.y, bounds.max.z);
            vertices[15] = new Vector3(bounds.min.x, bounds.min.y, bounds.max.z);
            //Top
            vertices[16] = new Vector3(bounds.max.x, bounds.max.y, bounds.min.z);
            vertices[17] = new Vector3(bounds.max.x, bounds.max.y, bounds.max.z);
            vertices[18] = new Vector3(bounds.min.x, bounds.max.y, bounds.max.z);
            vertices[19] = new Vector3(bounds.min.x, bounds.max.y, bounds.min.z);
            //Bottom
            vertices[20] = new Vector3(bounds.min.x, bounds.min.y, bounds.min.z);
            vertices[21] = new Vector3(bounds.min.x, bounds.min.y, bounds.max.z);
            vertices[22] = new Vector3(bounds.max.x, bounds.min.y, bounds.max.z);
            vertices[23] = new Vector3(bounds.max.x, bounds.min.y, bounds.min.z);

            // Add vertices to buffer
            targetBuffer.AddRange(vertices);

            pools.PushVector3Array(ref vertices);
        }
    }
}