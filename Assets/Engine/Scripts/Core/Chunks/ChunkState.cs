using System;

namespace Assets.Engine.Scripts.Core.Chunks
{
    [Flags]
    public enum ChunkState : byte
    {
        Idle = 0,

        // Progress states
        Generate = 0x01,  //! Chunk is generated
#if ENABLE_BLUEPRINTS
        GenerateBlueprints = 0x02, //! Chunk is waiting for the blueprints to be generated
#endif
        FinalizeData = 0x04, //! Chunk prepares for building vertices and serialization
        BuildVertices = 0x08, //! Chunk is building its vertex data
        Serialize = 0x10, //! Chunk is being serialized
        Remove = 0x20, //! Chunk is waiting for removal
    }
}
