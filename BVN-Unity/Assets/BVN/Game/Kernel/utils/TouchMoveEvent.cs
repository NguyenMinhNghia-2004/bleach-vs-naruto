using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class TouchMoveEvent : Event
    {
        public const string TOUCH_BEGIN = "EVENT_TOUCH_BEGIN";
        public const string TOUCH_MOVE = "EVENT_TOUCH_MOVE";
        public const string TOUCH_END = "EVENT_TOUCH_END";

        public TouchMoveEvent(string type) : base(type, false, false)
        {
        }

        public double deltaX = double.NaN;
        public double deltaY = double.NaN;
        public double delta = double.NaN;
        public double startX = 0;
        public double startY = 0;
        public double endX = 0;
        public double endY = 0;
        public double distanceX = 0; // 从触摸开始到结束的X距离
        public double distanceY = 0; // 从触摸开始到结束的Y距离
    }
}
