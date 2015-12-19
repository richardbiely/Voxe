using System;
using System.Collections;
using Assets.Engine.Scripts;
using Assets.Engine.Scripts.Common.DataTypes;
using Assets.Engine.Scripts.Core;
using Assets.Engine.Scripts.Physics;
using Assets.Engine.Scripts.Provider;
using UnityEngine;

namespace Assets.Client.Scripts
{
    public class GameDriver: MonoBehaviour
    {
        public TileCharacterController CharacterController;
        public Map GameMap;
        public Transform ViewerCamera;

        private LocalChunkProvider m_gameServer;
        private bool m_stop;

        private void Start()
        {
            m_gameServer = new LocalChunkProvider(GameMap);

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
            int posX = Mathf.FloorToInt(ViewerCamera.position.x) >> EngineSettings.ChunkConfig.LogSizeX;
            int posZ = Mathf.FloorToInt(ViewerCamera.position.z) >> EngineSettings.ChunkConfig.LogSizeZ;

            m_gameServer.LocalMap.ViewerChunkPos = new Vector2Int(posX, posZ);
            m_gameServer.Update();
        }

        private void OnDestroy()
        {
            m_stop = true;
            m_gameServer.Shutdown();
        }
    }
}