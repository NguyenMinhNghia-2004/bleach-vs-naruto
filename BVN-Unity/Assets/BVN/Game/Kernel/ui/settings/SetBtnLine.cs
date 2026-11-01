using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class SetBtnLine : Sprite
    {
        /** @private */
        private static bool _commonWarmed;

        /// <summary>Pre-warms the underline draw and scale tween path (idempotent; not added to the display list).</summary>
        public static void warmCommon()
        {
            if (_commonWarmed)
            {
                return;
            }
            _commonWarmed = true;

            try
            {
                var line = new SetBtnLine();
                line.warmUp();
            }
            catch (Exception)
            {
            }
        }

        public SetBtnLine()
        {
            mouseChildren = mouseEnabled = false;

            _line = new Sprite();
            addChild(_line);

            if (GameUI.SHOW_CN_TEXT)
            {
                _txt = new TextField();
                UIUtils.formatText(_txt, new ASObject { ["color"] = 0xfded65, ["size"] = 16, ["align"] = TextFormatAlign.RIGHT });
                _txt.y = 4;
                addChild(_txt);
            }
        }

        private TextField _txt;
        private Sprite _line;

        /// <summary>Warms this instance's line drawing and tween path.</summary>
        public virtual void warmUp()
        {
            show(200, "WARM");
            TweenLite.killTweensOf(_line);
            _line.scaleX = 1;
            hide();
        }

        public virtual void show(double width, string text)
        {
            _line.graphics.clear();
            _line.graphics.lineStyle(1, 0xfded65, 1);
            _line.graphics.lineTo(width, 0);

            _line.scaleX = 0.1;

            TweenLite.to(_line, 0.3, new ASObject { ["scaleX"] = 1 });

            this.visible = true;

            if (_txt != null)
            {
                _txt.width = width;
                _txt.text = text;
            }
        }

        public virtual void hide()
        {
            this.visible = false;
            if (_txt != null)
            {
                _txt.text = "";
            }
        }
    }
}
