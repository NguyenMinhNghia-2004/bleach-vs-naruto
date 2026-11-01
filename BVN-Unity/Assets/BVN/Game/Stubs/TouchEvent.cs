using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>
    /// Stub of flash.events.TouchEvent (missing from BVN.Flash). Touch input is not dispatched by the
    /// Unity runtime yet; listeners registered with these types simply never fire.
    /// </summary>
    public class TouchEvent : Event
    {
        public const string TOUCH_BEGIN = "touchBegin";
        public const string TOUCH_END = "touchEnd";
        public const string TOUCH_MOVE = "touchMove";
        public const string TOUCH_OVER = "touchOver";
        public const string TOUCH_OUT = "touchOut";
        public const string TOUCH_ROLL_OVER = "touchRollOver";
        public const string TOUCH_ROLL_OUT = "touchRollOut";
        public const string TOUCH_TAP = "touchTap";

        public int touchPointID;
        public bool isPrimaryTouchPoint;
        public double localX, localY, stageX, stageY;

        public TouchEvent(string type, bool bubbles = true, bool cancelable = false) : base(type, bubbles, cancelable) { }
    }
}
