using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class DebugSprite : Sprite
    {
        public DebugSprite(uint color, DisplayObject target = null)
        {
            double w = target.width < 30 ? 30 : target.width;
            double h = target.height < 30 ? 30 : target.height;

            this.x = target.x;
            this.y = target.y;
            this.graphics.beginFill(color, 1);
            this.graphics.drawRect(0, 0, w, h);

            this.target = target;
        }

        public DisplayObject target;

        public virtual void applySet()
        {
            target.x = x;
            target.y = y;

            trace("================== debug apply set ========================");
            trace("x:", target.x, "y:", target.y, "width:", target.width, "height:", target.height);
            trace("-----------------------------------------------------------");
        }

        public virtual void destroy()
        {
            target = null;
            try
            {
                this.parent.removeChild(this);
            }
            catch (Exception)
            {
            }
        }
    }
}
