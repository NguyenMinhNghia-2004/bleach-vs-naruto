using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>闯关选人流程（无地图，援助后直接开局）。</summary>
    public class ArcadeSelectFlow : ISelectModeFlow
    {
        public virtual bool createP2SelectVO() { return false; }

        public virtual bool initBothSelecters() { return false; }

        public virtual bool shouldWaitBothPlayers() { return false; }

        public virtual SelectFlowAction resolveNextStep(int curStep)
        {
            switch (curStep)
            {
                case 0:
                    return new SelectFlowAction(SelectFlowAction.INIT_FIGHTER, 1);
                case 1:
                    return new SelectFlowAction(SelectFlowAction.FADOUT_ASSIST, 3);
                case 3:
                    return new SelectFlowAction(SelectFlowAction.START_ARCADE, -1);
            }
            return new SelectFlowAction();
        }
    }
}
