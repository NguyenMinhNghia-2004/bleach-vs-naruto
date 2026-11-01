using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>Stub (LAN out of scope) of CORE_Shared data/lan/SocketInputBitCodec — faithful unpack.</summary>
    public class SocketInputBitCodec
    {
        public static void unpack(int msg, SocketInputData data)
        {
            string bits = Convert.ToString(msg, 2);
            int bitLen = bits.Length;

            data.special = charAt(bits, bitLen - 1) == "1";
            data.superSkill = charAt(bits, bitLen - 2) == "1";
            data.skill = charAt(bits, bitLen - 3) == "1";
            data.dash = charAt(bits, bitLen - 4) == "1";
            data.jump = charAt(bits, bitLen - 5) == "1";
            data.attack = charAt(bits, bitLen - 6) == "1";
            data.right = charAt(bits, bitLen - 7) == "1";
            data.left = charAt(bits, bitLen - 8) == "1";
            data.down = charAt(bits, bitLen - 9) == "1";
            data.up = charAt(bits, bitLen - 10) == "1";
        }
    }
}
