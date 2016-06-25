using System.Diagnostics;
using Engine.Scripts.Builders.Mesh;
using Engine.Scripts.Common.Threading;
using Engine.Scripts.Core.Pooling;

namespace Engine.Scripts
{
    public static class Globals
    {
        // Thread pool
        public static ThreadPool WorkPool { get; private set; }

        public static void InitWorkPool()
        {
            if (WorkPool == null)
            {
                WorkPool = new ThreadPool();
                WorkPool.Start();
            }
        }

        // Task pool for IO-related tasks
        public static TaskPool IOPool { get; private set; }

        public static void InitIOPool()
        {
            if (IOPool == null)
            {
                IOPool = new TaskPool();
                IOPool.Start();
            }
        }

        // Render geometry mesh builder
        private static readonly IMeshGeometryBuilder s_cubeMeshGeometryBuilder = new CubeMeshGeometryBuilder();
        public static IMeshGeometryBuilder CubeMeshGeometryBuilder
        {
            get
            {
                return s_cubeMeshGeometryBuilder;
            }
        }

        // Global object pools
        public static GlobalPools MemPools { get; private set; }

        public static void InitMemPools()
        {
            if (MemPools == null)
                MemPools = new GlobalPools();
        }

        // Global stop watch
        public static Stopwatch Watch { get; private set; }
        public static void InitWatch()
        {
            if (Watch == null)
            {
                Watch = new Stopwatch();
                Watch.Start();
            }
        }
    }
}
