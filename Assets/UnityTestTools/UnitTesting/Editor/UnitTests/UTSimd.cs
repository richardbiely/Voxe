#if DEBUG && UNITY_STANDALONE_WIN
using Assets.Engine.Scripts.Common.DataTypes;
using Assets.Engine.Scripts.Core.Chunks;
using Mono.Simd;
using NUnit.Framework;
using UnityEngine;

namespace Assets.Engine.Scripts.UnitTesting
{
    [TestFixture]
    public class UTSimd
    {
        [Test]
        public void SimdTest()
        {
            Bounds bounds = new Bounds(new Vector3(1, 1, 1), new Vector3(3, 3, 3));
            const int positionX = 4;
            const int positionZ = 3;

            // Non-SIMD version
            Vector3Int bMin;
            Vector3Int bMax;
            Vector3Int myVec52 = new Vector3Int(Chunk.Vec5.X, Chunk.Vec5.X, Chunk.Vec5.X);
            {
                Vector3Int pom = new Vector3Int(positionX * EngineSettings.ChunkConfig.SizeX, 0, positionZ * EngineSettings.ChunkConfig.SizeZ);

                int minX = Mathf.Clamp(Mathf.FloorToInt(bounds.min.x) - pom.X - myVec52.X, 0, EngineSettings.ChunkConfig.MaskX);
                int minY = Mathf.Clamp(Mathf.FloorToInt(bounds.min.y) - myVec52.Y, 0, EngineSettings.ChunkConfig.SizeYTotal - 1);
                int minZ = Mathf.Clamp(Mathf.FloorToInt(bounds.min.z) - pom.Z - myVec52.Z, 0, EngineSettings.ChunkConfig.MaskZ);
                bMin = new Vector3Int(minX, minY, minZ);

                int maxX = Mathf.Clamp(Mathf.Clamp(bMin.X, 0, EngineSettings.ChunkConfig.MaskX), 0, EngineSettings.ChunkConfig.MaskX);
                int maxY = Mathf.Clamp(Mathf.Clamp(bMin.Y, 0, EngineSettings.ChunkConfig.SizeYTotal - 1), 0, EngineSettings.ChunkConfig.SizeYTotal-1);
                int maxZ = Mathf.Clamp(Mathf.Clamp(bMin.Z, 0, EngineSettings.ChunkConfig.MaskZ), 0, EngineSettings.ChunkConfig.MaskZ);
                bMax = new Vector3Int(maxX, maxY, maxZ);
            }

            // SIMD version
            Vector4i bMinv;
            Vector4i bMaxv;
            {
                Vector4f bMinf = new Vector4f(bounds.min.x, bounds.min.y, bounds.min.z, 0f);
                bMinv = bMinf.ConvertToIntTruncated();
                Vector4f bMaxf = new Vector4f(bounds.max.x, bounds.max.y, bounds.max.z, 0f);
                bMaxv = bMaxf.ConvertToInt();

                Vector4i pom = new Vector4i(positionX, 0, positionZ, 0)*Chunk.VecSize;
                bMinv -= pom;
                bMinv -= Chunk.Vec5;
                bMinv = bMinv.Max(Vector4i.Zero).Min(Chunk.VecSize1);
                    // Clamp to 0..size (x,z) or 0..height (y) respectively

                bMaxv -= pom;
                bMaxv += Chunk.Vec5;
                bMaxv = bMaxv.Max(Vector4i.Zero).Min(Chunk.VecSize1);
                    // Clamp to 0..size (x,z) or 0..height (y) respectively
            }

            Assert.AreEqual(bMin.X, bMinv.X);
            Assert.AreEqual(bMin.Y, bMinv.Y);
            Assert.AreEqual(bMin.Z, bMinv.Z);

            Assert.AreEqual(bMax.X, bMaxv.X);
            Assert.AreEqual(bMax.Y, bMaxv.Y);
            Assert.AreEqual(bMax.Z, bMaxv.Z);
        }
    }
}
#endif