using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>Scene/layer managed by KyoStageCtrl.</summary>
    public interface IStage
    {
        DisplayObject display { get; }

        void build();

        void afterBuild();

        void destroy(Delegate back = null);
    }
}
