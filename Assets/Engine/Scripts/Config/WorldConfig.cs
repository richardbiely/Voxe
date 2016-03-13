using System.Runtime.Serialization;

namespace Assets.Engine.Scripts.Config
{
    [DataContract]
    public class WorldConfig: IEngineConfig
    {
        #region Configurable parameters
        
        [DataMember] public bool Streaming { get; set; }
        [DataMember] public bool Infinite { get; set; }

        #endregion

        internal WorldConfig()
        {
            Streaming = false;
            Infinite = true;

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