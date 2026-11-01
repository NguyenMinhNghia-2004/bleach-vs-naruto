using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class MusouMiniHpBar
    {
        public MusouMiniHpBar(MovieClip ui)
        {
            _ui = ui;
        }

        private MovieClip _ui;
        private FighterMain _fighter;

        public virtual void setFighter(FighterMain f)
        {
            _fighter = f;
            updateFace();
        }

        public virtual void renderAnimate()
        {
            if (_fighter == null)
            {
                return;
            }
            _ui.getChildByName("bar_hp").scaleX = getScale(_fighter.hp, _fighter.hpMax);
            _ui.getChildByName("bar_qi").scaleX = getScale(_fighter.qi, 300);
        }

        private void updateFace()
        {
            var face = _ui.getChildByName("face") as DisplayObjectContainer;
            var ct = face != null ? face.getChildByName("ct") as Sprite : null;
            if (ct != null)
            {
                // AS3 would throw on a null fighter here; guard instead.
                DisplayObject faceImg = _fighter != null ? AssetManager.I.getFighterFace(_fighter.data) : null;
                if (faceImg != null)
                {
                    ct.removeChildren();
                    ct.addChild(faceImg);
                }
            }
        }

        private static double getScale(double val, double max)
        {
            double rate = val / max;
            if (rate < 0.0001)
            {
                rate = 0.0001;
            }
            if (rate > 1)
            {
                rate = 1;
            }
            return rate;
        }
    }
}
