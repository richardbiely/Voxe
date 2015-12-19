using Assets.Engine.Scripts.Common.Threading;

namespace Assets.Engine.Scripts.Core.Threading
{
    static class Core
    {
        // Thread pool
        private static ThreadPool _sThreadPool;
        public static ThreadPool WorkPool
        {
            get
            {
                if (_sThreadPool == null)
                {
                    _sThreadPool = new ThreadPool();
                    _sThreadPool.Start();
                }
                return _sThreadPool;
            }
        }

        // Task pool for IO-related tasks
        private static TaskPool _sIOPool;
        public static TaskPool IOPool
        {
            get
            {
                if (_sIOPool == null)
                {
                    _sIOPool = new TaskPool();
                    _sIOPool.Start();
                }
                return _sIOPool;
            }
        }
    }
}
