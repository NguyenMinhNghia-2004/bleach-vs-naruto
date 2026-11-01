using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public interface IGameUI
    {
        void setVolume(double v);

        void destroy();

        void fadIn(bool animate = true);

        void fadOut(bool animate = true);

        DisplayObject getUI();

        void render();

        void renderAnimate();

        void showHits(int hits, int id);

        void hideHits(int id);

        void showStart(Delegate finishBack = null, object @params = null);

        void showEnd(Delegate finishBack = null, object @params = null);

        void showContinue(Delegate onClick);

        void clearStartAndEnd();

        void pause();

        bool resume();
    }
}
