using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>对战暂停对话框。</summary>
    public class PauseDialog : BasePauseDialog
    {
        /// <summary>构造对战暂停菜单。</summary>
        public PauseDialog() : base(new List<object>
        {
            new ASObject { ["label"] = "GAME TITLE", ["cn"] = GetLang("txt.pause_dialog.game_title") },
            new ASObject { ["label"] = "MOVE LIST", ["cn"] = GetLang("txt.common.move_list") },
            new ASObject { ["label"] = "CONTINUE", ["cn"] = GetLang("txt.common.continue") }
        })
        {
        }

        public override bool handleExtraSelect(string label)
        {
            if (label != "GAME TITLE")
            {
                return false;
            }

            _btnGroup.keyEnable = false;
            GameUI.confrim(
                GetLang("confirm.common.back_menu_title"),
                GetLang("confirm.common.back_menu"),
                (Action)(() =>
                {
                    MainGame.I.goMenu();
                }),
                (Action)(() =>
                {
                    _btnGroup.keyEnable = true;
                }),
                IsMobile()
            );

            return true;
        }
    }
}
