using System;

namespace Assets.Engine.Scripts.Common.Threading
{
    public struct ThreadItem
    {
        public readonly Action<object> Action;
        public readonly object Arg;

        public ThreadItem(Action<object> action, object arg)
        {
            Action = action;
            Arg = arg;
        }
    }
}