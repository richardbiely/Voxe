using System;
using System.Runtime.Serialization;
using UnityEngine;

namespace Assets.Engine.Scripts.Config
{
    [DataContract]
    public class ChunkConfig: IEngineConfig
    {
        #region Configurable parameters

        [DataMember] public int SizeX { get; set; }
        [DataMember] public int SizeY { get; set; }
        [DataMember] public int SizeZ { get; set; }
        [DataMember] public int StackSize { get; set; }

        #endregion

        #region Parameters generated based on configurable parameters

        public int SizeYTotal { get; private set; }
        public int Volume { get; private set; }
        public int VolumeTotal { get; private set; }
        public int LogSizeX { get; private set; }
        public int LogSizeX2 { get; private set; }
        public int LogSizeY { get; private set; }
        public int LogSizeZ { get; private set; }
        public int MaskX { get; private set; }
        public int MaskY { get; private set; }
        public int MaskZ { get; private set; }


        #endregion

        internal ChunkConfig()
        {
            SizeX = SizeY = SizeZ = 8;
            StackSize = 16;

            Init();
        }

        public void Init()
        {
            Volume = SizeX*SizeY*SizeZ;
            VolumeTotal = Volume*StackSize;
            SizeYTotal = SizeY*StackSize;
            LogSizeX = Convert.ToInt32(Mathf.Log(SizeX, 2f));
            LogSizeX2 = 2*LogSizeX;
            LogSizeY = Convert.ToInt32(Mathf.Log(SizeY, 2f));
            LogSizeZ = Convert.ToInt32(Mathf.Log(SizeZ, 2f));
            MaskX = SizeX-1;
            MaskY = SizeY-1;
            MaskZ = SizeZ-1;

            if(!Verify())
                Debug.LogError("Error in ChunkConfig");
        }

        public bool Verify()
        {
            if (!Mathf.IsPowerOfTwo(SizeX))
                return false;
            if (!Mathf.IsPowerOfTwo(SizeX))
                return false;
            if (!Mathf.IsPowerOfTwo(SizeZ))
                return false;

            return true;
        }
    }
}
