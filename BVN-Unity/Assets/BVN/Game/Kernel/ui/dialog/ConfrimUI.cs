using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class ConfrimUI : BaseDialog
    {
        public ConfrimUI() : base()
        {
            // _ui = ResUtils.I.createDisplayObject(ResUtils.swfLib.dialog, 'dialog_confrim');
            // _dialogUI= _ui;

            build();
        }

        private BitmapFontText _enTxt;
        private TextField _old_cnTxt;
        private SetBtnGroup _btnGroup;

        private Sprite _ui;

        public override void onDestroy()
        {
            base.onDestroy();

            if (_old_cnTxt != null)
            {
                _old_cnTxt = null;
            }
            if (_cnTxt != null)
            {
                _cnTxt.destroy();
                _cnTxt = null;
            }
            if (_enTxt != null)
            {
                _enTxt.dispose();
                _enTxt = null;
            }
            if (_btnGroup != null)
            {
                _btnGroup.removeEventListener<SetBtnEvent>(SetBtnEvent.SELECT, selectHandler);
                _btnGroup.destroy();
                _btnGroup = null;
            }

            BtnUtils.destroyBtn(_noBtn);
            BtnUtils.destroyBtn(_yesBtn);
        }

        public override void setMsg(string en = null, string cn = null)
        {
            _enTxt.text = !string.IsNullOrEmpty(en) ? en : "";
            _enTxt.x = (GameConfig.GAME_SIZE.x - _enTxt.width) / 2;

            // if(!cn) return;

            if (_old_cnTxt != null)
            {
                _old_cnTxt.text = !string.IsNullOrEmpty(cn) ? cn : "";
            }
            if (_cnTxt != null)
            {
                _cnTxt.text = !string.IsNullOrEmpty(cn) ? cn : "";
            }
        }

        private void build()
        {
            _ui = new Sprite();
            _dialogUI = _ui;

            var box = new Box(GameConfig.GAME_SIZE.x, 300, 0, 0.8);
            box.y = (GameConfig.GAME_SIZE.y - box.height) / 2;
            _ui.addChild(box);

            _enTxt = new BitmapFontText(AssetManager.I.getFont("font1"));
            _enTxt.y = 18;
            box.addChild(_enTxt);

            if (GameUI.SHOW_CN_TEXT)
            {
                _old_cnTxt = new TextField();
                UIUtils.formatText(_old_cnTxt, new ASObject { ["color"] = (double)0xffffff, ["size"] = 20.0, ["align"] = TextFormatAlign.CENTER });
                _old_cnTxt.y = _enTxt.y + _enTxt.height + 80;
                _old_cnTxt.width = GameConfig.GAME_SIZE.x;
                _old_cnTxt.height = 100;
                _old_cnTxt.mouseEnabled = false;
                box.addChild(_old_cnTxt);
            }

            _btnGroup = new SetBtnGroup();
            _btnGroup.startX = _btnGroup.startY = 0;
            _btnGroup.direct = 0;
            _btnGroup.gap = 200;
            _btnGroup.setBtnData(new List<object>
            {
                new ASObject { ["label"] = "YES", ["cn"] = GetLang("txt.common.yes") },
                new ASObject { ["label"] = "NO", ["cn"] = GetLang("txt.common.no") }
            }, 1);
            _btnGroup.addEventListener<SetBtnEvent>(SetBtnEvent.SELECT, selectHandler);
            _btnGroup.x = (GameConfig.GAME_SIZE.x - _btnGroup.width) / 2 + 30;
            _btnGroup.y = box.height - 80;
            box.addChild(_btnGroup);

            double boxy = box.y;
            box.y = GameConfig.GAME_SIZE.y;
            TweenLite.to(box, 0.2, new ASObject { ["y"] = boxy });
        }

        private void selectHandler(SetBtnEvent e)
        {
            switch (e.selectedLabel)
            {
                case "YES":
                    if (yesBack != null)
                    {
                        Call(yesBack);
                    }
                    break;
                case "NO":
                    if (noBack != null)
                    {
                        Call(noBack);
                    }
                    break;
            }
        }
    }
}
