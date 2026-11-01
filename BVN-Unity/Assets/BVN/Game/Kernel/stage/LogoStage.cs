using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class LogoStage : IStage
    {
        private MovieClip _ui; // $common$MC_logo

        /// <summary>显示对象</summary>
        public virtual DisplayObject display => _ui;

        /// <summary>构建</summary>
        public virtual void build()
        {
            _ui = ResUtils.I.createDisplayObject(ResUtils.swfLib.common, "$common$MC_logo") as MovieClip;
            _ui.addEventListener(Event.COMPLETE, playComplete);
            _ui.gotoAndPlay(2);
        }

        /// <summary>稍后构建</summary>
        public virtual void afterBuild()
        {
        }

        /// <summary>销毁</summary>
        public virtual void destroy(Delegate back = null)
        {
            _ui.removeEventListener(Event.COMPLETE, playComplete);
        }

        private void playComplete(Event e)
        {
            _ui.removeEventListener(Event.COMPLETE, playComplete);
            MainGame.I.goMenu();
        }
    }
}
