using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class KeyMapping
    {
        public KeyMapping(Sprite mc, string keyId, string name, string cn)
        {
            this.mc = mc;
            this.keyId = keyId;
            this.name = name;
            this.cn = cn;

            _keyTxt = new BitmapFontText(AssetManager.I.getFont("font1"));

            var matrix = new ColorMatrix();
            matrix.SetBrightnessMatrix(-100);
            matrix.SetSaturationMatrix(0);
            var flat = new List<double>();
            foreach (var v in matrix.GetFlatArray()) flat.Add(Num(v));
            _keyTxt.filters = new List<object> { new ColorMatrixFilter(flat) };

            mc.addChild(_keyTxt);
        }

        public string name;
        public Sprite mc;
        public string keyId;
        public string cn;
        private BitmapFontText _keyTxt;

        public virtual void setKey(int code, string keyName = null)
        {
            if (string.IsNullOrEmpty(keyName)) keyName = KyoKeyCode.code2name(code);
            _keyTxt.text = (keyName ?? "").ToUpperInvariant();

            double _maxWidth = 0;
            double _scale = 1;
            double offsetY = 0;

            if (keyId == "up" || keyId == "down" || keyId == "left" || keyId == "right")
            {
                _scale = 0.8;
                _maxWidth = 40;
                offsetY = 5;
            }
            else
            {
                _maxWidth = 60;
            }

            if (_keyTxt.width > _maxWidth)
            {
                _keyTxt.width = _maxWidth;
                _keyTxt.scaleY = _keyTxt.scaleX;
            }
            else
            {
                _keyTxt.scaleX = _keyTxt.scaleY = _scale;
            }

            _keyTxt.x = -_keyTxt.width / 2;
            _keyTxt.y = -_keyTxt.height / 2 + offsetY;
        }
    }
}
