using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class SteelHitEffect : EffectView
    {
        public SteelHitEffect(EffectVO data) : base(data)
        {
        }

        private FighterMain _fighter;

        public override void setTarget(IGameSprite v)
        {
            base.setTarget(v);

            if (v is FighterMain)
            {
                _fighter = v as FighterMain;

                if (_fighter.isSteelBody)
                {
                    var ct = new ColorTransform();
                    ct.redOffset = 150;
                    ct.greenOffset = 150;
                    ct.blueOffset = 150;

                    if (_fighter.isSuperSteelBody)
                    {
                        ct.blueOffset = 0;
                        EffectCtrl.I.shine(0xffff00, 0.3);
                    }

                    _fighter.changeColor(ct);
                    var f = _fighter;
                    EffectCtrl.I.setOnFreezeOver((Action)(() =>
                    {
                        // AS3 closure reads the field at call time
                        (_fighter ?? f).resumeColor();
                    }));
                }
            }
        }
    }
}
