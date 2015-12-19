using Assets.Engine.Scripts.Common.DataTypes;
using UnityEngine;

namespace Assets.Engine.Scripts.Physics
{
    /// <summary>
    /// Represents data about a raycast performed against voxels
    /// </summary>
    public struct TileRaycastHit
    {
        /// <summary>
        /// The world position of the block hit by the raycast
        /// </summary>
        public Vector3Int HitBlock;

        /// <summary>
        /// The normal of the block face hit by the raycast
        /// </summary>
        public Vector3 HitFace;
    }
}