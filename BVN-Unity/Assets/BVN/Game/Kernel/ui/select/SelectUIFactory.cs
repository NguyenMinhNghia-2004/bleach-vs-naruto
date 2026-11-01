using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>创建 P1/P2 选人光标及其已选角色栏。</summary>
    public class SelectUIFactory
    {
        public static SelecterItemUI createSelecter(int playerType = 1)
        {
            var ui = new SelecterItemUI(playerType);

            ui.inputType = playerType == 1 ? GameInputType.P1 : GameInputType.P2;
            ui.selectVO = playerType == 1 ? GameData.I.p1Select : GameData.I.p2Select;

            string groupClassName = playerType == 1 ? "$select$SP_selectBarItemP1" : "$select$SP_selectBarItemP2";

            object groupClass = ResUtils.I.getItemClass(ResUtils.swfLib.select, groupClassName);

            var group = new SelectedFighterGroup(groupClass);
            group.x = playerType == 1 ? 10 : GameConfig.GAME_SIZE.x - 265;
            group.y = GameConfig.GAME_SIZE.y - 80;

            group.addFighter(null);

            ui.group = group;

            return ui;
        }
    }
}
