using System.Collections.Generic;
using Assets.Engine.Scripts.Common.Threading;

namespace Assets.Engine.Scripts.Core.Threading
{
    public static class IOPoolManager
    {
        private static readonly List<ThreadItem> WorkItems = new List<ThreadItem>();

        public static void Add(ThreadItem action)
        {
            WorkItems.Add(action);
        }

        public static void Commit()
        {
            // Commit all the work we have
            if (EngineSettings.CoreConfig.IOThread)
            {
                for (int i = 0; i<WorkItems.Count; i++)
                {
                    var item = WorkItems[i];
                    Core.IOPool.AddItem(item.Action, item.Arg);
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

            // Clear after all work items are processed
            WorkItems.Clear();
        }
    }
}
