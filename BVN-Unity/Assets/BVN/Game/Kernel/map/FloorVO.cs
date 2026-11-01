using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class FloorVO : IInstanceVO
    {
        // include '../include/Clone.as'
        public virtual IInstanceVO clone(params object[] arg)
        {
            return InstanceVOClone.clone(this, arg == null ? null : new List<object>(arg));
        }

        public double y = 0;
        public double xFrom = 0;
        public double xTo = 0;

        public virtual string toString()
        {
            return "FloorVO::{xFrom:" + NumStr(xFrom) + ",xTo:" + NumStr(xTo) + ",y:" + NumStr(y) + "}";
        }

        public override string ToString() => toString();

        public virtual bool hitTest(double X, double Y, double SPEED)
        {
            //SPEED = 5
            return Y > y - SPEED && Y < y + SPEED && X > xFrom && X < xTo;
        }
    }
}
