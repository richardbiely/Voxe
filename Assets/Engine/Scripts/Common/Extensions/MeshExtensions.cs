using UnityEngine;

namespace Assets.Engine.Scripts.Common.Extensions
{
    public static class MeshExtensions
    {
        public static void GenerateTangents(Mesh mesh)
        {
            // Speed up math by copying the mesh arrays
            int[] triangles = mesh.triangles;
            Vector3[] vertices = mesh.vertices;
            Vector2[] uv = mesh.uv;
            Vector3[] normals = mesh.normals;

            // Variable definitions
            int triangleCount = triangles.Length;
            int vertexCount = vertices.Length;

            Vector3[] tan1 = new Vector3[vertexCount];
            Vector3[] tan2 = new Vector3[vertexCount];

            Vector4[] tangents = new Vector4[vertexCount];

            for (long t = 0; t < triangleCount; t += 3)
            {
                long i1 = triangles[t + 0];
                long i2 = triangles[t + 1];
                long i3 = triangles[t + 2];

                Vector3 v1 = vertices[i1];
                Vector3 v2 = vertices[i2];
                Vector3 v3 = vertices[i3];

                Vector2 w1 = uv[i1];
                Vector2 w2 = uv[i2];
                Vector2 w3 = uv[i3];

                float x1 = v2.x - v1.x;
                float y1 = v2.y - v1.y;
                float z1 = v2.z - v1.z;

                float x2 = v3.x - v1.x;
                float y2 = v3.y - v1.y;
                float z2 = v3.z - v1.z;

                float s1 = w2.x - w1.x;
                float s2 = w3.x - w1.x;

                float t1 = w2.y - w1.y;
                float t2 = w3.y - w1.y;

                // Avoid division by zero
                float div = s1 * t2 - s2 * t1;
                float r = (Mathf.Abs(div) > Mathf.Epsilon) ? (1f / div) : 0f;

                Vector3 sdir = new Vector3((t2 * x1 - t1 * x2) * r, (t2 * y1 - t1 * y2) * r, (t2 * z1 - t1 * z2) * r);
                Vector3 tdir = new Vector3((s1 * x2 - s2 * x1) * r, (s1 * y2 - s2 * y1) * r, (s1 * z2 - s2 * z1) * r);

                tan1[i1] += sdir;
                tan1[i2] += sdir;
                tan1[i3] += sdir;

                tan2[i1] += tdir;
                tan2[i2] += tdir;
                tan2[i3] += tdir;
            }

            for (long v = 0; v < vertexCount; ++v)
            {
                Vector3 n = normals[v];
                Vector3 t = tan1[v];

                //Vector3 tmp = (t - n*Vector3.Dot(n, t)).normalized;
                //tangents[v] = new Vector4(tmp.x, tmp.y, tmp.z);
                Vector3.OrthoNormalize(ref n, ref t);

                tangents[v].x = t.x;
                tangents[v].y = t.y;
                tangents[v].z = t.z;
                tangents[v].w = (Vector3.Dot(Vector3.Cross(n, t), tan2[v]) < 0.0f) ? -1.0f : 1.0f;
            }

            mesh.tangents = tangents;
        }
    }
}
