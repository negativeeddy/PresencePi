using System;

namespace NegativeEddy.PresencePi.Display
{
    public class PresenceChangingEventArgs : EventArgs
    {
        public Microsoft.Graph.Presence Current { get; set; }
        public Microsoft.Graph.Presence New { get; set; }
    }
}
