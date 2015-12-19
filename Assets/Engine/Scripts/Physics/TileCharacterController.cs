using System;
using UnityEngine;

namespace Assets.Engine.Scripts.Physics
{
    [RequireComponent( typeof( TilePhysicsController ) )]
    public class TileCharacterController : MonoBehaviour
    {
        public float MoveSpeed = 5f;
        public float JumpSpeed = 8f;

        [NonSerialized]
        public Vector3 InputMove;

        [NonSerialized]
        public bool InputJump;

        private TilePhysicsController m_physController;

        private float m_yVel = 0f;

        void Awake()
        {
            m_physController = GetComponent<TilePhysicsController>();
        }

        void FixedUpdate()
        {
            Vector3 moveDir = new Vector3( InputMove.x * MoveSpeed, m_yVel, InputMove.z * MoveSpeed );

            m_yVel = m_physController.SimpleMove( moveDir ).y;

            if (!m_physController.Grounded)
                return;

            if( InputJump )
            {
                m_yVel = JumpSpeed;
            }
        }
    }
}