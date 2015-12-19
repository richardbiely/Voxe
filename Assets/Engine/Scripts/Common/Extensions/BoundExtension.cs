using UnityEngine;

namespace Assets.Engine.Scripts.Common.Extensions
{
    public static class BoundExtension
    {
        public static bool Contains(this Bounds bounds, Bounds target)
        {
            return bounds.Contains(target.min) && bounds.Contains(target.max);
        }
    }
}
