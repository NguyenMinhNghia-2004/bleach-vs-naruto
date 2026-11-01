using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>Fighter input command strings.</summary>
    public class FighterInputCmd
    {
        public const string LEFT = "A";
        public const string RIGHT = "D";
        public const string DEFENSE = "S";

        public const string ATTACK = "J";
        public const string SKILL_1 = "SJ";
        public const string SKILL_2 = "WJ";

        public const string ZHAO_1 = "U";
        public const string ZHAO_2 = "SU";
        public const string ZHAO_3 = "WU";

        public const string BISHA = "I";
        public const string BISHA_UP = "WI";
        public const string BISHA_SUPER = "SI";
        public const string BISHA_AIR = "KI";

        public const string ATTACK_AIR = "KJ";
        public const string SKILL_AIR = "KU";
        public const string SKILL_AIR_W = "WKU";
        public const string SKILL_AIR_S = "SKU";

        public const string DASH = "L";
        public const string JUMP = "K";
        public const string JUMP_DOWN = "SK";

        public const string BANKAI = "JK";
        public const string BANKAI_W = "WJK";
        public const string BANKAI_S = "SJK";

        public const string GHOST_DASH_S = "SL";
        public const string GHOST_DASH_W = "WL";

        public const string ASSIST = "O";
    }
}
