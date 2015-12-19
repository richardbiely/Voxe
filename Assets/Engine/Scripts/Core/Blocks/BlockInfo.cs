using Assets.Engine.Scripts.Physics;
using UnityEngine;

namespace Assets.Engine.Scripts.Core.Blocks
{
    public class BlockInfo
    {
        private readonly bool m_isSolid;
        private readonly PhysicsType m_physics;
        private readonly Color32 m_color;

        public bool IsSolid { get { return m_isSolid; } }
        public PhysicsType Physics { get { return m_physics; } }
        public Color32 Color { get { return m_color; } }

        public BlockInfo( bool isSolid, PhysicsType physics, Color32 color )
        {
            m_isSolid = isSolid;
            m_physics = physics;
            m_color = Color;
        }
    }
}