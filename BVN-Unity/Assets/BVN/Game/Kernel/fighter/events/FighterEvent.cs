using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>角色事件</summary>
    public class FighterEvent : Event
    {
        // 角色创建
        public const string BIRTH = "BIRTH";

        // 发射子弹
        public const string FIRE_BULLET = "FIRE_BULLET";
        // 加入攻击对象
        public const string ADD_ATTACKER = "ADD_ATTACKER";
        // 加入特效对象
        public const string ADD_EFFECT = "ADD_EFFECT";

        // 攻击到目标
        public const string HIT_TARGET = "HIT_TARGET";
        // 被打
        public const string HURT = "HURT";
        // 从被打状态恢复
        public const string HURT_RESUME = "HURT_RESUME";
        // 被击倒
        public const string HURT_DOWN = "HURT_DOWN";

        // 防御
        public const string DEFENSE = "DEFENSE";
        // 恢复站立
        public const string IDLE = "IDLE";

        // 死亡
        public const string DIE = "DIE";
        // 死透（死亡&击倒）
        public const string DEAD = "DEAD";

        // 执行动作
        public const string DO_ACTION = "DO_ACTION";
        // 执行特殊
        public const string DO_SPECIAL = "DO_SPECIAL";

        public FighterEvent(string type, bool bubbles = false, bool cancelable = false) : base(type, bubbles, cancelable)
        {
        }

        // 传递参数
        public object @params;
        // 目标对象
        public BaseGameSprite fighter;
    }
}
