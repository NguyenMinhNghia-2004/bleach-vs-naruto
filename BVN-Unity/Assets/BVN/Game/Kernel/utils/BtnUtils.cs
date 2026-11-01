using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>
    /// 游戏 UI 按钮：触控/鼠标、点击音效与按下色变。
    /// 玩法界面首选本类。壳 LAN 的 Tween 按钮用库 KyoBtnUtils；两套依赖不同，勿合并。
    /// </summary>
    public class BtnUtils
    {
        private class BtnVO
        {
            public Delegate handler;
            public object target;
            public ColorTransform transform;
        }

        private static Dictionary<object, object> _btnMap = new Dictionary<object, object>();

        public static void btnMode(Sprite btn, bool buttonMode = true, bool mouseChildren = false)
        {
            if (btn == null)
            {
                return;
            }

            btn.buttonMode = buttonMode;
            btn.mouseChildren = mouseChildren;
        }

        public static void initBtn(DisplayObject btn, Delegate handler, object target = null)
        {
            if (btn == null)
            {
                return;
            }

            if (!Bool(target)) target = btn;

            _btnMap[btn] = new BtnVO
            {
                handler = handler, target = target, transform = KyoDisplayUtils.cloneColorTransform(btn.transform.colorTransform)
            };

            if (GameConfig.TOUCH_MODE)
            {
                btn.addEventListener<TouchEvent>(TouchEvent.TOUCH_END, touchHandler);
            }
            else
            {
                btn.addEventListener<MouseEvent>(MouseEvent.MOUSE_OVER, btnHandler);
                btn.addEventListener<MouseEvent>(MouseEvent.MOUSE_OUT, btnHandler);
                btn.addEventListener<MouseEvent>(MouseEvent.CLICK, btnHandler);
            }
        }

        public static void destroyBtn(DisplayObject btn)
        {
            if (btn == null)
            {
                return;
            }

            btn.removeEventListener<TouchEvent>(TouchEvent.TOUCH_END, touchHandler);

            btn.removeEventListener<MouseEvent>(MouseEvent.MOUSE_OVER, btnHandler);
            btn.removeEventListener<MouseEvent>(MouseEvent.MOUSE_OUT, btnHandler);
            btn.removeEventListener<MouseEvent>(MouseEvent.CLICK, btnHandler);

            _btnMap.Remove(btn);
        }

        private static void overEffect(DisplayObject btn, bool v)
        {
            _btnMap.TryGetValue(btn, out var o);
            var obj = o as BtnVO;
            var orgCT = obj?.transform;

            if (v)
            {
                var ct = KyoDisplayUtils.cloneColorTransform(orgCT);
                ct.redOffset += 30;
                ct.greenOffset += 30;
                ct.blueOffset += 30;
                btn.transform.colorTransform = ct;
                SoundCtrl.I.sndSelect();
            }
            else
            {
                btn.transform.colorTransform = orgCT;
            }
        }

        private static void applyBtnFunc(DisplayObject btn)
        {
            if (btn == null || !_btnMap.TryGetValue(btn, out var oo))
            {
                return;
            }
            var o = oo as BtnVO;
            if (o == null || o.handler == null)
            {
                return;
            }

            SoundCtrl.I.sndConfrim();
            Call(o.handler, o.target);
        }

        private static void btnHandler(MouseEvent e)
        {
            var btn = e.currentTarget as DisplayObject;

            switch (e.type)
            {
                case MouseEvent.CLICK:
                    applyBtnFunc(btn);
                    break;
                case MouseEvent.MOUSE_OVER:
                    overEffect(btn, true);
                    break;
                case MouseEvent.MOUSE_OUT:
                    overEffect(btn, false);
                    break;
            }
        }

        private static void touchHandler(TouchEvent e)
        {
            var btn = e.currentTarget as DisplayObject;

            switch (e.type)
            {
                case TouchEvent.TOUCH_END:
                    applyBtnFunc(btn);
                    break;
            }
        }
    }
}
