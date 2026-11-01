using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class WinUI
    {
        /// <param name="ui">$fight$MC_win</param>
        public WinUI(MovieClip ui, int team)
        {
            _ui = ui;
            _team = team;
        }

        private int _team;

        private MovieClip _ui;

        public virtual DisplayObject ui
        {
            get => _ui;
        }

        public virtual void show(FighterVO fighter, int wins)
        {
            if (fighter == null)
            {
                return;
            }

            MovieClip playmc = null;

            switch (wins)
            {
                case 1:
                    playmc = (_team == TeamID.TEAM_1 ? _ui.getChildByName("w1") : _ui.getChildByName("w2")) as MovieClip;
                    break;
                case 2:
                    playmc = (_team == TeamID.TEAM_1 ? _ui.getChildByName("w2") : _ui.getChildByName("w1")) as MovieClip;
                    break;
            }

            if (playmc == null)
            {
                return;
            }

            switch (fighter.comicType)
            {
                case 0:
                    playmc.gotoAndPlay("in_bleach");
                    break;
                case 1:
                    playmc.gotoAndPlay("in_naruto");
                    break;
            }
        }
    }
}
