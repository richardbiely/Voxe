﻿namespace Assets.Engine.Scripts.Common.Memory
{
    internal interface IArrayPool<T>
    {

        /// <summary>
        ///     Retrieves an array from the top of the pool
        /// </summary>
        void Pop(out T[] item);
        
        /// <summary>
        ///     Returns an array back to the pool
        /// </summary>
        void Push(ref T[] item);
    }
}
