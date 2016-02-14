using System;
using System.Collections;
using Assets.Engine.Scripts.Core;
using Assets.Engine.Scripts.Provider;
using UnityEngine;

namespace Assets.Client.Scripts
{
    public class GameDriver: MonoBehaviour
    {
        public Map GameMap;
        public Transform ViewerCamera;

        private ChunkProvider m_gameServer;
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
            GameMap.UpdateMap();
        }

        private void OnDestroy()
        {
            m_stop = true;
            GameMap.Shutdown();
        }
    }
}