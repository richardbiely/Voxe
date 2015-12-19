using System;

namespace Assets.Engine.Scripts.Core.Chunks
{
    [Flags]
    public enum ChunkState : byte
    {
        Idle = 0,

#region Real states
        // Progress states
        Generate = 0x01,  //! Chunk section is generated
        GenerateBlueprints = 0x02, //! Chunk section is waiting for the blueprints to be generated
        FinalizeData = 0x04,
        BuildVertices = 0x08, //! Chunk section is building its vertex data
        Serialize = 0x10, //! Chunk is being serialized
        Remove = 0x20, //! Chunk is waiting for removal
        #endregion

        #region Virtual states
        // Special states
        Rebuild = 0x40, //! Virtual state used in external rebuild requests
#endregion
    }
}
