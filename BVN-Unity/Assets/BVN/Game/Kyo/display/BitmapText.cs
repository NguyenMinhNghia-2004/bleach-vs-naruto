using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>A TextField rasterised into a Bitmap (optionally with bitmap filters).</summary>
    public class BitmapText : Bitmap
    {
        public BitmapText(bool autoUpdate = true, uint color = KyoColor.BLACK, List<object> bitmapFilters = null)
        {
            this.autoUpdate = autoUpdate;
            this.smoothing = true;
            _bitmapFilters = bitmapFilters;
            _tf = new TextField();
            this.color = color;
        }

        public bool autoUpdate;
        protected TextField _tf;
        private TextFormat _format = new TextFormat();
        private List<object> _bitmapFilters;
        private double _width = 0;
        private double _height = 0;

        // AS3 overrode only the width/height setters; the compat DisplayObject properties are not virtual, so they are hidden.
        public new double width
        {
            get => base.width;
            set
            {
                _width = value;
                _tf.width = value;
            }
        }

        public new double height
        {
            get => base.height;
            set
            {
                _height = value;
                _tf.height = value;
            }
        }

        public virtual TextField textfield => _tf;

        public virtual string font
        {
            get => _format.font;
            set
            {
                _format.font = value;
                if (autoUpdate) update();
            }
        }

        public virtual object fontSize
        {
            get => _format.size;
            set
            {
                _format.size = value;
                if (autoUpdate) update();
            }
        }

        public virtual uint color
        {
            get => Uint(_format.color);
            set
            {
                _format.color = value;
                if (autoUpdate) update();
            }
        }

        public virtual string align
        {
            get => _format.align;
            set => _format.align = value;
        }

        public virtual string text
        {
            get => _tf.text;
            set
            {
                _tf.text = value;
                if (autoUpdate) update();
            }
        }

        public virtual TextFormat defaultTextFormat
        {
            get => _format;
            set
            {
                _format = value;
                if (autoUpdate) update();
            }
        }

        public virtual double textWidth
        {
            get => _tf.width;
            set => _tf.width = value;
        }

        public virtual double textHeight
        {
            get => _tf.height;
            set => _tf.height = value;
        }

        public virtual double leading
        {
            set => _format.leading = value;
        }

        public virtual double letterSpacing
        {
            set => _format.letterSpacing = value;
        }

        public virtual void multiLine(bool v)
        {
            _tf.multiline = v;
            _tf.wordWrap = true;
        }

        public virtual void setTextFormat(TextFormat f, int beginIndex = -1, int endIndex = -1)
        {
            _tf.setTextFormat(f, beginIndex, endIndex);
            if (autoUpdate) update();
        }

        public virtual double getTextWidth()
        {
            return _tf.textWidth;
        }

        public virtual void update()
        {
            if (_tf == null || string.IsNullOrEmpty(_tf.text)) return;

            int size = Int(_format.size) > 0 ? Int(_format.size) : 12;
            _tf.setTextFormat(_format);
            _tf.width = _width != 0 ? _width : _tf.textWidth + size;
            _tf.height = _height != 0 ? _height : _tf.textHeight + size;

            var bd = new BitmapData((int)_tf.width, (int)_tf.height, true, 0);
            bd.draw(_tf);

            if (_bitmapFilters != null)
            {
                var rect = new Rectangle(0, 0, bd.width, bd.height);
                var origin = new Point();
                foreach (var filter in _bitmapFilters) bd.applyFilter(bd, rect, origin, filter);
            }

            if (bitmapData != null) bitmapData.dispose();
            bitmapData = bd;
        }

        public virtual void destroy()
        {
            try
            {
                parent?.removeChild(this);
            }
            catch (Exception)
            {
            }
            if (bitmapData != null) bitmapData.dispose();
            _tf = null;
        }
    }
}
