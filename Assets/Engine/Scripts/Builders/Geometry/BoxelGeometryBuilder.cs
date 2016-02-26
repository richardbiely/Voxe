using Assets.Engine.Scripts.Builders.Block;
using Assets.Engine.Scripts.Common.DataTypes;
using Assets.Engine.Scripts.Core.Blocks;
using Assets.Engine.Scripts.Core.Chunks;
using Assets.Engine.Scripts.Utils;
using UnityEngine;
using UnityEngine.Assertions;
using RenderBuffer = Assets.Engine.Scripts.Rendering.RenderBuffer;

namespace Assets.Engine.Scripts.Builders.Geometry
{
    /// <summary>
    /// Generates a typical cubical voxel geometry for a chunk. Faces will be merged, however, to decrease its complexity.
    /// </summary>
    public class BoxelGeometryBuilder: AVoxelGeometryBuilder
    {
        public override void BuildMesh(
            Map map, RenderBuffer renderBuffer, int offsetX, int offsetY, int offsetZ,
            int minX, int maxX, int minY, int maxY, int minZ, int maxZ, int lod
            )
        {
            renderBuffer.Clear();

            BlockFace face = 0;

            int stepSize = 1 << lod;
            Assert.IsTrue(lod <= EngineSettings.ChunkConfig.LogSize); // LOD can't be bigger than chunk size
            int width = EngineSettings.ChunkConfig.Size >> lod;

            int[] mins = { minX >> lod, minY >> lod, minZ >> lod };
            int[] maxes = { maxX >> lod, maxY >> lod, maxZ >> lod };

            int[] x = { 0, 0, 0 }; // Relative position of a block
            int[] xx = { 0, 0, 0 }; // Relative position of a block after applying lod
            int[] q = { 0, 0, 0 }; // Direction in which we compare neighbors when building mask (q[d] is our current direction)
            int[] du = { 0, 0, 0 }; // Width in a given dimension (du[u] is our current dimension)
            int[] dv = { 0, 0, 0 }; // Height in a given dimension (dv[v] is our current dimension)

            BlockData[] mask = new BlockData[width * width];

            // Iterate over 3 dimensions. Once for front faces, once for back faces
            for (int dd = 0; dd < 2 * 3; dd++)
            {
                int d = dd % 3;
                int u = (d + 1) % 3;
                int v = (d + 2) % 3;

                x[0] = 0;
                x[1] = 0;
                x[2] = 0;

                q[0] = 0;
                q[1] = 0;
                q[2] = 0;
                q[d] = stepSize;

                // Determine which side we're meshing
                bool backFace = dd < 3;
                switch (dd)
                {
                    case 0: face = BlockFace.Left; break;
                    case 3: face = BlockFace.Right; break;

                    case 1: face = BlockFace.Bottom; break;
                    case 4: face = BlockFace.Top; break;

                    case 2: face = BlockFace.Back; break;
                    case 5: face = BlockFace.Front; break;
                }

                // Move through the dimension from front to back
                for (x[d] = mins[d] - 1; x[d] <= maxes[d];)
                {
                    // Compute the mask
                    int n = 0;

                    for (x[v] = 0; x[v] < mins[v]; x[v]++)
                    {
                        for (x[u] = 0; x[u] < width; x[u]++)
                            mask[n++] = BlockData.Air;
                    }

                    for (x[v] = mins[v]; x[v] <= maxes[v]; x[v]++)
                    {
                        for (x[u] = 0; x[u] < mins[u]; x[u]++)
                            mask[n++] = BlockData.Air;

                        for (x[u] = mins[u]; x[u] <= maxes[u]; x[u]++)
                        {
                            int realX = (x[0] << lod) + offsetX;
                            int realY = (x[1] << lod) + offsetY;
                            int realZ = (x[2] << lod) + offsetZ;

                            BlockData voxelFace0 = map.GetBlock(realX, realY, realZ);
                            BlockData voxelFace1 = map.GetBlock(realX + q[0], realY + q[1], realZ + q[2]);

                            mask[n++] = (voxelFace0.IsSolid() && voxelFace1.IsSolid())
                                            ? BlockData.Air
                                            : (backFace ? voxelFace1 : voxelFace0);
                        }

                        for (x[u] = maxes[u] + 1; x[u] < width; x[u]++)
                            mask[n++] = BlockData.Air;
                    }

                    for (x[v] = maxes[v] + 1; x[v] < width; x[v]++)
                    {
                        for (x[u] = 0; x[u] < width; x[u]++)
                            mask[n++] = BlockData.Air;
                    }

                    x[d]++;
                    n = 0;

                    // Build faces from the mask if it's possible
                    int j;
                    for (j = 0; j < width; j++)
                    {
                        int i;
                        for (i = 0; i < width;)
                        {
                            if (mask[n].IsEmpty())
                            {
                                i++;
                                n++;
                                continue;
                            }

                            BlockType type = mask[n].BlockType;

                            // Compute width
                            int w;
                            for (w = 1; i + w < width && mask[n + w].BlockType == type; w++)
                            {
                            }

                            // Compute height
                            bool done = false;
                            int k;
                            int h;
                            for (h = 1; j + h < width; h++)
                            {
                                for (k = 0; k < w; k++)
                                {
                                    if (
                                        mask[n + k + h * width].IsEmpty() ||
                                        mask[n + k + h * width].BlockType != type
                                        )
                                    {
                                        done = true;
                                        break;
                                    }
                                }

                                if (done)
                                    break;
                            }

                            // Determine whether we really want to build this face
                            // TODO: Skip bottom faces at the bottom of the world
                            bool buildFace = true;
                            if (buildFace)
                            {
                                // Prepare face coordinates and dimensions
                                x[u] = i;
                                x[v] = j;

                                xx[0] = x[0] << lod;
                                xx[1] = x[1] << lod;
                                xx[2] = x[2] << lod;

                                du[0] = du[1] = du[2] = 0;
                                dv[0] = dv[1] = dv[2] = 0;
                                du[u] = w << lod;
                                dv[v] = h << lod;

                                // Face vertices
                                Vector3Int v1 = new Vector3Int(
                                    xx[0], xx[1], xx[2]
                                    );
                                Vector3Int v2 = new Vector3Int(
                                    xx[0] + du[0], xx[1] + du[1], xx[2] + du[2]
                                    );
                                Vector3Int v3 = new Vector3Int(
                                    xx[0] + du[0] + dv[0], xx[1] + du[1] + dv[1], xx[2] + du[2] + dv[2]
                                    );
                                Vector3Int v4 = new Vector3Int(
                                    xx[0] + dv[0], xx[1] + dv[1], xx[2] + dv[2]
                                    );

                                // Face vertices transformed to world coordinates
                                // 0--1
                                // |  |
                                // |  |
                                // 3--2
                                Vector3[] vecs =
                                {
                                    new Vector3(
                                        (float)v4.X,
                                        (float)v4.Y+offsetY,
                                        (float)v4.Z
                                        ),
                                    new Vector3(
                                        (float)v3.X,
                                        (float)v3.Y+offsetY,
                                        (float)v3.Z
                                        ),
                                    new Vector3(
                                        (float)v2.X,
                                        (float)v2.Y+offsetY,
                                        (float)v2.Z
                                        ),
                                    new Vector3(
                                        (float)v1.X,
                                        (float)v1.Y+offsetY,
                                        (float)v1.Z
                                        )
                                };

                                // Build the face
                                IBlockBuilder builder = BlockDatabase.GetBlockBuilder(type);
                                builder.Build(renderBuffer, ref mask[n], face, backFace, ref vecs);
                            }

                            // Zero out the mask
                            int l;
                            for (l = 0; l < h; ++l)
                            {
                                for (k = 0; k < w; ++k)
                                {
                                    mask[n + k + l * width] = BlockData.Air;
                                }
                            }

                            i += w;
                            n += w;
                        }
                    }
                }
            }
        }
    }
}
