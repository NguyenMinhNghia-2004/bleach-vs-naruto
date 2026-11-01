using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class KeyEvent : Event
    {
        public const string KEY_DOWN = "KEY_DOWN";
        public const string KEY_UP = "KEY_UP";

        public KeyEvent(string type, uint keyCode, bool justDown = false, bool bubbles = false, bool cancelable = false)
            : base(type, bubbles, cancelable)
        {
            this.keyCode = keyCode;
            this.justDown = justDown;
        }

        public uint keyCode;
        public bool justDown;
    }
}
