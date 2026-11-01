using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>对战电脑 / 观战 / 练习选人流程（P1 键依次选双方）。</summary>
    public class VsCpuSelectFlow : ISelectModeFlow
    {
        public virtual bool createP2SelectVO() { return true; }

        public virtual bool initBothSelecters() { return false; }

        public virtual bool shouldWaitBothPlayers() { return false; }

        public virtual SelectFlowAction resolveNextStep(int curStep)
        {
            switch (curStep)
            {
                case 0:
                    return new SelectFlowAction(SelectFlowAction.INIT_FIGHTER, 1);
                case 1:
                    return new SelectFlowAction(SelectFlowAction.ENABLE_P2_WITH_P1_INPUT, 2);
                case 2:
                    return new SelectFlowAction(SelectFlowAction.FADOUT_ASSIST, 3);
                case 3:
                    return new SelectFlowAction(SelectFlowAction.ENABLE_P2_WITH_P1_INPUT, 4);
                case 4:
                    return new SelectFlowAction(SelectFlowAction.FADOUT_MAP, 5);
                case 5:
                    return new SelectFlowAction(SelectFlowAction.SELECT_FINISH, -1);
            }
            return new SelectFlowAction();
        }
    }
}
