using System;

namespace Engine.Scripts.Core.Chunks.States
{
    [Flags]
    public enum ChunkState : ushort
    {
        Idle = 0,
        
        Generate = 0x01,  //! Generated new chunk data

        SaveData = 0x04, //! Saving data to starage

        FinalizeData = 0x08, //! Chunk prepares for building vertices and serialization

        BuildVertices = 0x10, //! Prepare geometry
        BuildVerticesNow = 0x20, //! Prepare geometry with priority
        
        Remove = 0x40, //! Removal requested
        GenericWork = 0x80 //! Some generic work
    }
}
