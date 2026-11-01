using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>Fighter action state ids and category checks.</summary>
    public class FighterActionState
    {
        public const int NORMAL = 0;
        public const int FREEZE = 40;

        public const int ATTACK_ING = 10;
        public const int SKILL_ING = 11;
        public const int BISHA_ING = 12;
        public const int BISHA_SUPER_ING = 13;

        public const int JUMP_ING = 14;
        public const int DASH_ING = 15;
        public const int HURT_ACT_ING = 16;

        public const int DEFENSE_ING = 20;
        public const int HURT_ING = 21;
        public const int HURT_FLYING = 22;
        public const int HURT_DOWN = 23;
        public const int HURT_DOWN_TAN = 24;

        public const int DEAD = 30;

        public const int WAN_KAI_ING = 50;
        public const int KAI_CHANG = 60;
        public const int WIN = 61;
        public const int LOSE = 62;

        private static readonly List<int> _isNotAllowWinStates = new List<int>
        {
            BISHA_ING, BISHA_SUPER_ING, WAN_KAI_ING
        };

        private static readonly List<int> _isBishaIngStates = new List<int>
        {
            BISHA_ING, BISHA_SUPER_ING
        };

        private static readonly List<int> _isAttackIngStates = new List<int>
        {
            ATTACK_ING, SKILL_ING, BISHA_ING, BISHA_SUPER_ING
        };

        private static readonly List<int> _isHurtIngStates = new List<int>
        {
            HURT_ING, HURT_FLYING, HURT_DOWN, HURT_DOWN_TAN
        };

        public static bool isAllowWinState(int actionState)
        {
            return _isNotAllowWinStates.IndexOf(actionState) == -1;
        }

        public static bool allowGhostStep(int actionState)
        {
            return _isNotAllowWinStates.IndexOf(actionState) == -1;
        }

        public static bool isBishaIng(int actionState)
        {
            return _isBishaIngStates.IndexOf(actionState) != -1;
        }

        public static bool isAttacking(int actionState)
        {
            return _isAttackIngStates.IndexOf(actionState) != -1;
        }

        public static bool isHurting(int actionState)
        {
            return _isHurtIngStates.IndexOf(actionState) != -1;
        }
    }
}
