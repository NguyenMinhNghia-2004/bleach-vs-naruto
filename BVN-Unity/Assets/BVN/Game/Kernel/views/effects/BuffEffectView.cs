using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class BuffEffectView : EffectView
    {
        public BuffEffectView(EffectVO data) : base(data)
        {
            this.loopPlay = true;
        }

        private FighterMain _fighter;
        private FighterBuffVO _buff;

        public override void setTarget(IGameSprite v)
        {
            base.setTarget(v);

            if (v is FighterMain)
            {
                _fighter = v as FighterMain;
            }

            if (_fighter != null && _data.targetColorOffset != null)
            {
                var ct = new ColorTransform();
                ct.redOffset = Num(_data.targetColorOffset[0]);
                ct.greenOffset = Num(_data.targetColorOffset[1]);
                ct.blueOffset = Num(_data.targetColorOffset[2]);
                _fighter.changeColor(ct);
            }
        }

        public override void render()
        {
            base.render();
            if (_buff.finished)
            {
                if (_data.targetColorOffset != null && _fighter != null)
                {
                    _fighter.resumeColor();
                }
                remove();
            }
            else
            {
                if (_fighter != null)
                {
                    setPos(_fighter.x, _fighter.y);
                }
            }
        }

        public virtual void setBuff(FighterBuffVO v)
        {
            _buff = v;
        }
    }
}
