using System;

namespace Client.Scripts.Signals
{
    public class SignalAttribute : Attribute
    {
        public string Name;

        public SignalAttribute()
        {

        }
        public SignalAttribute(string name)
        {
            Name = name;
        }
    }
}