using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class AlertUI : MusouConfrimUI
    {
        public AlertUI() : base()
        {
            build();
        }

        public override void build()
        {
            base.build();

            _noBtn.visible = false;

            _yesBtn.x = 253;
        }
    }
}
