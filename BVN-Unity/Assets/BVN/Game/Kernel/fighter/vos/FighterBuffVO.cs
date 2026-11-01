using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class FighterBuffVO : IInstanceVO
    {
        // include/Clone.as
        /// <summary>克隆自身</summary>
        public virtual IInstanceVO clone(params object[] arg)
        {
            return InstanceVOClone.clone(this, new List<object>(arg ?? Array.Empty<object>()));
        }

        public FighterBuffVO(string param, double hold = 1)
        {
            this.param = param;
            //		this.value = value;
            this._holdFrame = hold * GameConfig.FPS_GAME;
            this.finished = false;
        }

        public string param;
        public double resumeValue = 0;
        public bool finished = false;
        private double _holdFrame = 1;

        public virtual void setHold(double v)
        {
            this._holdFrame = v * GameConfig.FPS_GAME;
            this.finished = false;
        }

        public virtual bool render()
        {
            if (finished)
            {
                return true;
            }

            if (--_holdFrame <= 0)
            {
                finished = true;
                return true;
            }
            return false;
        }
    }
}
