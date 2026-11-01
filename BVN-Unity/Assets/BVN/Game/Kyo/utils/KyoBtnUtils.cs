using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>Turns display objects into simple buttons with a press effect.</summary>
    public class KyoBtnUtils
    {
        private static Dictionary<object, object> _btnMap = new Dictionary<object, object>();
        private static ColorTransform _blackTransform = new ColorTransform(1, 1, 1, 1, -20, -20, -20);
        private static ColorTransform _emptyTransform = new ColorTransform();
        private static TweenLite _btnTween;
        private static object _curOnClick;
        private static object _curOnClickParam;

        public static void initSimpleBtn(DisplayObject d, Delegate click = null, object clickParam = null)
        {
            _btnMap[d] = new ASObject
            {
                ["x"] = d.x,
                ["y"] = d.y,
                ["scaleX"] = d.scaleX,
                ["scaleY"] = d.scaleY,
                ["click"] = click,
                ["clickParam"] = clickParam
            };
            if (click != null) d.addEventListener<MouseEvent>(MouseEvent.CLICK, simpleBtnHandler);
        }

        public static void disposeSimpleBtn(DisplayObject d)
        {
            if (d == null) return;
            _btnMap[d] = null;
            d.removeEventListener<MouseEvent>(MouseEvent.CLICK, simpleBtnHandler);
        }

        public static void initBtn(DisplayObject d, Delegate click = null, object clickParam = null, int effectType = 1)
        {
            if (d is Sprite sp)
            {
                sp.mouseChildren = false;
                sp.buttonMode = true;
            }

            _btnMap[d] = new ASObject
            {
                ["x"] = d.x,
                ["y"] = d.y,
                ["scaleX"] = d.scaleX,
                ["scaleY"] = d.scaleY,
                ["effectType"] = effectType,
                ["click"] = click,
                ["clickParam"] = clickParam
            };

            d.addEventListener<MouseEvent>(MouseEvent.MOUSE_DOWN, btnHandler);
            d.addEventListener<MouseEvent>(MouseEvent.MOUSE_UP, btnHandler);
            if (click != null) d.addEventListener<MouseEvent>(MouseEvent.CLICK, btnHandler);
        }

        public static void disposeBtn(DisplayObject d)
        {
            if (d == null) return;
            d.removeEventListener<MouseEvent>(MouseEvent.MOUSE_DOWN, btnHandler);
            d.removeEventListener<MouseEvent>(MouseEvent.MOUSE_UP, btnHandler);
            d.removeEventListener<MouseEvent>(MouseEvent.CLICK, btnHandler);
            _btnMap[d] = null;
        }

        private static void doBtnEffect(DisplayObject d, object param, bool resume = false)
        {
            switch (Int(Get(param, "effectType")))
            {
                case 2:
                    if (!resume)
                    {
                        d.y += 10;
                        d.transform.colorTransform = _blackTransform;
                    }
                    else
                    {
                        d.transform.colorTransform = _emptyTransform;
                        _btnTween = TweenLite.to(d, 0.5, new ASObject
                        {
                            ["y"] = Get(param, "y"),
                            ["ease"] = Elastic.easeOut,
                            ["onComplete"] = (Action)btnEffectFinish
                        });
                    }
                    break;
                case 1:
                    d.transform.colorTransform = !resume ? _blackTransform : _emptyTransform;
                    break;
            }
        }

        private static void btnEffectFinish()
        {
            if (_curOnClick != null)
            {
                if (_curOnClickParam != null) Call(_curOnClick, _curOnClickParam);
                else Call(_curOnClick);
                _curOnClick = null;
                _curOnClickParam = null;
            }
        }

        private static void simpleBtnHandler(MouseEvent e)
        {
            _btnMap.TryGetValue(e.currentTarget, out var o);
            var click = Get(o, "click");
            if (click != null)
            {
                var clickParam = Get(o, "clickParam");
                if (clickParam != null) Call(click, clickParam);
                else Call(click);
            }
        }

        private static void btnHandler(MouseEvent e)
        {
            var d = e.currentTarget as DisplayObject;
            _btnMap.TryGetValue(d, out var o);
            switch (e.type)
            {
                case MouseEvent.MOUSE_DOWN:
                    if (_btnTween != null && _btnTween.active) _btnTween.kill();
                    doBtnEffect(d, o, false);
                    break;
                case MouseEvent.MOUSE_UP:
                    if (_btnTween != null && _btnTween.active) _btnTween.kill();
                    doBtnEffect(d, o, true);
                    break;
                case MouseEvent.CLICK:
                    _curOnClick = Get(o, "click");
                    _curOnClickParam = Get(o, "clickParam");
                    if (Int(Get(o, "effectType")) == 1) btnEffectFinish();
                    break;
            }
        }
    }
}
