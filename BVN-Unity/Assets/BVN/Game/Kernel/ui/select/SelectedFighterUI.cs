using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>已选角色条（linkage $select$SP_selectBarItemP1 / P2），显示大头像、名字与出场顺序。</summary>
    public class SelectedFighterUI : EventDispatcher
    {
        public SelectedFighterUI(Sprite ui)
        {
            this.ui = ui;

            ui.mouseChildren = false;

            if (GameUI.SHOW_CN_TEXT)
            {
                _text = new BitmapText(true, 0xffffff, new List<object> { new GlowFilter(0, 1, 3, 3, 3) });

                if (isLinkedAs(ui, "$select$SP_selectBarItemP1"))
                {
                    UIUtils.formatText(_text.textfield, new ASObject { ["color"] = 0xffffff, ["size"] = 14, ["align"] = TextFormatAlign.RIGHT });
                    _text.width = ui.width - 10;
                }
                else
                {
                    UIUtils.formatText(_text.textfield, new ASObject { ["color"] = 0xffffff, ["size"] = 14, ["align"] = TextFormatAlign.LEFT });
                    _text.x = 10;
                    _text.width = ui.width - 10;
                }

                _text.y = ui.height - 30;
                ui.addChild(_text);
            }
        }

        public Sprite ui;
        public double trueY = 0;
        private int _fighterIndex = -1;
        private DisplayObject _face;
        private FighterVO _fighter;
        private BitmapText _text;
        private double _uiWidth;

        /// <summary>AS3 `ui is $select$SP_xxx` for a linkage class of a converted library.</summary>
        private static bool isLinkedAs(DisplayObject d, string className)
        {
            var mc = d as MovieClip;
            if (mc == null || mc.Symbol == null) return false;
            if (mc.Library != null && mc.Library.GetLinkedSymbol(className) == mc.Symbol) return true;
            return mc.Symbol.Name == className || mc.Symbol.BaseClass == className;
        }

        public virtual void mouseEnabled(bool v)
        {
            if (v)
            {
                if (GameConfig.TOUCH_MODE)
                {
                    ui.addEventListener(TouchEvent.TOUCH_TAP, mouseHandler);
                }
                else
                {
                    ui.buttonMode = true;
                    ui.addEventListener(MouseEvent.MOUSE_OVER, mouseHandler);
                    ui.addEventListener(MouseEvent.CLICK, mouseHandler);
                }
            }
            else
            {
                ui.buttonMode = false;
                ui.removeEventListener(TouchEvent.TOUCH_TAP, mouseHandler);
                ui.removeEventListener(MouseEvent.MOUSE_OVER, mouseHandler);
                ui.removeEventListener(MouseEvent.CLICK, mouseHandler);
            }
        }

        public virtual void destroy()
        {
            if (ui != null)
            {
                ui.removeEventListener(TouchEvent.TOUCH_TAP, mouseHandler);
                ui.removeEventListener(MouseEvent.MOUSE_OVER, mouseHandler);
                ui.removeEventListener(MouseEvent.CLICK, mouseHandler);
            }
            if (_text != null)
            {
                _text.destroy();
                _text = null;
            }
        }

        public virtual void setFighter(FighterVO vo)
        {
            if (vo == null)
            {
                return;
            }

            _fighter = vo;

            if (_text != null)
            {
                _text.text = vo.name;
            }

            var ctm = ui.getChildByName("ct") as DisplayObjectContainer;
            var ct = ctm != null ? ctm.getChildByName("ct") as Sprite : null;
            if (ct != null)
            {
                if (_face != null)
                {
                    try
                    {
                        ct.removeChild(_face);
                    }
                    catch (Exception)
                    {
                    }
                }

                var face = AssetManager.I.getFighterFaceBig(vo);
                if (face != null)
                {
                    _face = face;
                    ct.addChild(face);
                }
            }
        }

        public virtual FighterVO getFighter()
        {
            return _fighter;
        }

        public virtual int getFighterIndex()
        {
            return _fighterIndex;
        }

        public virtual void setFighterIndex(int index)
        {
            _fighterIndex = index;

            var au = ResUtils.I.createDisplayObject(ResUtils.swfLib.select, "$loading$MC_selectText") as MovieClip;
            au.gotoAndStop(index);
            ui.addChild(au);
            if (isLinkedAs(ui, "$select$SP_selectBarItemP1"))
            {
                au.x = -8;
            }
            else
            {
                au.x = 252 - au.width;
            }
            au.y = -5;

            mouseEnabled(false);
        }

        public virtual void setAssister()
        {
            var au = ResUtils.I.createDisplayObject(ResUtils.swfLib.select, "$loading$MC_selectText") as MovieClip;
            au.gotoAndStop(4);
            ui.addChild(au);
            if (isLinkedAs(ui, "$select$SP_selectBarItemP1"))
            {
                au.x = -8;
            }
            else
            {
                au.x = 176;
            }
            au.y = -5;
        }

        private void mouseHandler(Event e)
        {
            dispatchEvent(e);
        }
    }
}
