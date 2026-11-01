using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class MusouKOsUI
    {
        public MusouKOsUI(Sprite ui)
        {
            _text = new BitmapFontText(AssetManager.I.getFont("font1"));
            ui.addChild(_text);
        }

        private Sprite _ct;
        private BitmapFontText _text;

        public virtual void update()
        {
            _text.text = GameCtrl.I.getMusouCtrl().gameRunData.koNum.ToString();
        }
    }
}
