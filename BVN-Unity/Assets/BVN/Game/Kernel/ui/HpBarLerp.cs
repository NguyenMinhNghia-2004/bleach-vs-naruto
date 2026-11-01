using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>
    /// 血条主条与红色滞后条插值。供对战 / 无双 / Boss 血条复用同一套受击延迟与缩放插值。
    /// </summary>
    public class HpBarLerp
    {
        /// <param name="bar">主血条（scaleX）。</param>
        /// <param name="redBar">红色滞后条（scaleX）。</param>
        public HpBarLerp(DisplayObject bar, DisplayObject redBar)
        {
            _bar = bar;
            _redBar = redBar;
        }

        private DisplayObject _bar;
        private DisplayObject _redBar;
        private double _hprate = 1;
        private bool _redBarMoving;
        private int _redBarMoveDelay;
        private bool _justHurtFly;

        /// <summary>按角色当前血量与受击状态推进插值。</summary>
        public virtual void render(FighterMain fighter)
        {
            double rate = (double)fighter.hp / fighter.hpMax;

            if (_redBarMoving && rate != _hprate)
            {
                _redBar.scaleX = _hprate;
                _redBarMoving = false;
            }

            _hprate = rate;
            double diff = _hprate - _bar.scaleX;
            double addRate = diff < 0 ? 0.4 : 0.04;

            if (Math.Abs(diff) < 0.01)
            {
                _bar.scaleX = _hprate;
            }
            else
            {
                _bar.scaleX += diff * addRate;
            }

            int state = fighter.actionState;
            if (state == FighterActionState.HURT_ING)
            {
                _redBarMoveDelay = 100;
            }
            else if (state == FighterActionState.HURT_FLYING || state == FighterActionState.HURT_DOWN)
            {
                if (_redBarMoveDelay > 0)
                {
                    if (!_justHurtFly)
                    {
                        _redBarMoveDelay = (int)(1.5 * GameConfig.FPS_GAME);
                        _justHurtFly = true;
                    }
                    else
                    {
                        if (_redBarMoveDelay > 0)
                        {
                            _redBarMoveDelay--;
                        }
                    }
                }
            }
            else
            {
                _redBarMoveDelay = 0;
                _justHurtFly = false;
            }

            if (_redBarMoveDelay <= 0)
            {
                double diff2 = _hprate - _redBar.scaleX;
                double addRate2 = diff2 < 0 ? 0.1 : 0.02;
                if (Math.Abs(diff2) < 0.01)
                {
                    _redBar.scaleX = _hprate;
                    _redBarMoving = false;
                }
                else
                {
                    _redBar.scaleX += diff2 * addRate2;
                    _redBarMoving = true;
                }
            }
        }
    }
}
