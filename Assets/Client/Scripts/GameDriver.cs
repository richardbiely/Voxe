using System;
using System.Collections;
using Engine.Scripts.Core.Chunks.Managers;
using UnityEngine;

namespace Client.Scripts
{
    public class GameDriver: MonoBehaviour
    {
        public Map GameMap;

        private bool m_stop;

        private void Start()
        {
            //StartCoroutine(OnActivateGC());
        }

        public IEnumerator OnActivateGC()
        {
            while (!m_stop)
            {
                GC.Collect();
                yield return new WaitForSeconds(1f);
            }
        }

        private void FixedUpdate()
        {
            GameMap.ProcessChunks();
        }

        private void OnDestroy()
        {
            m_stop = true;
            GameMap.Shutdown();
        }
    }
}