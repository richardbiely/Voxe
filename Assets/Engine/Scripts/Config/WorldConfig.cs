using System.Runtime.Serialization;

namespace Assets.Engine.Scripts.Config
{
    [DataContract]
    public class WorldConfig: IEngineConfig
    {
        #region Configurable parameters
        
        [DataMember] public bool Streaming { get; set; }
        [DataMember] public bool Infinite { get; set; }
        [DataMember] public bool OcclusionCulling { get; set; }

        #endregion

        internal WorldConfig()
        {
            Streaming = false;
            Infinite = true;
            OcclusionCulling = false;

            Init();
        }

        public void Init()
        {
        }

        public bool Verify()
        {
            return true;
        }
    }
}