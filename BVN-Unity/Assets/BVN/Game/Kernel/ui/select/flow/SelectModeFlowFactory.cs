using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>按 GameMode 创建选人流程实现。</summary>
    public class SelectModeFlowFactory
    {
        public static ISelectModeFlow create()
        {
            if (GameMode.isVsPeople()) return new VsPeopleSelectFlow();
            if (GameMode.isVsCPU()) return new VsCpuSelectFlow();
            if (GameMode.currentMode == GameMode.MUSOU_ARCADE) return new MusouSelectFlow();
            return new ArcadeSelectFlow();
        }
    }
}
