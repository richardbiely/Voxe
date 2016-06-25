using System;
using Engine.Scripts.Core.Chunks;
using UnityEngine;

namespace Engine.Scripts.Generators
{
    public abstract class AChunkGenerator: MonoBehaviour, IChunkGenerator
    {
        public virtual void Generate(Chunk chunk)
        {
            throw new NotImplementedException();
        }
    }
}
