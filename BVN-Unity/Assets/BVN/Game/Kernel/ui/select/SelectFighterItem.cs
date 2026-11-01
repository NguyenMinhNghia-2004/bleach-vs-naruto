using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>选人列表中的单个角色头像格子（linkage $select$SP_selectItem）。</summary>
    public class SelectFighterItem : EventDispatcher
    {
        public static string getIdByPoint(int X, int Y)
        {
            return X + "," + Y;
        }

        public SelectFighterItem(FighterVO fighterData, SelectCharListItemVO selectData, bool isMore = false) : base()
        {
            this.isMore = isMore;

            this.selectData = selectData;
            this.fighterData = fighterData;

            var face = AssetManager.I.getFighterFace(fighterData);
            if (face != null)
            {
                var ct = ui.getChildByName("ct") as DisplayObjectContainer;
                if (ct != null) ct.addChild(face);
            }

            ui.mouseChildren = false;
            ui.buttonMode = true;

            if (selectData != null && selectData.moreFighterIDs != null)
            {
                initMoreUI();
            }

            var more_bg = ui.getChildByName("more_bg");
            if (more_bg != null)
            {
                more_bg.visible = isMore;
                more_bg.mouseEnabled = false;
                if (more_bg is DisplayObjectContainer moreCt) moreCt.mouseChildren = false;
                var ct = new ColorTransform();
                ct.redOffset = 255;
                //			ct.greenOffset = 255;
                ct.blueOffset = -255;
                more_bg.transform.colorTransform = ct;
            }

            //        ui.warning.visible = fighterData.hasWarning;
            if (!fighterData.hasWarning)
            {
                var warning = ui.getChildByName("warning");
                if (warning != null) ui.removeChild(warning);
            }
        }

        public SelectCharListItemVO selectData;
        public FighterVO fighterData;

        public Sprite ui = ResUtils.I.createDisplayObject(ResUtils.swfLib.select, "$select$SP_selectItem") as Sprite;

        public Point position = new Point();

        public bool isMore = false;

        private Point faceSize = new Point(50, 50);

        private BitmapText _moreText;
        private Dictionary<string, Delegate> _listeners = new Dictionary<string, Delegate>();
        private Point _tweenFrom;
        private Point _tweenTo;

        public virtual string positionId => getIdByPoint((int)position.x, (int)position.y);

        /// <summary>
        /// AS3 override of EventDispatcher.addEventListener: registers on the ui, and the listener is
        /// called as listener(type:String, item:SelectFighterItem).
        /// </summary>
        public virtual void addEventListener(string type, Delegate listener, bool useCapture = false, int priority = 0,
            bool useWeakReference = false)
        {
            if (ui.hasEventListener(type))
            {
                return;
            }
            ui.addEventListener(type, selfHandler, useCapture, priority, useWeakReference);
            _listeners[type] = listener;
        }

        public virtual void setMoreNumberVisible(bool v)
        {
            if (_moreText != null)
            {
                _moreText.visible = v;
            }
        }

        public virtual void removeAllEventListener()
        {
            foreach (var kv in _listeners)
            {
                ui.removeEventListener(kv.Key, kv.Value);
            }
            _listeners = new Dictionary<string, Delegate>();
        }

        public virtual void initMoreTween(Point fromP, Point toP)
        {
            _tweenFrom = fromP;
            _tweenTo = toP;
        }

        public virtual void showMore(double delay = 0.1)
        {
            ui.x = _tweenFrom.x;
            ui.y = _tweenFrom.y;
            ui.mouseEnabled = false;
            TweenLite.to(ui, 0.2, new ASObject
            {
                ["x"] = _tweenTo.x,
                ["y"] = _tweenTo.y,
                ["ease"] = Back.easeOut,
                ["delay"] = delay,
                ["onComplete"] = (Action)(() =>
                {
                    if (ui != null) ui.mouseEnabled = true;
                })
            });
        }

        public virtual void hideMore()
        {
            TweenLite.to(ui, 0.1, new ASObject
            {
                ["x"] = _tweenFrom.x,
                ["y"] = _tweenFrom.y,
                ["onComplete"] = (Action)(() =>
                {
                    try
                    {
                        ui.parent.removeChild(ui);
                    }
                    catch (Exception)
                    {
                    }
                })
            });
        }

        public virtual void destroy()
        {
            if (ui != null)
            {
                removeAllEventListener();
            }
            if (ui != null && ui.parent != null)
            {
                try
                {
                    ui.parent.removeChild(ui);
                }
                catch (Exception)
                {
                }
                ui = null;
            }
        }

        private void initMoreUI()
        {
            var txt = new BitmapText(false, 0xffff00, new List<object> { new GlowFilter(0, 1, 5, 5, 3) });
            txt.width = 50;
            txt.defaultTextFormat.bold = true;
            txt.defaultTextFormat.color = 0xffff00;
            txt.defaultTextFormat.size = 16;
            txt.align = TextFormatAlign.RIGHT;
            txt.y = -3;
            txt.text = selectData.moreFighterIDs.Count + "+";
            txt.update();
            ui.addChild(txt);

            _moreText = txt;
        }

        private void selfHandler(Event e)
        {
            if (_listeners.TryGetValue(e.type, out var fn))
            {
                Call(fn, e.type, this);
            }
        }
    }
}
