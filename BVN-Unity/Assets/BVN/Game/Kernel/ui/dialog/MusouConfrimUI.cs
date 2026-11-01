using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class MusouConfrimUI : BaseDialog
    {
        public MusouConfrimUI() : base()
        {
            width = 495;
            height = 240;

            _ui = ResUtils.I.createDisplayObject(ResUtils.swfLib.dialog, "$dialog$MC_confirm") as MovieClip;
            _dialogUI = _ui;

            build();
        }

        private MovieClip _ui; // $dialog$MC_confirm

        public override void onDestroy()
        {
            base.onDestroy();
            if (_cnTxt != null)
            {
                _cnTxt.destroy();
                _cnTxt = null;
            }
            BtnUtils.destroyBtn(_noBtn);
            BtnUtils.destroyBtn(_yesBtn);
        }

        public override void setMsg(string en = null, string cn = null)
        {
            setTitle(en);

            if (string.IsNullOrEmpty(cn))
            {
                return;
            }

            _cnTxt.text = cn;
            _cnTxt.visible = true;
        }

        public virtual void build()
        {
            _cnTxt = new Text();
            _cnTxt.leading = 12;
            _cnTxt.x = 15;
            _cnTxt.y = 35;
            _cnTxt.width = 460;
            _cnTxt.height = 140;
            _cnTxt.multiLine(true);
            _cnTxt.align = TextFormatAlign.CENTER;
            _ui.addChild(_cnTxt);

            _noBtn = _ui.getChildByName("no");
            _yesBtn = _ui.getChildByName("yes");

            BtnUtils.initBtn(_noBtn, (Action<object>)okHandler);
            BtnUtils.initBtn(_yesBtn, (Action<object>)okHandler);
        }

        private void okHandler(object e)
        {
            if (e == _yesBtn)
            {
                if (yesBack != null)
                {
                    Call(yesBack);
                }
            }
            else if (e == _noBtn)
            {
                if (noBack != null)
                {
                    Call(noBack);
                }
            }
        }
    }
}
