using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

namespace Assets.Client.Scripts
{
    public class FreeCameraMover : MonoBehaviour
    {
        public float MoveSpeed = 3f;

        void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
        }

        void Update()
        {
            float mult = 1f;
            if( Input.GetKey( KeyCode.LeftShift ) )
            {
                mult = 2f;
            }

            transform.Translate( Vector3.right * CrossPlatformInputManager.GetAxis( "Horizontal" ) * mult * MoveSpeed * Time.deltaTime );
            transform.Translate( Vector3.forward * CrossPlatformInputManager.GetAxis( "Vertical" ) * mult * MoveSpeed * Time.deltaTime );
        }
    }
}