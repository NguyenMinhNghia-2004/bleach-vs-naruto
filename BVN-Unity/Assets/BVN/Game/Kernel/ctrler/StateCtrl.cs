using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>场景转场控制（门面转到 GameUI 静态转场 API）。</summary>
    public class StateCtrl
    {
        private static StateCtrl _i;

        public static StateCtrl I
        {
            get
            {
                _i ??= new StateCtrl();
                return _i;
            }
        }

        /// <summary>是否启用转场</summary>
        public bool transEnabled = true;

        public virtual void transIn(Delegate back = null, bool removeAfterComplete = false)
        {
            if (!transEnabled)
            {
                if (back != null) Call(back);
                return;
            }
            GameUI.transIn(back, removeAfterComplete);
        }

        public virtual void transOut(Delegate back = null, bool removeAfterComplete = true)
        {
            if (!transEnabled)
            {
                if (back != null) Call(back);
                return;
            }
            GameUI.transOut(back, removeAfterComplete);
        }

        public virtual void quickTrans(Delegate back = null)
        {
            GameUI.quickTrans(back);
        }

        public virtual void clearTrans()
        {
            GameUI.clearTrans();
        }
    }
}
