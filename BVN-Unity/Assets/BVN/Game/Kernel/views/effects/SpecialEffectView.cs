using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>火、冰、雷 击中后续效果</summary>
    public class SpecialEffectView : EffectView
    {
        public SpecialEffectView(EffectVO data) : base(data)
        {
        }

        private FighterMain _fighter;
        private bool _finished;

        public override void setTarget(IGameSprite v)
        {
            base.setTarget(v);

            if (v is FighterMain)
            {
                _fighter = v as FighterMain;
                if (_data.targetColorOffset != null)
                {
                    var ct = new ColorTransform();
                    ct.redOffset = Num(_data.targetColorOffset[0]);
                    ct.greenOffset = Num(_data.targetColorOffset[1]);
                    ct.blueOffset = Num(_data.targetColorOffset[2]);
                    _fighter.changeColor(ct);
                }
            }
        }

        public override void start(double x = 0, double y = 0, int direct = 1, bool playSound = true)
        {
            base.start(x, y, direct, playSound);
            _finished = false;
        }

        public override void render()
        {
            base.render();

            if (_finished)
            {
                return;
            }
            if (_fighter == null)
            {
                return;
            }

            var state = _fighter.actionState;
            if (state == FighterActionState.HURT_DOWN ||
                state == FighterActionState.HURT_DOWN_TAN ||
                state == FighterActionState.NORMAL)
            {
                gotoAndPlay("finish");
                _finished = true;
                if (_data.targetColorOffset != null)
                {
                    _fighter.resumeColor();
                }
            }
            else
            {
                setPos(_fighter.x, _fighter.y);
            }
        }
    }
}
