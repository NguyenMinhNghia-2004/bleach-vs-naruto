using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>分页圆点（linkage $dialog$MC_dot）。</summary>
    public class DotItemUI
    {
        public DotItemUI()
        {
            _ui = ResUtils.I.createDisplayObject(ResUtils.swfLib.dialog, "$dialog$MC_dot") as MovieClip;
            _ui.mouseChildren = false;
            _ui.buttonMode = true;
            _ui.gotoAndStop(2);
        }

        public int page = 0;
        private MovieClip _ui;

        public virtual MovieClip getUI()
        {
            return _ui;
        }

        public virtual void focus(bool v)
        {
            _ui.gotoAndStop(v ? 1 : 2);
        }
    }
}
