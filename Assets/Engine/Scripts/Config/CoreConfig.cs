﻿using System.Runtime.Serialization;
using UnityEngine;

namespace Engine.Scripts.Config
{
    [DataContract]
    public class CoreConfig: IEngineConfig
    {
        //! If enabled, voxel operations are executed in separate threads
        [DataMember] public bool Mutlithreading { get; set; }
        //! If enabled, IO-related tasks are executed on a separate thread
        [DataMember] public bool IOThread { get; set; }
        [DataMember] public bool OcclusionCulling { get; set; }

        internal CoreConfig()
        {
            Mutlithreading = true;
            IOThread = true;
            OcclusionCulling = false;

            if (!Verify())
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