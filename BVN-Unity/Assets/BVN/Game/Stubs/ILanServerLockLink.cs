using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>Stub of interfaces.lan.ILanServerLockLink (LAN out of scope, PORTING.md §6).</summary>
    public interface ILanServerLockLink
    {
        void sendUDP(object data);
    }
}
