using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>Stub of interfaces.lan.IUdpTransport (LAN out of scope, PORTING.md §6).</summary>
    public interface IUdpTransport
    {
        void listen(int port);
        void unListen();
        void addDataHandler(Delegate func);
        void removeDataHandler(Delegate func);
        void send(string ip, int port, object msg);
        void sendBroadcast(int port, object msg);
    }
}
