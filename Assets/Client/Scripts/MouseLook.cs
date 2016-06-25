using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

namespace Client.Scripts
{
    /// MouseLook rotates the transform based on the mouse delta.
    /// Minimum and Maximum values can be used to constrain the possible rotation

    /// To make an FPS style character:
    /// - Create a capsule.
    /// - Add the MouseLook script to the capsule.
    ///   -> Set the mouse look to use LookX. (You want to only turn character but not tilt it)
    /// - Add FPSInputController script to the capsule
    ///   -> A CharacterMotor and a CharacterController component will be automatically added.

    /// - Create a camera. Make the camera a child of the capsule. Reset it's transform.
    /// - Add a MouseLook script to the camera.
    ///   -> Set the mouse look to use LookY. (You want the camera to tilt up and down like a head. The character already turns.)
    [AddComponentMenu("Camera-Control/Mouse Look")]
    public class MouseLook : MonoBehaviour {

        public enum RotationAxes { MouseXAndY = 0, MouseX = 1, MouseY = 2 }
        public RotationAxes Axes = RotationAxes.MouseXAndY;
        public float SensitivityX = 15F;
        public float SensitivityY = 15F;

        public float MinimumX = -360F;
        public float MaximumX = 360F;

        public float MinimumY = -60F;
        public float MaximumY = 60F;

        float m_rotationY = 0F;

        void Awake() {
            SensitivityX = PlayerPrefs.GetFloat("SenstivityX", 15);
            SensitivityY = PlayerPrefs.GetFloat("SensitivityY", 15);
        }

        void Update()
        {
            switch (Axes)
            {
                case RotationAxes.MouseXAndY:
                    float rotationX = transform.localEulerAngles.y + CrossPlatformInputManager.GetAxis("Mouse X") * SensitivityX;

                    m_rotationY += CrossPlatformInputManager.GetAxis("Mouse Y") * SensitivityY;
                    m_rotationY = Mathf.Clamp(m_rotationY, MinimumY, MaximumY);

                    transform.localEulerAngles = new Vector3(-m_rotationY, rotationX, 0);
                    break;
                case RotationAxes.MouseX:
                    transform.Rotate(0, CrossPlatformInputManager.GetAxis("Mouse X") * SensitivityX, 0);
                    break;
                default:
                    m_rotationY += CrossPlatformInputManager.GetAxis("Mouse Y") * SensitivityY;
                    m_rotationY = Mathf.Clamp(m_rotationY, MinimumY, MaximumY);

                    transform.localEulerAngles = new Vector3(-m_rotationY, transform.localEulerAngles.y, 0);
                    break;
            }
        }

        void Start() {
            // Make the rigid body not change rotation
            if(GetComponent<Rigidbody>())
                GetComponent<Rigidbody>().freezeRotation = true;
        }
    }
}