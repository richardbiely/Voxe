using Assets.Engine.Scripts.Builders;
using Assets.Engine.Scripts.Common.Threading;

namespace Assets.Engine.Scripts.Core
{
    public static class Globals
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

        // Mesh builder
        private static IMeshBuilder _sCubeMeshBuilder;

        public static IMeshBuilder CubeMeshBuilder
        {
            get
            {
                if (_sCubeMeshBuilder == null)
                    _sCubeMeshBuilder = new CubeMeshBuilder();
                return _sCubeMeshBuilder;
            }
        }
    }
}
