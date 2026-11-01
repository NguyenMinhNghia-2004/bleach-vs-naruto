using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class MusouWaveUI
    {
        public MusouWaveUI(Sprite ui)
        {
            _ui = ui;

            _txtCur = ui.getChildByName("txt") as TextField;
            _txtMax = ui.getChildByName("txt_max") as TextField;

            _circleBp = new Bitmap();

            var ct = ui.getChildByName("ct_circle") as Sprite;
            ct.addChild(_circleBp);
        }

        private Sprite _ui;
        private TextField _txtCur;
        private TextField _txtMax;
        private Sprite _circleMc;
        private Bitmap _circleBp;

        public virtual void renderAnimate()
        {
            MusouCtrl musouCtrl = GameCtrl.I.getMusouCtrl();
            _txtCur.text = musouCtrl.currentWave.ToString();
            _txtMax.text = musouCtrl.waveCount.ToString();
            renderCircle();
        }

        private void renderCircle()
        {
            MusouCtrl musouCtrl = GameCtrl.I.getMusouCtrl();
            int angle = (int)(musouCtrl.getWavePercent() * 360);
            _circleBp.bitmapData = KyoDrawUtils.drawRing(10, 29, angle, new List<object> { KyoColor.YELLOW, KyoColor.RED }, 1);
        }
    }
}
