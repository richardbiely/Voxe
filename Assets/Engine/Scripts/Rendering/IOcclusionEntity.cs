using UnityEngine;

namespace Assets.Engine.Scripts.Rendering
{
    public interface IOcclusionEntity: IRasterizationEntity
    {
        Bounds Bounds { set; get; }
        bool Visible { set; get; }

        bool IsOccluder();
    }
}
