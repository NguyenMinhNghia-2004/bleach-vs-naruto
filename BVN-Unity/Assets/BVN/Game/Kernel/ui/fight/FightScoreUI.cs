using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class FightScoreUI
    {
        /// <param name="ui">$fight$MC_score</param>
        public FightScoreUI(MovieClip ui)
        {
            _ui = ui;

            object scoreNumberCls = ResUtils.I.getItemClass(ResUtils.swfLib.fight, "$fight$MC_scoreNumber");
            _numberMc = new MCNumber(scoreNumberCls, 0, 1, 10, 10);
            (_ui.getChildByName("ct") as DisplayObjectContainer).addChild(_numberMc);
        }

        private MovieClip _ui;
        private MCNumber _numberMc;

        public virtual void setScore(int v)
        {
            _numberMc.number = (uint)v;
        }
    }
}
