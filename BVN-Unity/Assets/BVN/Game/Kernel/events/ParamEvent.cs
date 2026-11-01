using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class ParamEvent : Event
    {
        public ParamEvent(string type, object param, bool bubbles = false, bool cancelable = false) : base(type, bubbles, cancelable)
        {
            this.param = param;
        }

        public object param;
    }
}
