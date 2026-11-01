using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>Bitmap showing text rendered with a BitmapFont.</summary>
    public class BitmapFontText : Bitmap
    {
        public BitmapFontText(BitmapFont font) : base(null, "auto", true)
        {
            _font = font;
        }

        private BitmapFont _font;
        private BitmapData _originalBitmapData;
        private string _text;

        public virtual string text
        {
            get => _text;
            set
            {
                _text = value;
                disposeOriginalBitmap();
                if (bitmapData != null) bitmapData.dispose();
                bitmapData = _font != null ? _font.renderText(value) : null;
                smoothing = true;
                if (bitmapData != null) width = bitmapData.width;
            }
        }

        public virtual void applyColorTransform(ColorTransform ct)
        {
            if (ct == null)
            {
                if (_originalBitmapData != null)
                {
                    if (bitmapData != null) bitmapData.dispose();
                    bitmapData = _originalBitmapData.clone();
                }
                return;
            }

            if (bitmapData == null) return;
            if (_originalBitmapData == null) _originalBitmapData = bitmapData.clone();
            bitmapData.colorTransform(new Rectangle(0, 0, bitmapData.width, bitmapData.height), ct);
        }

        public virtual void dispose()
        {
            disposeOriginalBitmap();
            if (bitmapData != null)
            {
                bitmapData.dispose();
                bitmapData = null;
            }
        }

        private void disposeOriginalBitmap()
        {
            if (_originalBitmapData != null)
            {
                _originalBitmapData.dispose();
                _originalBitmapData = null;
            }
        }
    }
}
