using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>分页圆点组。</summary>
    public class DotsGroupUI : Sprite
    {
        public Delegate onDotClick;
        private List<object> _dotArr;

        public virtual void update(int total)
        {
            _dotArr = new List<object>();

            for (int i = 0; i < total; i++)
            {
                var dot = new DotItemUI();
                dot.page = i + 1;

                addChild(dot.getUI());

                _dotArr.Add(dot);

                dot.getUI().x = i * 40;

                if (GameConfig.TOUCH_MODE)
                {
                    dot.getUI().addEventListener<TouchEvent>(TouchEvent.TOUCH_TAP, touchHandler);
                }
                else
                {
                    dot.getUI().addEventListener<MouseEvent>(MouseEvent.CLICK, mouseHandler);
                }

                if (i == 0)
                {
                    dot.focus(true);
                }
            }
        }

        public virtual void updateByPage(int v)
        {
            if (_dotArr == null) return;
            foreach (var o in _dotArr)
            {
                var d = o as DotItemUI;
                d.focus(d.page == v);
            }
        }

        public virtual void destroy()
        {
            if (_dotArr == null) return;
            foreach (var o in _dotArr)
            {
                var d = o as DotItemUI;
                d.getUI().removeEventListener<TouchEvent>(TouchEvent.TOUCH_TAP, touchHandler);
                d.getUI().removeEventListener<MouseEvent>(MouseEvent.CLICK, mouseHandler);
            }
        }

        private void doClick(object target)
        {
            if (onDotClick == null)
            {
                return;
            }

            var curUI = target as DisplayObject;

            foreach (var o in new List<object>(_dotArr))
            {
                var d = o as DotItemUI;
                if (d.getUI() == curUI)
                {
                    Call(onDotClick, d.page);
                }
            }
        }

        private void mouseHandler(MouseEvent e)
        {
            doClick(e.currentTarget);
        }

        private void touchHandler(TouchEvent e)
        {
            doClick(e.currentTarget);
        }
    }
}
