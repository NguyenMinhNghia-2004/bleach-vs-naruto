using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class CloudView
    {
        public CloudView(double X, double Y)
        {
            mc = ResUtils.I.createDisplayObject(ResUtils.swfLib.big_map, "$big_map$MC_cloud") as MovieClip;
            mc.x = X;
            mc.y = Y;
            mc.alpha = 0.2 + random() * 0.3;
            mc.gotoAndStop(1 + (int)(random() * mc.totalFrames));
            speed = 0.02 + random() * 0.1;
        }

        public double speed = 0;
        public MovieClip mc;

        public virtual bool render()
        {
            mc.y -= speed;
            return mc.y < -mc.height + 30;
        }
    }
}
