using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class MusouTimeUI
    {
        public MusouTimeUI(Sprite ui)
        {
            _ui = ui;
            _txt = ui.getChildByName("text") as TextField;
        }

        private Sprite _ui;
        private TextField _txt;

        public virtual void renderAnimate()
        {
            if (_txt != null)
            {
                int sec = (int)(Num(GameCtrl.I.getMusouCtrl().gameRunData.gameTime) / Num(GameConfig.FPS_ANIMATE));
                _txt.text = KyoTimerFormat.secToTime(sec);
            }
        }
    }
}
