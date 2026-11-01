using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>选人流程步进动作（由 SelectFighterStage 执行）。</summary>
    public class SelectFlowAction
    {
        public const int NONE = 0;
        public const int INIT_FIGHTER = 1;
        public const int ENABLE_P2_WITH_P1_INPUT = 2;
        public const int FADOUT_ASSIST = 3;
        public const int FADOUT_MAP = 4;
        public const int START_ARCADE = 5;
        public const int START_MUSOU = 6;
        public const int SELECT_FINISH = 7;

        public int action = NONE;
        /// <summary>执行后写入的 _curStep；-1 表示不改步号。</summary>
        public int nextStep = -1;

        public SelectFlowAction(int action = 0, int nextStep = -1)
        {
            this.action = action;
            this.nextStep = nextStep;
        }
    }
}
