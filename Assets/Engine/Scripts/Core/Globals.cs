using Assets.Engine.Scripts.Builders.Mesh;
using Assets.Engine.Scripts.Common.Threading;

namespace Assets.Engine.Scripts.Core
{
    public static class Globals
    {
        // Thread pool
        private static ThreadPool s_threadPool;
        public static ThreadPool WorkPool
        {
            get
            {
                if (s_threadPool == null)
                {
                    s_threadPool = new ThreadPool();
                    s_threadPool.Start();
                }
                return s_threadPool;
            }
        }

        // Task pool for IO-related tasks
        private static TaskPool s_IOPool;
        public static TaskPool IOPool
        {
            get
            {
                if (s_IOPool == null)
                {
                    s_IOPool = new TaskPool();
                    s_IOPool.Start();
                }
                return s_IOPool;
            }
        }

        // Mesh builder
        private static IMeshBuilder s_cubeMeshBuilder;
        public static IMeshBuilder CubeMeshBuilder
        {
            get
            {
                if (s_cubeMeshBuilder == null)
                    s_cubeMeshBuilder = new CubeMeshBuilder();
                return s_cubeMeshBuilder;
            }
        }

        // Global object pools
        private static GlobalPools s_pools;
        public static GlobalPools Pools
        {
            get
            {
                if (s_pools == null)
                    s_pools = new GlobalPools();
                return s_pools;
            }
        }
    }
}
