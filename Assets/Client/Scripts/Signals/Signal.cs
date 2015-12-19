using System;
using UnityEngine;

namespace Assets.Client.Scripts.Signals
{
    [Serializable]
    public class Signal
    {
        public GameObject Target;
        public string Method;
        public string ArgType;

        public Signal()
        {

        }
        public Signal(Type argType)
        {
            ArgType = argType.FullName;
        }

        public void Invoke()
        {
            if (Target != null && !string.IsNullOrEmpty(Method))
                Target.SendMessage(Method, SendMessageOptions.RequireReceiver);
        }
        public void Invoke(object value)
        {
            if (ArgType != null)
            {
                if (Target == null || string.IsNullOrEmpty(Method))
                    return;

                if (ArgType.Equals(value.GetType().FullName))
                    Target.SendMessage(Method, value, SendMessageOptions.RequireReceiver);
                else
                    Debug.LogError("Incorrect parameter type, expected [" + ArgType + "], got [" + value.GetType().FullName + "].");
            }
            else
                Invoke();
        }
    }
}