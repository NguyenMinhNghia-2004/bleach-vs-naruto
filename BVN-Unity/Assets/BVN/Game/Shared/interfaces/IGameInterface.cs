using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>Shell (platform) interface injected into the kernel.</summary>
    public interface IGameInterface
    {
        void initTitleUI(DisplayObject ui);
        void moreGames();
        void showRank();
        void submitScore(int score);
        void saveGame(object data);
        object loadGame();
        List<IGameInput> getGameInput(string type);
        List<object> getGameMenu();
        List<object> getSettingMenu();
        bool updateInputConfig();
        IExtendConfig getConfigExtend();
        void afterBuildGame();
        void applyConfig(object config);
        Sprite getCredits(string creditsInfo);
        /// <summary>file: flash.utils.ByteArray in AS3 (ByteArray stub or raw bytes; typed object so shells need not depend on the stub).</summary>
        bool checkFile(string url, object file);
        void addMusouMoney(Delegate back);
    }
}
