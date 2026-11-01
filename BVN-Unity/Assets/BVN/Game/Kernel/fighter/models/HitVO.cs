using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>攻击值对象</summary>
    public class HitVO : IInstanceVO
    {
        // include/Clone.as
        /// <summary>克隆自身</summary>
        public virtual IInstanceVO clone(params object[] arg)
        {
            return InstanceVOClone.clone(this, new List<object>(arg ?? Array.Empty<object>()));
        }

        /// <summary>Parameterless constructor (needed for reflective cloning; same as passing null).</summary>
        public HitVO() : this(null)
        {
        }

        /// <summary>攻击值对象</summary>
        /// <param name="obj">对象参数</param>
        public HitVO(object obj)
        {
            if (Bool(obj))
            {
                KyoUtils.setValueByObject(this, obj);
            }

            if (hitType == HitType.KAN)
            {
                if (hurtTime < 100)
                {
                    hurtTime = 100;
                }
            }

            // 如果包含自定义效果，则执行
            if (Bool(customEffect))
            {
                customEffectVO = new EffectVO(null, customEffect);
                customEffectVO.cacheBitmapData();
            }
        }

        // 攻击 id
        public string id = null;
        // 所有者
        public IGameSprite owner = null;

        // 攻击力
        public double power = 0;
        // 叠加攻击力
        public double powerAdd = 0;
        // 攻击倍数
        public double powerRate = 1;

        // 攻击类型
        public int hitType = HitType.KAN;

        // 是否造成破防效果
        public bool isBreakDef = false;

        // 击退速度
        public double hitx = 0;
        // 击飞速度
        public double hity = 0;

        // 僵直时间（毫秒）
        public double hurtTime = 300;
        // 受击类型（0 被打， 1 击飞）
        public int hurtType = FighterHurtType.HURT;

        // 受击后的慢放时间（毫秒）
        public double slowDown = 0;

        // 是否检查方向，若为 true，则当攻击方与被攻击方同向时，无视防御
        public bool checkDirect = true;

        // 攻击范围
        public Rectangle currentArea = null;

        // 攻击到对方时，设置对方为焦点
        public bool focusTarget = false;

        // 自定义击打效果参数
        // {customMcCls: mc_cls, customSndCls: snd_cls, ...}
        public object customEffect = null;
        // 自定义打击效果 EffectVO
        public EffectVO customEffectVO = null;

        // 对方受到此次伤害后，是否受到重力效果
        public bool targetApplyG = true;

        /// <summary>是否为必杀</summary>
        public virtual bool isBisha()
        {
            if (id == null)
            {
                return false;
            }

            return AS3.indexOf(id, "bs") != -1 ||
                   AS3.indexOf(id, "sbs") != -1 ||
                   AS3.indexOf(id, "cbs") != -1 ||
                   AS3.indexOf(id, "kbs") != -1;
        }

        /// <summary>是否为普通技能</summary>
        public virtual bool isSkill()
        {
            if (id == null)
            {
                return false;
            }

            return AS3.indexOf(id, "tz") != -1 ||
                   AS3.indexOf(id, "kj") != -1 ||
                   AS3.indexOf(id, "zh") != -1 ||
                   AS3.indexOf(id, "sh") != -1;
        }

        /// <summary>是否为摔技</summary>
        public virtual bool isCatch()
        {
            return hitType == HitType.CATCH && isBreakDef;
        }

        /// <summary>获取当前攻击伤害</summary>
        public virtual int getDamage()
        {
            double powAdd = power * (powerAdd / 100);

            return Int((power + powAdd) * powerRate);
        }

        /// <summary>是否为弱打击伤害</summary>
        public virtual bool isWeakHit()
        {
            if (owner == null)
            {
                return false;
            }

            if (HitType.isHeavy(hitType) || hurtType == 1)
            {
                return false;
            }

            if (owner is FighterMain)
            {
                return checkEnemyFighter(owner as FighterMain);
            }
            if (owner is Bullet)
            {
                return checkEnemyFighter((owner as Bullet).owner as FighterMain);
            }
            if (owner is FighterAttacker)
            {
                return checkEnemyFighter((owner as FighterAttacker).getOwner() as FighterMain);
            }

            return false;
        }

        /// <summary>检查敌人 FighterMain 是否不为 Boss</summary>
        private static bool checkEnemyFighter(FighterMain fighter)
        {
            if (fighter == null || fighter.musouEnemyData == null)
            {
                return false;
            }

            return !fighter.musouEnemyData.isBoss;
        }
    }
}
