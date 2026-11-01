using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class BaseDialog
    {
        public double offsetX = 0;
        public double offsetY = 0;
        public double width = 0;
        public double height = 0;
        public Delegate yesBack;
        public Delegate noBack;
        // AS3 SimpleButton: converted SWF buttons are MovieClips, so buttons are typed DisplayObject.
        public DisplayObject _backBtn;
        // protected var _ui:Sprite;
        public Sprite _dialogUI;
        public Text _cnTxt;
        public DisplayObject _noBtn;
        public DisplayObject _yesBtn;
        private BitmapFontText _titleTxt;

        public virtual DisplayObject getDisplay()
        {
            return _dialogUI;
        }

        public void init()
        {
            _backBtn = _dialogUI.getChildByName("back");

            BtnUtils.initBtn(_backBtn, (Action<object[]>)closeSelf);
        }

        public void show(double X, double Y)
        {
            _dialogUI.x = X + offsetX;
            _dialogUI.y = Y + offsetY;
            onShow();
        }

        public virtual void setTitle(string v)
        {
            if (string.IsNullOrEmpty(v))
            {
                return;
            }

            if (_titleTxt == null)
            {
                _titleTxt = new BitmapFontText(AssetManager.I.getFont("font1"));
                _titleTxt.y = -10;
                _dialogUI.addChild(_titleTxt);
            }

            _titleTxt.text = v;
            _titleTxt.x = (width - _titleTxt.width) / 2;
        }

        public void close()
        {
            onClose();
        }

        public void closeSelf(params object[] @params)
        {
            DialogManager.closeDialog(this);
        }

        public void destroy()
        {
            if (_titleTxt != null)
            {
                _titleTxt.dispose();
                _titleTxt = null;
            }

            onDestroy();
        }

        public void hide()
        {
            _dialogUI.visible = false;
            onHide();
        }

        public bool hiding()
        {
            return !_dialogUI.visible;
        }

        public void resume()
        {
            _dialogUI.visible = true;
            onResume();
        }

        public virtual void setMsg(string en = null, string cn = null)
        {
        }

        public virtual void onShow()
        {
        }

        public virtual void onHide()
        {
        }

        public virtual void onResume()
        {
        }

        public virtual void onClose()
        {
        }

        public virtual void onDestroy()
        {
        }
    }
}
