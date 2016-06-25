using System.Collections.Generic;
using Engine.Scripts.Common.Threading;

namespace Engine.Scripts.Core.Threading
{
    public static class IOPoolManager
    {
        private static readonly List<TaskPoolItem> WorkItems = new List<TaskPoolItem>();

        public static void Add(TaskPoolItem action)
        {
            WorkItems.Add(action);
        }

        public static void Commit()
        {
            // Commit all the work we have
            if (EngineSettings.CoreConfig.IOThread)
            {
                TaskPool pool = Globals.IOPool;

                for (int i = 0; i < WorkItems.Count; i++)
                {
                    var item = WorkItems[i];
                    pool.AddItem(item.Action, item.Arg);
                }
            }
            else
            {
                for (int i = 0; i<WorkItems.Count; i++)
                {
                    var item = WorkItems[i];
                    item.Action(item.Arg);
                }
            }

            // Remove processed work items
            WorkItems.Clear();
        }
    }
}
