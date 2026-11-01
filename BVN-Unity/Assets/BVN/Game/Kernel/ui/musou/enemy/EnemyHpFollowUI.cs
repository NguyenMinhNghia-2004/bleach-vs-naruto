using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class EnemyHpFollowUI
    {
        public EnemyHpFollowUI(FighterMain fighter)
        {
            // AS3: new $musou$MC_enemyHpBarFollow()
            _ui = ResUtils.I.createDisplayObject(ResUtils.swfLib.musou, "$musou$MC_enemyHpBarFollow") as MovieClip;
            _ui.scaleX = _ui.scaleY = 0.5;
            _bar = _ui.getChildByName("barmc");
            _fighter = fighter;

            _ui.visible = false;
        }

        private MovieClip _ui;
        private FighterMain _fighter;
        private DisplayObject _bar;
        private int _showDelay;

        public virtual FighterMain getFighter()
        {
            return _fighter;
        }

        public virtual DisplayObject getUI()
        {
            return _ui;
        }

        public virtual void active()
        {
            _ui.visible = true;
            _showDelay = (int)(GameConfig.FPS_GAME * 3);
        }

        public virtual bool render()
        {
            if (_fighter == null)
            {
                return false;
            }

            if (!_ui.visible)
            {
                return _fighter.isAlive;
            }

            double val = _fighter.hp / _fighter.hpMax;
            if (val < 0.0001)
            {
                val = 0.0001;
            }
            if (val > 1)
            {
                val = 1;
            }

            _ui.x = _fighter.x;
            _ui.y = _fighter.y - 50;

            _bar.scaleX = val;

            if (--_showDelay <= 0)
            {
                _ui.visible = false;
            }

            return _fighter.isAlive;
        }

        public virtual void destroy()
        {
            _fighter = null;
            _bar = null;
            _ui = null;
        }
    }
}
