using Engine.Scripts.Common;
using Engine.Scripts.Core.Threading;
using UnityEngine;

namespace Engine.Scripts
{
    [AddComponentMenu("Voxe/Singleton/GlobalObjects")]
    public sealed class GlobalObjects : MonoSingleton<GlobalObjects>
    {
        void Awake()
        {
            Globals.InitWorkPool();
            Globals.InitIOPool();
            Globals.InitMemPools();
            Globals.InitWatch();

            Profiler.maxNumberOfSamplesPerFrame = Mathf.Max(Profiler.maxNumberOfSamplesPerFrame, 1000000);
        }

        void Update()
        {
            IOPoolManager.Commit();
            WorkPoolManager.Commit();
        }
    }
}
