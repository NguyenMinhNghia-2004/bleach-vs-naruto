using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>Clickable payment QR sprites that toggle between thumbnail and full-screen view.</summary>
    public class PayUtils
    {
        private static Rectangle _orgRect;

        public static Sprite getPaySp(Bitmap bp)
        {
            var sp = new Sprite();
            sp.buttonMode = true;
            sp.addChild(bp);
            sp.addEventListener<MouseEvent>(MouseEvent.MOUSE_UP, payMouseHandler, false, 0, true);
            return sp;
        }

        private static void payMouseHandler(MouseEvent e)
        {
            e.stopImmediatePropagation();
            e.stopPropagation();

            var sp = e.currentTarget as Sprite;
            if (sp == null)
            {
                return;
            }

            DisplayObject bp = sp.getChildAt(0);

            if (_orgRect != null && sp.width != _orgRect.width)
            {
                sp.graphics.clear();
                if (bp != null)
                {
                    bp.scaleX = bp.scaleY = 1;
                    bp.x = 0;
                    bp.y = 0;
                }
                sp.x = _orgRect.x;
                sp.y = _orgRect.y;
                sp.width = _orgRect.width;
                sp.height = _orgRect.height;

                _orgRect = null;
                return;
            }

            _orgRect = new Rectangle(sp.x, sp.y, sp.width, sp.height);

            sp.graphics.beginFill(0, 0.7);
            sp.graphics.drawRect(0, 0, GameConfig.GAME_SIZE.x, GameConfig.GAME_SIZE.y + 20);
            sp.graphics.endFill();

            sp.parent.addChild(sp);

            sp.x = 0;
            sp.y = 0;
            sp.width = GameConfig.GAME_SIZE.x;
            sp.height = GameConfig.GAME_SIZE.y;

            if (bp != null)
            {
                bp.scaleX = bp.scaleY = 0.8;
                bp.x = (GameConfig.GAME_SIZE.x - bp.width) / 2;
                bp.y = (GameConfig.GAME_SIZE.y - bp.height) / 2;
            }
        }
    }
}
