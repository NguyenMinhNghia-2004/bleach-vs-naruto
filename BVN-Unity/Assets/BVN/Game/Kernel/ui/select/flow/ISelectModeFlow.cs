using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>选人模式流程：步进、是否双人选人器、是否等待双方完成。</summary>
    public interface ISelectModeFlow
    {
        bool createP2SelectVO();
        bool initBothSelecters();
        bool shouldWaitBothPlayers();
        SelectFlowAction resolveNextStep(int curStep);
    }
}
