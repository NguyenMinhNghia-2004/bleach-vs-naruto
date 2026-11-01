using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class LevelModel
    {
        /// <summary>角色升级需要的经验值</summary>
        public static int getLevelUpExp(int level)
        {
            if (level <= 0)
            {
                return 0;
            }

            int solveExp(int lv)
            {
                int n = lv;
                int e = (int)(((3.8 * n * n) - (1.2 * n)) + 48.6);
                return e;
            }

            int exp = solveExp(level);
            if (level > 1)
            {
                for (int i = 1; i < level; i++)
                {
                    exp += solveExp(i);
                }
            }
            return exp;
        }
    }
}
