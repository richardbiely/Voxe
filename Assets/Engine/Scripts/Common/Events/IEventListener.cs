﻿namespace Engine.Scripts.Common.Events
{
    public interface IEventListener<TEvent>
    {
        void OnNotified(IEventSource<TEvent> source, TEvent evt);
    }
}
