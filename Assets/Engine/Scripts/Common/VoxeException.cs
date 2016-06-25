using System;

namespace Engine.Scripts.Common
{
    public class VoxeException : Exception
    {
        public VoxeException(string message, Exception inner) : base(message, inner)
        {
        }

        public VoxeException(string message) : base(message)
        {
        }
    }
}
