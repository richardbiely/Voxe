using System;
using Assets.Engine.Scripts.Core.Blocks;
using Assets.Engine.Scripts.Core.Chunks;
using UnityEngine;

namespace Assets.Engine.Scripts.Generators
{
    public abstract class AChunkGenerator: MonoBehaviour, IChunkGenerator
    {
        public virtual void Generate(Chunk chunk)
        {
            throw new NotImplementedException();
        }
    }
}
