using System;

namespace Assets.Engine.Scripts.Common.Collections
{
    public class ObjectPoolAllocator<T> where T: class
    {
        public readonly Func<T, T> Action;
        public readonly T Arg;

        public ObjectPoolAllocator(Func<T, T> action)
        {
            Action = action;
            Arg = null;
        }

        public ObjectPoolAllocator(Func<T, T> action, T arg)
        {
            Action = action;
            Arg = arg;
        }
    }
}
