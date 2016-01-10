using System.Runtime.Serialization;
using UnityEngine;

namespace Assets.Engine.Scripts.Config
{
    [DataContract]
    public class WorldConfig: IEngineConfig
    {
        #region Configurable parameters

        [DataMember] public int VisibleRange { get; set; }
        [DataMember] public int CachedRange { get; set; }
        [DataMember] public bool Streaming { get; set; }
        [DataMember] public bool Infinite { get; set; }
        [DataMember] public bool OcclusionCulling { get; set; }

        #endregion

        #region Parameters generated based on configurable parameters

        public int MinDiff { get; private set; }

        public int MaxDiff { get; private set; }


        #endregion

        internal WorldConfig()
        {
            VisibleRange = 6;
            CachedRange = VisibleRange+1;
            Streaming = false;
            Infinite = true;
            OcclusionCulling = false;

            Init();
        }

        public void Init()
        {
            MinDiff = (CachedRange-VisibleRange)>>1;
            MaxDiff = MinDiff+VisibleRange;

            if(!Verify())
                Debug.LogError("Error in WorldConfig");
        }

        public bool Verify()
        {
            if (CachedRange>1 && VisibleRange>CachedRange)
                return false;

            return true;
        }
    }
}