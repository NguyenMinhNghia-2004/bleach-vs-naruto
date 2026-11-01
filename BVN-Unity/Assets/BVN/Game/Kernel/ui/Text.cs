using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class Text : BitmapText
    {
        public Text(uint color = 0xffffff, int size = 20) : base(true, color, new List<object> { new DropShadowFilter() })
        {
            font = "Arial";
            fontSize = size;
        }

        public override double textWidth
        {
            get => _tf.textWidth;
        }

        public override double textHeight
        {
            get => _tf.textHeight;
        }
    }
}
