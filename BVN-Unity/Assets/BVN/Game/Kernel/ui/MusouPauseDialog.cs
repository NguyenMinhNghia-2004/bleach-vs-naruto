using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>无双暂停对话框。</summary>
    public class MusouPauseDialog : BasePauseDialog
    {
        /// <summary>构造无双暂停菜单。</summary>
        public MusouPauseDialog() : base(new List<object>
        {
            new ASObject { ["label"] = "BACK MAP", ["cn"] = GetLang("txt.musou_pause_dialog.back_map") },
            new ASObject { ["label"] = "MOVE LIST", ["cn"] = GetLang("txt.common.move_list") },
            new ASObject { ["label"] = "CONTINUE", ["cn"] = GetLang("txt.common.continue") }
        }, true)
        {
        }

        public override bool handleExtraSelect(string label)
        {
            if (label != "BACK MAP")
            {
                return false;
            }

            _btnGroup.keyEnable = false;
            GameUI.confrim(
                GetLang("confirm.musou_pause_dialog.back_map_title"),
                GetLang("confirm.musou_pause_dialog.back_map"),
                (Action)(() =>
                {
                    MainGame.I.goWorldMap();
                    GameEvent.dispatchEvent(GameEvent.MUSOU_BACK_MAP);
                }),
                (Action)(() =>
                {
                    _btnGroup.keyEnable = true;
                }),
                true
            );

            return true;
        }
    }
}
