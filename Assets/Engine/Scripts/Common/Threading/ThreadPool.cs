using System;
using System.Collections.Generic;
using System.Threading;

namespace Assets.Engine.Scripts.Common.Threading
{
    public class ThreadPool
    {
        private bool m_stop;
        private bool m_started;

        // A list of actions waiting to be run async
        private readonly Queue<ThreadItem> m_items = new Queue<ThreadItem>();

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
            if (threadCnt <= 0)
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
                            while (m_stop == false)
                            {
                                // Sleep if all queues are empty
                                if (m_items.Count == 0)
                                    Monitor.Wait(m_lock);
                                else
                                    break;
                            }

                            if (m_stop)
                                return;

                            item = m_items.Dequeue();
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

        public void AddItem(Action<object> action)
        {
            // Do not allow to an invalid task to the queue
            if (m_stop)
                return;

            lock (m_lock)
            {
                m_items.Enqueue(new ThreadItem(action, null));
                Monitor.Pulse(m_lock);
            }
        }

        public void AddItem(Action<object> action, object arg)
        {
            // Do not allow to an invalid task to the queue
            if (m_stop)
                return;

            lock (m_lock)
            {
                m_items.Enqueue(new ThreadItem(action, arg));
                Monitor.Pulse(m_lock);
            }
        }

        public int Size
        {
            get
            {
                return m_items.Count;
            }
        }
    }
}
