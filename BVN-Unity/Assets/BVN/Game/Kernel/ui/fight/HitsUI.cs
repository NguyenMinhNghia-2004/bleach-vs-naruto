using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class HitsUI
    {
        public HitsUI(MovieClip mc)
        {
            _mc = mc;
            object cls = ResUtils.I.getItemClass(ResUtils.swfLib.fight, "$fight$MC_hitsNumer");
            _txtmc = new MCNumber(cls, 0, 1, 35);
            _orgPos = new Point(mc.x, mc.y);
            ct.addChild(_txtmc);
        }

        private MovieClip _mc;
        private MCNumber _txtmc;
        private bool _isShow;
        private Point _orgPos;

        /// <summary>AS3 `_mc.ct`</summary>
        private DisplayObjectContainer ct => _mc.getChildByName("ct") as DisplayObjectContainer;

        public virtual void destroy()
        {
            if (_txtmc != null)
            {
                try
                {
                    ct.removeChild(_txtmc);
                }
                catch (Exception)
                {
                }
                _txtmc = null;
            }

            if (_mc != null)
            {
                _mc = null;
            }

            _orgPos = null;
        }

        public virtual void show(int num)
        {
            _txtmc.number = (uint)num;

            double xoffset = -_txtmc.width + 45;
            _txtmc.x = xoffset;

            if (_mc.name == "hits1")
            {
                _mc.x = _orgPos.x - xoffset;
            }

            if (_isShow)
            {
                _mc.gotoAndPlay("update");
                return;
            }
            _isShow = true;
            _mc.gotoAndPlay("fadin");
        }

        public virtual void hide()
        {
            if (!_isShow)
            {
                return;
            }
            _isShow = false;
            _mc.gotoAndPlay("fadout");
        }
    }
}
