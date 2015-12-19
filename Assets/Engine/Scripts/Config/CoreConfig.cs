using System.Runtime.Serialization;
using UnityEngine;

namespace Assets.Engine.Scripts.Config
{
    [DataContract]
    public class CoreConfig: IEngineConfig
    {
        //! If enabled, voxel operations are executed in separate threads
        [DataMember] public bool Mutlithreading { get; set; }
        //! If enabled, IO-related tasks are executed on a separate thread
        [DataMember] public bool IOThread { get; set; }

        internal CoreConfig()
        {
            Mutlithreading = true;
            IOThread = true;

            if(!Verify())
                Debug.LogError("Error in CoreConfig");
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