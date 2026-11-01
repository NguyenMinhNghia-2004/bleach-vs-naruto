using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>Language item (flag + text + selection bar).</summary>
    public class CountryItem : Sprite
    {
        // gap between flag and text
        private const int GAP = 25;
        // height coefficient
        private const double HEIGHT_COEFFICIENT = 1.5;
        // width coefficient
        private const double WIDTH_COEFFICIENT = 2;

        public CountryItem()
        {
            _top = new Sprite();

            _mc = ResUtils.I.createDisplayObject(ResUtils.swfLib.language, "$language$MC_country") as MovieClip;
            _txt = ResUtils.I.createDisplayObject(ResUtils.swfLib.language, "$language$MC_text") as MovieClip;
            _base = ResUtils.I.createDisplayObject(ResUtils.swfLib.language, "$language$MC_base") as MovieClip;

            _top.addChild(_mc);
            _top.addChild(_txt);

            _pMc = _mc.getChildByName("p") as MovieClip;
            _cMc = _mc.getChildByName("c") as MovieClip;

            _cMc.stop();
            _txt.stop();

            _txt.x = _mc.width + GAP;

            addChild(_base);
            addChild(_top);

            _base.scaleX = 0;
            _base.height = height * HEIGHT_COEFFICIENT;
            _base.x = width / 2;
            _base.y = height / 2;
        }

        // flag symbol
        private MovieClip _mc;
        // text symbol
        private MovieClip _txt;

        // flag progress symbol
        private MovieClip _pMc;
        // flag pattern symbol
        private MovieClip _cMc;

        // bottom bar symbol
        private MovieClip _base;

        /// <summary>Width (of top). AS3 override of the width getter; the setter keeps the base behaviour.</summary>
        public new virtual double width
        {
            get => _top.width;
            set => base.width = value;
        }

        /// <summary>Height (of top).</summary>
        public new virtual double height
        {
            get => _top.height;
            set => base.height = value;
        }

        // top container
        private Sprite _top;

        /// <summary>Top container.</summary>
        public virtual Sprite top => _top;

        // current language
        private string _language;

        /// <summary>Current language.</summary>
        public virtual string language
        {
            get => _language;
            set
            {
                _language = value;

                if (MCUtils.hasFrameLabel(_cMc, value) && MCUtils.hasFrameLabel(_txt, value))
                {
                    _cMc.gotoAndStop(value);
                    _txt.gotoAndStop(value);
                }
                else
                {
                    _cMc.gotoAndStop(1);
                }

                refreshLayout();
            }
        }

        // font SWF path (relative to assets)
        private string _fontUrl;

        /// <summary>Font SWF path (relative to assets).</summary>
        public virtual string fontUrl
        {
            get => _fontUrl;
            set => _fontUrl = value;
        }

        // selected state
        private bool _selected;
        /** @private target scaleX of the selection bar (relative to the symbol's natural width) */
        private double _expandScale = 1;
        /** @private natural width of the bar (scaleX = 1) */
        private double _baseNaturalW = 0;

        /// <summary>Whether this item is selected.</summary>
        public virtual bool selected
        {
            get => _selected;
            set
            {
                bool b = value;
                // same state: nothing to do
                if (_selected == b)
                {
                    return;
                }

                _selected = b;

                if (_expandScale <= 0 || _baseNaturalW < 1)
                {
                    refreshLayout();
                }

                TweenLite.killTweensOf(_base);
                // tween scaleX instead of width to avoid relayout every frame
                TweenLite.to(_base, 0.2, new ASObject { ["scaleX"] = b ? _expandScale : 0 });
            }
        }

        /// <summary>Pre-warms the selection bar and tween to avoid a hitch on first hover.</summary>
        public virtual void warmUp()
        {
            if (_expandScale <= 0 || _baseNaturalW < 1)
            {
                refreshLayout();
            }

            double targetScale = _expandScale > 0 ? _expandScale : 1;
            TweenLite.killTweensOf(_base);
            _base.scaleX = targetScale;

            // force rasterisation (reading width is not enough)
            forceRasterizeBase();

            // non-zero scale tween to exercise the real hover path
            TweenLite.to(_base, 0.01, new ASObject { ["scaleX"] = targetScale });
            TweenLite.killTweensOf(_base);
            _base.scaleX = _selected ? targetScale : 0;
        }

        // AS3 overrides addEventListener / removeEventListener to forward to _top.
        // EventDispatcher methods are not virtual in BVN.Flash, so these hide them (effective when typed as CountryItem).

        public new virtual void addEventListener(string type, Action<Event> listener, bool useCapture = false, int priority = 0,
            bool useWeakReference = false)
        {
            _top.addEventListener(type, listener, useCapture, priority, useWeakReference);
        }

        public new virtual void addEventListener<T>(string type, Action<T> listener, bool useCapture = false, int priority = 0,
            bool useWeakReference = false) where T : Event
        {
            _top.addEventListener<T>(type, listener, useCapture, priority, useWeakReference);
        }

        public new virtual void addEventListener(string type, Action listener, bool useCapture = false, int priority = 0,
            bool useWeakReference = false)
        {
            _top.addEventListener(type, listener, useCapture, priority, useWeakReference);
        }

        public new virtual void removeEventListener(string type, Delegate listener, bool useCapture = false)
        {
            _top.removeEventListener(type, listener, useCapture);
        }

        public new virtual void removeEventListener(string type, Action<Event> listener, bool useCapture = false)
        {
            _top.removeEventListener(type, listener, useCapture);
        }

        public new virtual void removeEventListener<T>(string type, Action<T> listener, bool useCapture = false) where T : Event
        {
            _top.removeEventListener<T>(type, listener, useCapture);
        }

        public new virtual void removeEventListener(string type, Action listener, bool useCapture = false)
        {
            _top.removeEventListener(type, listener, useCapture);
        }

        /// <summary>Destroy.</summary>
        public virtual void destroy()
        {
            TweenLite.killTweensOf(_base);

            _pMc = null;
            _cMc = null;

            _mc = null;
            _txt = null;

            _top = null;
            _base = null;

            _fontUrl = null;
        }

        /// <summary>@private Refreshes bar height and expand scale from the current top size.</summary>
        private void refreshLayout()
        {
            double topW = _top.width;
            double topH = _top.height;

            _base.scaleX = 1;
            if (_baseNaturalW < 1)
            {
                _baseNaturalW = _base.width;
                if (_baseNaturalW < 1)
                {
                    _baseNaturalW = 1;
                }
            }

            _expandScale = (topW * WIDTH_COEFFICIENT) / _baseNaturalW;
            _base.height = topH * HEIGHT_COEFFICIENT;
            _base.x = topW / 2;
            _base.y = topH / 2;
            _base.scaleX = _selected ? _expandScale : 0;
        }

        /// <summary>@private Draws the bar into a temp bitmap to force one rasterisation.</summary>
        private void forceRasterizeBase()
        {
            if (_base == null)
            {
                return;
            }
            int w = (int)Math.Ceiling(Math.Abs(_base.width));
            int h = (int)Math.Ceiling(Math.Abs(_base.height));
            if (w < 1 || h < 1)
            {
                return;
            }
            try
            {
                var bd = new BitmapData(w, h, true, 0);
                var m = new Matrix();
                m.translate(w * 0.5, h * 0.5);
                bd.draw(_base, m);
                bd.dispose();
            }
            catch (Exception)
            {
            }
        }
    }
}
