using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>Stub of flash.events.GameInputEvent.</summary>
    public class GameInputEvent : Event
    {
        public const string DEVICE_ADDED = "deviceAdded";
        public const string DEVICE_REMOVED = "deviceRemoved";
        public const string DEVICE_UNUSABLE = "deviceUnusable";

        public GameInputDevice device;

        public GameInputEvent(string type, bool bubbles = false, bool cancelable = false, GameInputDevice device = null) : base(type, bubbles, cancelable)
        {
            this.device = device;
        }
    }
}
