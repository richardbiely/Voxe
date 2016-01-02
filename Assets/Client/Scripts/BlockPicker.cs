using Assets.Engine.Scripts.Core;
using Assets.Engine.Scripts.Physics;
using UnityEngine;
using Assets.Engine.Scripts.Core.Blocks;

namespace Assets.Client.Scripts
{
    public class BlockPicker : MonoBehaviour
    {
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
            if (Map.Current.Raycast(ray, PickDistance, out hit))
            {
                m_cursorTransform.position = hit.HitBlock + Vector3.one * 0.5f;
                m_cursorTransform.rotation = Quaternion.identity;
                CursorRenderer.enabled = true;

                if (Input.GetMouseButtonDown(0))
                {
                    //Map.Current.DamageBlock(hit.HitBlock.X, hit.HitBlock.Y, hit.HitBlock.Z, 16);
					Map.Current.SetBlock(BlockData.Air, hit.HitBlock.X, hit.HitBlock.Y, hit.HitBlock.Z);
                }
            }
            else
            {
                //CursorRenderer.enabled = false;
            }
        }
    }
}