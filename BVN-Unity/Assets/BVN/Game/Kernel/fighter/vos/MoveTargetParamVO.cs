using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class MoveTargetParamVO : IInstanceVO
    {
        // include/Clone.as
        /// <summary>克隆自身</summary>
        public virtual IInstanceVO clone(params object[] arg)
        {
            return InstanceVOClone.clone(this, new List<object>(arg ?? Array.Empty<object>()));
        }

        /// <summary>Parameterless constructor (needed for reflective cloning; same as passing null).</summary>
        public MoveTargetParamVO() : this(null)
        {
        }

        /**
         * params:{
         * 		x:Number X位置
         * 		y:Number Y位置
         * 		followmc:String 按MC的位置移动目标，MC名称
         * 		speed:Number|{x:Number,y:Number} 移动速度，0或不设置时，直接移动到相应位置
         * }
         */
        public MoveTargetParamVO(object @params)
        {
            if (!Bool(@params))
            {
                return;
            }

            x = Has(@params, "x") ? Num(Get(@params, "x")) : 0;
            y = Has(@params, "y") ? Num(Get(@params, "y")) : 0;
            followMcName = Has(@params, "followmc") ? Str(Get(@params, "followmc")) : null;

            var sp = Get(@params, "speed");
            if (Bool(sp))
            {
                speed = new Point();
                if (Ops.IsNumeric(sp))
                {
                    speed.x = speed.y = Num(sp) * GameConfig.SPEED_PLUS;
                }
                else
                {
                    speed.x = Has(sp, "x") ? Num(Get(sp, "x")) * GameConfig.SPEED_PLUS : 0;
                    speed.y = Has(sp, "y") ? Num(Get(sp, "y")) * GameConfig.SPEED_PLUS : 0;
                }
            }
        }

        public double x = double.NaN;
        public double y = double.NaN;
        public string followMcName;
        public IGameSprite target;
        //移动速度，为NULL时，直接移动到相应位置
        public Point speed;

        public virtual void setTarget(IGameSprite v)
        {
            target = v;
            if (target is BaseGameSprite)
            {
//				(target as BaseGameSprite).isApplyG = false;
                (target as BaseGameSprite).setVelocity(0, 0);
            }
        }

        public virtual void clear()
        {
            if (target != null)
            {
                if (target is BaseGameSprite)
                {
                    (target as BaseGameSprite).isApplyG = true;
                }
            }
        }
    }
}
