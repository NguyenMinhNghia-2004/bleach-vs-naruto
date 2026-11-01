using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>对局会话：格斗 / 无双等模式差异的窄接口。</summary>
    public interface IFightSession
    {
        void onGameInitialize();
        void destroy();
        FighterAttacker getAttacker(string name, int team);
        void buildGame();
        void render();
        void renderAnimate();
        bool allowsRoundTimer();
        bool isFinished();
        MusouCtrl getMusouCtrl();
    }
}
