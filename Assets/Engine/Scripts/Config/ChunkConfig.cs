using System;
using System.Runtime.Serialization;
using UnityEngine;

namespace Assets.Engine.Scripts.Config
{
    [DataContract]
    public class ChunkConfig: IEngineConfig
    {
        #region Configurable parameters

        [DataMember] public int Size { get; set; }
        [DataMember] public int StackSize { get; set; }

        #endregion

        #region Parameters generated based on configurable parameters

        public int SizeYTotal { get; private set; }
        public int Volume { get; private set; }
        public int VolumeTotal { get; private set; }
        public int LogSize { get; private set; }
        public int LogSize2 { get; private set; }
        public int Mask { get; private set; }
		public int MaskYTotal { get; private set; }

        #endregion

        internal ChunkConfig()
        {
            Size = 16;
            StackSize = 8;

            Init();
        }

        public void Init()
        {
            Volume = Size*Size*Size;
            VolumeTotal = Volume*StackSize;
            SizeYTotal = Size*StackSize;
            LogSize = Convert.ToInt32(Mathf.Log(Size, 2f));
            LogSize2 = LogSize + LogSize;
            Mask = Size-1;
			MaskYTotal = SizeYTotal-1;

            if(!Verify())
                Debug.LogError("Error in ChunkConfig");
        }

        public bool Verify()
        {
            if (!Mathf.IsPowerOfTwo(Size))
                return false;

            return true;
        }
    }
}
