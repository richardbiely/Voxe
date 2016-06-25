using UnityEngine;

namespace Engine.Scripts.Rendering
{
    public interface IOcclusionEntity: IRasterizationEntity
    {
        Bounds GeometryBounds { set; get; }
        bool Visible { set; get; }

        bool IsOccluder();
    }
}
