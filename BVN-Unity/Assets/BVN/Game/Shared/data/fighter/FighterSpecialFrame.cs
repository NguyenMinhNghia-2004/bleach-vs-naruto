using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>Fighter timeline frame labels for special actions.</summary>
    public class FighterSpecialFrame
    {
        public const string IDLE = "站立";
        public const string MOVE = "走";
        public const string DASH = "瞬步";
        public const string JUMP_START = "起跳";
        public const string JUMP = "跳";
        public const string JUMP_ING = "跳中";
        public const string JUMP_DOWN = "落";
        public const string JUMP_TOUCH_FLOOR = "落地";

        public const string ATTACK_AIR = "跳砍";
        public const string SKILL_AIR = "跳招";
        public const string SKILL_AIR_W = "跳招-上";
        public const string SKILL_AIR_S = "跳招-下";

        public const string ATTACK = "砍1";

        public const string SKILL_1 = "砍技1";
        public const string SKILL_2 = "砍技2";

        public const string ZHAO_1 = "招1";
        public const string ZHAO_2 = "招2";
        public const string ZHAO_3 = "招3";

        public const string BISHA = "必杀";
        public const string BISHA_UP = "上必杀";
        public const string BISHA_SUPER = "超必杀";
        public const string BISHA_AIR = "空中必杀";

        public const string CATCH_1 = "摔1";
        public const string CATCH_2 = "摔2";

        public const string DEFENSE = "防御";
        public const string DEFENSE_RESUME = "防御恢复";

        public const string HURT = "被打";
        public const string HURT_FLY = "击飞";
        public const string HURT_FALL = "击飞_落";
        public const string HURT_TAN = "击飞_弹";
        public const string HURT_DOWN = "击飞_倒";
        public const string HURT_DOWN_RESUME = "击飞_起";
        public const string HURT_DOWN_JUMP = "起身";

        public const string BANKAI = "万解";
        public const string BANKAI_W = "万解W";
        public const string BANKAI_S = "万解S";

        public const string SAY_INTRO = "开场";
        public const string WIN = "胜利";
        public const string LOSE = "失败";
    }
}
