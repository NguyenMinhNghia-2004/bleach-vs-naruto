using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class SetBtnDialog
    {
        public SetBtnDialog()
        {
            ui = ResUtils.I.createDisplayObject(ResUtils.swfLib.setting, "$setting$SP_keySetDialog") as Sprite;
            ui.visible = false;

            _pushTxt = new BitmapFontText(AssetManager.I.getFont("font1"));
            _keyNameTxt = new BitmapFontText(AssetManager.I.getFont("font1"));

            _pushTxt.y = _keyNameTxt.y = -30;

            _pushTxt.text = "PUSH A KEY FOR";
            _pushTxt.x = -_pushTxt.width / 2;
            (ui.getChildByName("ct_msg") as DisplayObjectContainer).addChild(_pushTxt);

            (ui.getChildByName("ct_keyname") as DisplayObjectContainer).addChild(_keyNameTxt);

            _cntxt = ui.getChildByName("txt") as TextField;
            if (_cntxt != null) _cntxt.defaultTextFormat = new TextFormat("楷体", 20);
        }

        public Sprite ui;
        public bool isShow = true;
        private BitmapFontText _pushTxt;
        private BitmapFontText _keyNameTxt;
        private TextField _cntxt;

        public virtual void show(string name, string cn)
        {
            ui.visible = true;

            _keyNameTxt.text = name;
            _keyNameTxt.x = -_keyNameTxt.width / 2;
            if (_cntxt != null) _cntxt.text = GetLang("txt.set_btn_dialog.push_key", new ASObject { ["name"] = cn });

            isShow = true;
        }

        public virtual void hide()
        {
            ui.visible = false;
            isShow = false;
        }
    }
}
