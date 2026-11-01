using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class MusouHpBar
    {
        public MusouHpBar(DisplayObject bar, DisplayObject bar2)
        {
            _lerp = new HpBarLerp(bar, bar2);
        }

        private FighterMain _fighter;
        private HpBarLerp _lerp;

        public virtual void setFighter(FighterMain fighter)
        {
            _fighter = fighter;
        }

        public virtual void render()
        {
            _lerp.render(_fighter);
        }
    }
}
