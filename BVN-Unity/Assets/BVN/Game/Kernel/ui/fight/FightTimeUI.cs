using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class FightTimeUI
    {
        /// <param name="ui">$fight$MC_time</param>
        public FightTimeUI(MovieClip ui)
        {
            _ui = ui;

            int time = GameCtrl.I.gameRunData.gameTimeMax;
            var wuxian = _ui.getChildByName("wuxian");
            if (time == -1)
            {
                _renderTime = false;
                if (wuxian != null) wuxian.visible = true;
            }
            else
            {
                _renderTime = true;

                object timeNumberCls = ResUtils.I.getItemClass(ResUtils.swfLib.fight, "$fight$MC_timeNumber");
                _numMc = new MCNumber(timeNumberCls, 0, 1, 20, 2);
                _numMc.x = -22;
                _numMc.y = -15;
                _ui.addChild(_numMc);

                if (wuxian != null) wuxian.visible = false;
                _numMc.number = (uint)time;
                _lastTime = time;
            }
        }

        private MovieClip _ui;
        private MCNumber _numMc;
        private bool _renderTime;
        /// <summary>上次显示的秒数，未变则跳过 MCNumber 重建</summary>
        private int _lastTime = int.MinValue;

        public virtual DisplayObject timeUI
        {
            get => _numMc;
        }

        public virtual void render()
        {
            if (!_renderTime)
            {
                return;
            }
            int time = GameCtrl.I.gameRunData.gameTime;
            if (time == _lastTime)
            {
                return;
            }
            _lastTime = time;
            _numMc.number = (uint)time;
        }
    }
}
