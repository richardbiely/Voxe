using System.Collections.Generic;
using Assets.Engine.Scripts.Common.Threading;

namespace Assets.Engine.Scripts.Core.Threading
{
    public static class WorkPoolManager
    {
        private static readonly List<ThreadItem> WorkItems = new List<ThreadItem>();

        public static void Add(ThreadItem action)
        {
            WorkItems.Add(action);
        }

        public static void Commit()
        {
            // Commit all the work we have
            if (EngineSettings.CoreConfig.Mutlithreading)
            {
                for (int i = 0; i<WorkItems.Count; i++)
                {
                    var item = WorkItems[i];
                    if(item.ThreadID>=0)
                        Globals.WorkPool.AddItem(item.ThreadID, item.Action, item.Arg);
                    else
                        Globals.WorkPool.AddItem(item.Action, item.Arg);
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
