using Assets.Engine.Scripts.Core;
using UnityEngine;

namespace Assets.Engine.Scripts.Physics
{
    public class TileCollider : MonoBehaviour
    {
        public Bounds BoundingBox;

        void OnDrawGizmosSelected()
        {
            Bounds bounds = TransformBounds();
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }

        /// <summary>
        /// Returns whether the collider intersects with the map
        /// </summary>
        public bool CollidesWithScene()
        {
            // Only check chunks in close proximity to the world center
            return Map.Current.TestBlocksAABB(TransformBounds(), -1);
        }

        private Bounds TransformBounds()
        {
            Bounds bounds = BoundingBox;
            bounds.center += transform.position;
            bounds.extents *= 0.5f;
            return bounds;
        }
    }
}