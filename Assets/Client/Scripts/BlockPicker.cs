using Assets.Engine.Scripts.Physics;
using UnityEngine;
using Assets.Engine.Scripts.Core.Blocks;
using Assets.Engine.Scripts.Core.Chunks;

namespace Assets.Client.Scripts
{
    public class BlockPicker : MonoBehaviour
    {
        public Map LocalMap;
        public Renderer CursorRenderer;
        private Transform m_cursorTransform;

        public float PickDistance = 10f;

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            m_cursorTransform = CursorRenderer.transform;
            m_cursorTransform.parent = null;
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
                Cursor.lockState = CursorLockMode.Locked;

            TileRaycastHit hit;
            Ray ray = new Ray(transform.position, transform.forward);
            if (LocalMap.Raycast(ray, PickDistance, out hit))
            {
                Vector3 size = new Vector3(
                    1 << LocalMap.VoxelLogScaleX,
                    1 << LocalMap.VoxelLogScaleY,
                    1 << LocalMap.VoxelLogScaleZ
                    );

                m_cursorTransform.position = hit.HitBlock + size*0.5f;
                m_cursorTransform.localScale = size*1.01f;
                m_cursorTransform.rotation = Quaternion.identity;
                CursorRenderer.enabled = true;

                if (Input.GetMouseButtonDown(0))
                {
                    LocalMap.SetBlock(BlockData.Air, hit.HitBlock.X, hit.HitBlock.Y, hit.HitBlock.Z);
                }
            }
            else
            {
                CursorRenderer.enabled = false;
            }
        }
    }
}