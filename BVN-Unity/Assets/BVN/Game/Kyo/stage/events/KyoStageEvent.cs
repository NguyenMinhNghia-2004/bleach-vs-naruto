using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class KyoStageEvent : Event
    {
        public const string CHANGE_STATE = "CHANGE_STATE";

        public KyoStageEvent(string type, IStage stage, bool bubbles = false, bool cancelable = false) : base(type, bubbles, cancelable)
        {
            this.stage = stage;
        }

        public IStage stage;
    }
}
