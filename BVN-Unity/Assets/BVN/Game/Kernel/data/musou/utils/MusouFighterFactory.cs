using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class MusouFighterFactory
    {
        public static MusouFighterVO create(string id)
        {
            MusouFighterVO mv = new MusouFighterVO();
            mv.id = id;
            return mv;
        }
    }
}
