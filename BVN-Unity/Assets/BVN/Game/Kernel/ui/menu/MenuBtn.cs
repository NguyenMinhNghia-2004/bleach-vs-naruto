using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class MenuBtn : EventDispatcher
    {
        /** @private */
        private static bool _commonWarmed;

        /// <summary>
        /// Pre-warms the menu button background rasterisation and hover scale path (idempotent;
        /// not added to the display list, no sound). Call after loadBasic / font1.
        /// </summary>
        public static void warmCommon()
        {
            if (_commonWarmed)
            {
                return;
            }
            _commonWarmed = true;

            try
            {
                var btn = new MenuBtn("WARM", "");
                var bg = btn.ui.getChildByName("bg") as MovieClip;
                bg.visible = true;
                bg.scaleX = 1;
                bg.gotoAndStop(1);
                forceRasterizeDisplay(bg);

                bg.gotoAndStop(2);
                forceRasterizeDisplay(bg);
                bg.gotoAndStop(1);

                TweenLite.killTweensOf(bg);
                bg.scaleX = 0.01;
                TweenLite.to(bg, 0.01, new ASObject { ["scaleX"] = 1 });
                TweenLite.killTweensOf(bg);
                bg.scaleX = 1;
                bg.visible = false;

                btn.dispose();
            }
            catch (Exception)
            {
            }
        }

        /// <summary>@private Forces rasterisation of a display object.</summary>
        private static void forceRasterizeDisplay(object target)
        {
            if (!Bool(target))
            {
                return;
            }
            int w = (int)Math.Ceiling(Math.Abs(Num(Get(target, "width"))));
            int h = (int)Math.Ceiling(Math.Abs(Num(Get(target, "height"))));
            if (w < 1 || h < 1)
            {
                return;
            }
            try
            {
                var bd = new BitmapData(w, h, true, 0);
                bd.draw(target as DisplayObject);
                bd.dispose();
            }
            catch (Exception)
            {
            }
        }

        public MenuBtn(string label, string cn = "", Delegate func = null)
        {
            this.cn = cn;
            this.label = label;
            this.func = func;

            ui = ResUtils.I.createDisplayObject(ResUtils.swfLib.common, "$common$MC_menuBtn") as MovieClip;

            ui.buttonMode = true;
            ui.mouseChildren = false;

            _bitmapText = new BitmapFontText(AssetManager.I.getFont("font1"));
            _bitmapText.text = label;
            _bitmapText.x = -_bitmapText.width / 2;
            ui.addChild(_bitmapText);

            var bg = uiBg;
            bg.mouseChildren = bg.mouseEnabled = false;
            bg.visible = false;

            if (GameUI.SHOW_CN_TEXT)
            {
                _cnTxt = new BitmapText();
                UIUtils.formatText(_cnTxt.textfield, new ASObject { ["size"] = 18 });
                _cnTxt.text = cn;
                _cnTxt.x = 10;
                _cnTxt.y = 43;
                bg.addChild(_cnTxt);
            }
        }

        public MovieClip ui;
        public string cn;
        public string label;
        public Delegate func;
        public double height = 75;
        public int index;
        public List<object> children;
        private BitmapFontText _bitmapText;
        private BitmapText _cnTxt;
        private ASObject _listeners = new ASObject();
        private bool _isOpen;

        /// <summary>ui.bg (named timeline child of $common$MC_menuBtn).</summary>
        private MovieClip uiBg => ui.getChildByName("bg") as MovieClip;

        /// <summary>
        /// AS3 override of addEventListener: listener signature is (type:String, target:MenuBtn).
        /// EventDispatcher.addEventListener is not virtual in BVN.Flash, so this is an overload taking a Delegate.
        /// </summary>
        public virtual void addEventListener(string type, Delegate listener, bool useCapture = false, int priority = 0,
            bool useWeakReference = false)
        {
            if (ui.hasEventListener(type))
            {
                return;
            }
            ui.addEventListener(type, (Action<Event>)selfHandler, useCapture, priority, useWeakReference);
            _listeners[type] = listener;
        }

        public virtual void removeAllEventListener()
        {
            foreach (var i in Keys(_listeners))
            {
                ui.removeEventListener(Str(i), _listeners[Str(i)] as Delegate);
            }
            _listeners = new ASObject();
        }

        public virtual bool isHover()
        {
            return uiBg.visible;
        }

        public virtual void hover()
        {
            if (_isOpen)
            {
                return;
            }
            var bg = uiBg;
            if (bg.visible)
            {
                return;
            }
            bg.visible = true;
            double sx = bg.scaleX;
            bg.scaleX = 0.01;
            TweenLite.to(bg, 0.2, new ASObject { ["scaleX"] = sx });
            SoundCtrl.I.sndSelect();
        }

        public virtual void normal()
        {
            if (_isOpen)
            {
                return;
            }
            uiBg.visible = false;
        }

        public virtual void select(Delegate back = null)
        {
            ui.alpha = -1;
            TweenLite.to(ui, 1, new ASObject { ["alpha"] = 1, ["ease"] = Elastic.easeOut, ["onComplete"] = back });
            SoundCtrl.I.sndConfrim();
        }

        public virtual void openChild()
        {
            if (_isOpen)
            {
                return;
            }
            _isOpen = true;
            uiBg.gotoAndStop(2);
            var ct = new ColorTransform();
            ct.redOffset = 50;
            ct.greenOffset = -30;
            ct.blueOffset = -30;
            _bitmapText.applyColorTransform(ct);
        }

        public virtual void closeChild()
        {
            if (!_isOpen)
            {
                return;
            }
            _isOpen = false;
            uiBg.gotoAndStop(1);

            _bitmapText.applyColorTransform(null);
        }

        public virtual void dispose()
        {
            if (_bitmapText != null)
            {
                _bitmapText.dispose();
                _bitmapText = null;
            }
            removeAllEventListener();

            if (children != null)
            {
                foreach (var o in children)
                {
                    var b = (MenuBtn)o;
                    b.dispose();
                }
                children = null;
            }

            if (_cnTxt != null)
            {
                _cnTxt.destroy();
                _cnTxt = null;
            }
        }

        public virtual void childMode()
        {
            var ct = new ColorTransform();
            ct.redOffset = 50;
            ct.greenOffset = -30;
            ct.blueOffset = -30;

            _bitmapText.applyColorTransform(ct);
            _bitmapText.scaleX = _bitmapText.scaleY = 0.75;
            _bitmapText.x = -_bitmapText.width / 2;

            var bg = uiBg;
            bg.scaleY = 0.75;
            bg.scaleX = 0.9;
            bg.gotoAndStop(2);
            height = 55;
        }

        private void selfHandler(Event e)
        {
            Call(_listeners[e.type], e.type, this);
        }
    }
}
