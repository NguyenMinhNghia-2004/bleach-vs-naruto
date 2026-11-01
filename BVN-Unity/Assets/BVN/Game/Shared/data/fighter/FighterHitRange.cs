using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>Hit-range clip instance names.</summary>
    public class FighterHitRange
    {
        public const string ATTACK_AIR = "tkanmian";
        public const string SKILL_AIR = "tzmian";

        public const string ATTACK = "kanmian";

        public const string SKILL_1 = "kj1mian";
        public const string SKILL_2 = "kj2mian";

        public const string ZHAO_1 = "zh1mian";
        public const string ZHAO_2 = "zh2mian";
        public const string ZHAO_3 = "zh3mian";

        public const string BISHA = "bsmian";
        public const string BISHA_AIR = "kbsmian";
        public const string BISHA_UP = "sbsmian";
        public const string BISHA_SUPER = "cbsmian";

        public static readonly List<string> ALL_HIT_RANGES = new List<string>
        {
            ATTACK_AIR, SKILL_AIR,
            ATTACK,
            SKILL_1, SKILL_2,
            ZHAO_1, ZHAO_2, ZHAO_3,
            BISHA, BISHA_AIR, BISHA_UP, BISHA_SUPER
        };
    }
}
