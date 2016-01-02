using System;
using Assets.Engine.Scripts.Common;
using UnityEngine.Assertions;

namespace Assets.Engine.Scripts.Core.Chunks
{
    public class ChunkEvent: IEventBase<ChunkState>
    {
        //! List of subscribers. Fixed-size with 4 items
        protected readonly ChunkEvent[] Subscribers;
        //! Index of greatest usable subscriber index in subscriber array
        protected int SubscribersCurr { get; private set; }

        protected ChunkEvent(int subscribers)
        {
            Subscribers = Helpers.CreateArray1D<ChunkEvent>(subscribers);

            Reset();
        }

        protected void Reset()
        {
            SubscribersCurr = 0;
            for (int i = 0; i<Subscribers.Length; i++)
                Subscribers[i] = null;
        }

        public bool Register(IEventBase<ChunkState> section, bool registerListener)
        {
            // The idea here is to register each neighbor on the main thread. Once a section gathers
            // 4 notifications from its' neighbors chunk generation can
            // be started.

            // Expect the input to be correct
            //Assert.IsTrue(section!=null);
            
            // Register
            if (registerListener)
            {
                // Make sure this section is not registered yet
                ChunkEvent newSection = (ChunkEvent)section;
                int firstNullIndex = -1;
                for (int i = 0; i<Subscribers.Length; i++)
                {
                    ChunkEvent s = Subscribers[i];
                    if (s==null)
                    {
                        firstNullIndex = i;
                        continue;
                    }

                    if (s==newSection)
                        return false;
                }

                //Assert.IsTrue(section != this, "Trying to register the section to itself");
                //Assert.IsTrue(SubscribersCurr < Subscribers.Length, string.Format("ChunkEvent.Register: Condition {0} < {1} not met", SubscribersCurr, Subscribers.Length));
                
                // New registration, remember the subscriber and increase subscriber count
                Subscribers[firstNullIndex] = (ChunkEvent)section;

                ++SubscribersCurr;
                if (SubscribersCurr!=Subscribers.Length)
                    return false;
            }
            // Unregister
            else
            {
                // Only unregister already registered sections
                int i;
                for (i = 0; i < Subscribers.Length; i++)
                {
                    var item = Subscribers[i];
                    if (item != section)
                        continue;

                    break;
                }
                if (i >= Subscribers.Length)
                    return false;

                //Assert.IsTrue(section != this, "Trying to unregister the section from itself");
                //Assert.IsTrue(SubscribersCurr > 0);

                // Unregister
                --SubscribersCurr;
                Subscribers[i] = null;
                if (SubscribersCurr!=0)
                    return false;
            }

            OnRegistered(registerListener);
            return true;
        }

		public void NotifyAll(ChunkState state)
        {
            // Notify each registered listener
            for (int i = 0; i < Subscribers.Length; i++)
            {
                if (Subscribers[i]!=null)
					Subscribers[i].OnNotified(state);
            }
        }

		public void NotifyOne(IEventBase<ChunkState> receiver, ChunkState state)
        {
            // Notify one of the listeners
            for (int i = 0; i < Subscribers.Length; i++)
            {
                var listener = Subscribers[i];
                if (listener==null || listener!=receiver)
                    continue;

				Subscribers[i].OnNotified(state);
                break;
            }
        }

        public virtual void OnRegistered(bool registerListener)
        {
            throw new NotImplementedException();
        }

        public virtual void OnNotified(ChunkState state)
        {
            throw new NotImplementedException();
        }
    }
}
