using UnityEngine;

namespace Assets.Engine.Scripts.Core.Blocks
{
    public class BlockInfo
    {
        private readonly bool m_isSolid;
        private readonly Color32 m_color;

        public bool IsSolid { get { return m_isSolid; } }
        public Color32 Color { get { return m_color; } }

        public BlockInfo(bool isSolid, Color32 color)
        {
            m_isSolid = isSolid;
            m_color = Color;
        }
    }
}