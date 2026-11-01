using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>Fade in/out effect used by KyoStageCtrl.addLayer/removeLayer.</summary>
    public interface IStageFadeEffect
    {
        void fadeIn(IStage stage, Delegate complete = null);

        void fadeOut(IStage stage, Delegate complete = null);
    }
}
