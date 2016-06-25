using Engine.Scripts.Common.DataTypes;

namespace Engine.Scripts.Physics
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
    }
}