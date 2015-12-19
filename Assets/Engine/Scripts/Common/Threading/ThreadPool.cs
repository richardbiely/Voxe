using System;
using System.Collections.Generic;
using System.Threading;

namespace Assets.Engine.Scripts.Common.Threading
{
    public class ThreadPool
    {
        // Priority queue types
        public enum EPriority
        {
            High = 0,
            Normal = 1,
            Low = 2,
        }

        private bool m_stop;
        private bool m_started;

        // A list of actions waiting to be run async
        private readonly List<ThreadItem>[] m_items =
        {
            new List<ThreadItem>(), // Queue for high priority tasks
            new List<ThreadItem>(), // Queue for normal priority tasks
            new List<ThreadItem>()  // Queue for low priority tasks
        };

        private readonly object m_lock = new object();

        public ThreadPool()
        {
            m_stop = false;
            m_started = false;
        }

        ~ThreadPool()
        {
            m_stop = true;
            Monitor.PulseAll(m_lock);
        }

        public void Start(int threadCnt = 0)
        {
            if (m_started)
                return;
            m_started = true;

            // If the number of threads is not correctly specified, create as many as possible minus one (taking
            // all available core is not effective - there's still the main thread we should not forget).
            // Allways create at least one thread, however.
            if (threadCnt<=0)
                threadCnt = System.Math.Max(Environment.ProcessorCount - 1, 1);

            Thread[] threads = new Thread[threadCnt];
            for (int i = 0; i < threads.Length; i++)
            {
                // Create the threads
                threads[i] = new Thread(() =>
                {
                    while (true)
                    {
                        ThreadItem item;

                        lock (m_lock)
                        {
                            // Wait for work
                            int j = 0;
                            while (m_stop == false)
                            {
                                // !TODO: Priority system has to be a great deal more inteligent than just this...
                                // Find tasks in queues
                                for (j = 0; j < m_items.Length && m_items[j].Count == 0; j++)
                                {
                                }

                                // Sleep if all queues are empty
                                if (j == m_items.Length)
                                    Monitor.Wait(m_lock);
                                else
                                    break;
                            }

                            if (m_stop)
                                return;

                            item = m_items[j][0];
                            m_items[j].RemoveAt(0);
                        }

                        // Exectute the action
                        // Note, it's up to action to provide exception handling
                        item.Action(item.Arg);
                    }
                })
                { IsBackground = true };

                // Start the thread
                threads[i].Start();
            }
        }

        public void AddItem(Action<object> action, EPriority priority = EPriority.Normal)
        {
            // Do not allow to an invalid task to the queue
            if (m_stop)
                return;

            lock (m_lock)
            {
                // TODO: Incorporate priority info into ThreadItem somehow
                m_items[(int)priority].Add(new ThreadItem(action, null));
                Monitor.Pulse(m_lock);
            }
        }

        public void AddItem(Action<object> action, object arg, EPriority priority = EPriority.Normal)
        {
            // Do not allow to an invalid task to the queue
            if (m_stop)
                return;

            lock (m_lock)
            {
                m_items[(int) priority].Add(new ThreadItem(action, arg));
                Monitor.Pulse(m_lock);
            }            
        }

        public int Size
        {
            get
            {
                int sum = 0;
                for (int i = 0; i<m_items.Length; i++)
                {
                    var t = m_items[i];
                    sum += t.Count;
                }
                return sum;
            }
        }
    }
}
