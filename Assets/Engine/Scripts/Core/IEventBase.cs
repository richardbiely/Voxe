namespace Assets.Engine.Scripts.Core
{
	public interface IEventBase<TEvent>
	{
        //! Registers caller for receiving notification from parent
        bool Register(IEventBase<TEvent> neighbor, bool registerListener);
        //! Returns true if an object is registered
        bool IsRegistered();
        //! Notifies subscribers about something (implementation specific)	
        void NotifyAll(TEvent evt);
		//! Notifies one specific subscriber
		void NotifyOne(IEventBase<TEvent> receiver, TEvent evt);

		void OnRegistered(bool registerListener);
		void OnNotified(TEvent evt);
	}
}
