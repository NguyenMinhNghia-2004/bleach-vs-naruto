using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>双人对战选人流程。</summary>
    public class VsPeopleSelectFlow : ISelectModeFlow
    {
        public virtual bool createP2SelectVO() { return true; }

        public virtual bool initBothSelecters() { return true; }

        public virtual bool shouldWaitBothPlayers() { return true; }

        public virtual SelectFlowAction resolveNextStep(int curStep)
        {
            switch (curStep)
            {
                case 0:
                    return new SelectFlowAction(SelectFlowAction.INIT_FIGHTER, 1);
                case 1:
                    return new SelectFlowAction(SelectFlowAction.FADOUT_ASSIST, 3);
                case 3:
                    return new SelectFlowAction(SelectFlowAction.FADOUT_MAP, 5);
                case 5:
                    return new SelectFlowAction(SelectFlowAction.SELECT_FINISH, -1);
            }
            return new SelectFlowAction();
        }
    }
}
