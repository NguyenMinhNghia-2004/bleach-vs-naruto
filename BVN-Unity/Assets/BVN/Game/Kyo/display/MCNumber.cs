using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>Displays a number using one MovieClip per digit (digit d shown at frame startFrame + d).</summary>
    public class MCNumber : Sprite
    {
        /// <param name="mc">Class of the digit clip (System.Type or LinkedClass).</param>
        public MCNumber(object mc, uint number, int startFrame = 1, double mcWidth = -1, uint bits = 0)
        {
            _mc = mc;
            _bits = bits;
            this.startFrame = startFrame;
            this.mcWidth = mcWidth;
            this.number = number;
        }

        public double mcWidth = -1;
        public int startFrame;
        protected object _mc;
        protected List<DisplayObject> _mcs = new List<DisplayObject>();
        protected uint _bits;
        protected uint _number;

        public virtual uint number
        {
            get => _number;
            set
            {
                if (value == _number && _mcs.Count > 0) return;
                _number = value;

                foreach (var m in _mcs) removeChild(m);
                _mcs.Clear();

                string numStr = value.ToString();
                while (numStr.Length < _bits) numStr = "0" + numStr;

                double xx = 0;
                for (int i = 0; i < numStr.Length; i++)
                {
                    DisplayObject digit = createNum(numStr[i] - '0');
                    digit.x = xx;
                    xx += mcWidth == -1 ? digit.width : mcWidth;
                }
            }
        }

        protected virtual DisplayObject createNum(int i)
        {
            var mc = (MovieClip)New(_mc);
            mc.gotoAndStop(startFrame + i);
            addChild(mc);
            _mcs.Add(mc);
            return mc;
        }
    }
}
