using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>Filled rectangle Sprite.</summary>
    public class Box : Sprite
    {
        public Box(double width, double height, int color = (int)KyoColor.BLACK, double alpha = 1, Point origin = null) : base()
        {
            graphics.beginFill((uint)color, alpha);
            graphics.drawRect(origin != null ? -origin.x : 0, origin != null ? -origin.y : 0, width, height);
            graphics.endFill();
        }
    }
}
