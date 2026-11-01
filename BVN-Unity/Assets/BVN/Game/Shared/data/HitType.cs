using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>Hit effect type ids; <c>isHeavy</c> tells heavy-hit types.</summary>
    public class HitType
    {
        /// <summary>No effect</summary>
        public const int NONE = 0;

        /// <summary>Slash</summary>
        public const int KAN = 1;
        /// <summary>Heavy slash</summary>
        public const int KAN_HEAVY = 6;

        /// <summary>Blunt</summary>
        public const int DA = 2;
        /// <summary>Heavy blunt</summary>
        public const int DA_HEAVY = 3;

        /// <summary>Magic light</summary>
        public const int MAGIC = 4;
        /// <summary>Magic heavy</summary>
        public const int MAGIC_HEAVY = 5;

        /// <summary>Fire</summary>
        public const int FIRE = 7;
        /// <summary>Ice</summary>
        public const int ICE = 8;
        /// <summary>Electric</summary>
        public const int ELECTRIC = 9;
        /// <summary>Stone</summary>
        public const int STONE = 10;

        /// <summary>Throw</summary>
        public const int CATCH = 11;

        /// <summary>Heavy types (heavy slash, heavy blunt, magic heavy, fire, ice, electric).</summary>
        private static readonly List<int> _isHeavyTypes = new List<int>
        {
            KAN_HEAVY, DA_HEAVY, MAGIC_HEAVY,
            FIRE, ICE, ELECTRIC
        };

        public static bool isHeavy(int type)
        {
            return _isHeavyTypes.IndexOf(type) != -1;
        }
    }
}
