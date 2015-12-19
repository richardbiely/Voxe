using UnityEngine;

namespace Assets.Engine.Scripts.Physics
{
    [RequireComponent( typeof( TileCollider ) )]
    public class TilePhysicsController : MonoBehaviour
    {
        private TileCollider m_boxCollider;

        public bool Grounded { get; private set; }

        void Awake()
        {
            m_boxCollider = GetComponent<TileCollider>();
        }

        /// <summary>
        /// Perform move step on object
        /// </summary>
        public void Move( Vector3 delta )
        {
            // decompose movement into X, Y, and Z
            // attempt movement in each direction

            Vector3 xMove = new Vector3( delta.x, 0f, 0f );
            Vector3 yMove = new Vector3( 0f, delta.y, 0f );
            Vector3 zMove = new Vector3( 0f, 0f, delta.z );

            DoMove( xMove );
            bool yHit = DoMove( yMove );
            DoMove( zMove );

            Grounded = ( yHit && delta.y < 0f );
        }

        /// <summary>
        /// Perform move step on object
        /// Adds gravity to input vector, returns new vector
        /// </summary>
        public Vector3 SimpleMove( Vector3 delta )
        {
            // decompose movement into X, Y, and Z
            // attempt movement in each direction

            Vector3 xMove = new Vector3( delta.x, 0f, 0f );
            Vector3 yMove = new Vector3( 0f, delta.y, 0f );
            Vector3 zMove = new Vector3( 0f, 0f, delta.z );

            DoMove( xMove * Time.deltaTime );
            bool hitY = DoMove( yMove * Time.deltaTime );
            DoMove( zMove * Time.deltaTime );

            Grounded = ( hitY && delta.y < 0f );

            if( hitY && !Mathf.Approximately( delta.y, 0f ) )
            {
                // must have hit the ground or bumped our head
                return new Vector3( delta.x, 0f, delta.z );
            }

            return delta + ( UnityEngine.Physics.gravity * Time.deltaTime );
        }

        private bool DoMove( Vector3 dir )
        {
            Vector3 lastPos = transform.position;
            transform.position += dir;

            if (!m_boxCollider.CollidesWithScene())
                return false;

            // hit something, revert position
            transform.position = lastPos;
            return true;
        }
    }
}