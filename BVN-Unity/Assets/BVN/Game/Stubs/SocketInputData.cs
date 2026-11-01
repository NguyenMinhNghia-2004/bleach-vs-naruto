using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>Stub (LAN out of scope) of CORE_Shared data/lan/SocketInputData — full data class, trivial.</summary>
    public class SocketInputData
    {
        public bool up;
        public bool down;
        public bool left;
        public bool right;
        public bool attack;
        public bool jump;
        public bool dash;
        public bool skill;
        public bool superSkill;
        public bool special;
        public bool select;
        public bool back;

        public virtual void clear()
        {
            up = false;
            down = false;
            left = false;
            right = false;

            attack = false;
            jump = false;
            dash = false;
            skill = false;
            superSkill = false;
            special = false;

            select = false;
            back = false;
        }
    }
}
